using System;
using System.Collections.Generic;
using System.Linq;
namespace myapp
{
    class Program
    {
        static void Main(string region = null,
            string session = null,
            string package = null,
            string project = null,
            string[] args = null)
        {
            switch(region)
            {
                case "intro":
                    Intro();
                    break;
                case "strings":
                    Strings();
                    break;
                case "variables":
                    Variables();
                    break;
                case "interpolation":
                    Interpolation();
                    break;
                case "methods":
                    Methods();
                    break;
                case "collections":
                    Collections();
                    break;
            }
        }
        public static void Intro()
        {
            #region intro
            Console.WriteLine("Hello World!");
            #endregion
        }
        public static void Strings()
        {
            #region strings
            Console.WriteLine("Hello Rain");
            #endregion
        }
        public static void Variables()
        {
            #region variables
            var name = "Rain";
            Console.WriteLine("Hello " + name + "!");
            #endregion
        }
        
        public static void Interpolation()
        {
            #region interpolation
            var name = "Rain";
            Console.WriteLine($"Hello {name}!");
            #endregion
        }
        public static void Methods()
        {
            #region methods
            var name ="Rain";
            Console.WriteLine($"Hello {name.ToUpper()}!");
            #endregion
        }
        public static void Collections()
        {
            #region collections
            var names = new List<string> { "Rain", "Sage", "Lee" };
            foreach (var name in names)
            {
                Console.WriteLine($"Hello {name.ToUpper()}!");
            }
            #endregion
        }
        
    }
}
