using System;
using System.Reflection;
using System.IO;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;


namespace Generic
{
    class Program
    {
        public const string fileName = "Person.dat";
        // Convert string name of class to type object
        public static System.Type GetType(string typeName)
        {
            var type = System.Type.GetType(typeName);
            if (type != null) return type;
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = a.GetType(typeName);
                if (type != null)
                    return type;
            }
            return null;
        }

        // Find the Any.unpack method
        public static MethodInfo GetUnpackMethod() 
        {
            var methods = typeof(Any).GetMethods(BindingFlags.Public | BindingFlags.Instance);
            foreach (MethodInfo method in methods)
            {
                if (method.Name == "Unpack")
                {
                    return method;
                }
            }
            return null;
        }

        // Parse and unpack the any object
        public static object readChildMessage(GenericClass genericClass)
        {
            try
            {
                Any childMessage = genericClass.MyObject;
                // Get the type of the class 
                string clazzName = childMessage.TypeUrl.Split("/")[1]; 
                
                // Convert it to type object
                System.Type type = GetType(clazzName);

                // Create instance of the class
                object myObject = Activator.CreateInstance(type);

                // Beacuse we get the type of the generic object in run time, 
                // we can't call it directly so we need to invoke it,
                // so we search the Any.Unpack methods in order to invoke it later
                MethodInfo unpackMethod = GetUnpackMethod();

                // Build a method with the specific type argument you're interested in
                unpackMethod = unpackMethod.MakeGenericMethod(type);

                // Invoke the method
                myObject = unpackMethod.Invoke(childMessage, null);
                return myObject;
            }
            catch (Exception ex) when (ex is InvalidProtocolBufferException || ex is NullReferenceException)
            {
                return null;
            }

        }

        // Get a message and write it to a file
        public static void writeToFile(IMessage message ,string fileName) 
        {
            // Create out generic class that holds any other classes
            GenericClass genericClass = new GenericClass 
            { 
                MyObject = Any.Pack(message) 
            };

            // Serialize the class and write it to a file
            using (Stream output = File.Open(fileName, FileMode.Append))
            {
                MessageExtensions.WriteDelimitedTo(genericClass, output);
            }
        }

        // Get a file name and read the first object 
        public static GenericClass readFromFile(Stream input)
        {
            MessageParser<GenericClass> parser = new MessageParser<GenericClass>(() => { return new GenericClass(); });
            try
            {
                // Deerialize the first object and return it as generic class
                return parser.ParseDelimitedFrom(input);
            }
            // If there is no more objects
            catch (InvalidProtocolBufferException) 
            {
                return null;
            }
            
        }

        static void Main()
        {
            using (File.Create(fileName));

            // Create a class, can be any class
            PhoneNumber phoneNumber = new PhoneNumber
            {
                Prefix = "054",
                Number = "7523111"
            };

            Person person = new Person
            {
                Id = 5,
                Name = "shaked",
                Emails = { "shake", "email" },
                Number = phoneNumber
            };

            Person meir = new Person
            {
                Id = 8,
                Name = "meir",
                Emails = { "meir", "gmail" }
            };

            Dog dog = new Dog
            {
                Name = "Lucio",
                Type = "Bulldog"
            };

            // Write our objects to a file
            writeToFile(person, fileName);            
            writeToFile(phoneNumber, fileName);
            writeToFile(dog, fileName);
            writeToFile(meir, fileName);

            // Read objects from file. Parse and unpack the any object
            using (Stream input = File.OpenRead(fileName))
            {
                object obPerson = readChildMessage(readFromFile(input));
                object obPhone = readChildMessage(readFromFile(input));
                object obDog = readChildMessage(readFromFile(input));
                object obMeir = readChildMessage(readFromFile(input));
                Console.WriteLine("type: {0}\nvalue: {1}\n", obPerson.GetType(), obPerson.ToString());
                Console.WriteLine("type: {0}\nvalue: {1}\n", obPhone.GetType(), obPhone.ToString());
                Console.WriteLine("type: {0}\nvalue: {1}\n", obDog.GetType(), obDog.ToString());
                Console.WriteLine("type: {0}\nvalue: {1}\n", obMeir.GetType(), obMeir.ToString());
            }
        }
    }
}

