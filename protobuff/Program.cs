using System;
using System.Reflection;
using System.IO;
using Google.Protobuf;
using AnyClass;
using Google.Protobuf.WellKnownTypes;

namespace Person
{
    class Program
    {
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
        public static object readChildMessage(genericClass genericClass)
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
            catch (InvalidProtocolBufferException e)
            {
                e.GetType();
                return null;
            }

        }
        static void Main(string[] args)
        {
            // Create a class, can be any class
            Person person = new Person
            {
                Id = 5,
                Name = "shaked",
                Emails = { "shake", "email" }
            };

            // Create out generic class that holds any other classes
            genericClass genericClass = new genericClass
            {
                MyObject = Any.Pack(person)
            };

            // Serialize the class and write it to a file
            using (var output = File.Create("person.dat"))
            {
                genericClass.WriteTo(output);
            }

            // Deserialize the file and read it to our generic class
            genericClass parsedClass;
            using (var input = File.OpenRead("person.dat"))
            {
                parsedClass = genericClass.Parser.ParseFrom(input);
            }

            // Parse and unpack the any object
            object ob = readChildMessage(parsedClass);
            Console.WriteLine("type: {0}\nvalue: {1}", ob.GetType(), ob.ToString());

        }
    }
}

