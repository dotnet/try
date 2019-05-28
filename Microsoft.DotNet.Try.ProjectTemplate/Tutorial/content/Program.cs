using System;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Try.ProjectTemplate.Tutorial
{
    public class Program
    {
        static int Main(
            string region = null,
            string session = null,
            string package = null,
            string project = null,
            string[] args = null)
        {
            return region switch
            {
                "HelloWorld" => HelloWorld(),
                "DateTime" => DateTime(),
                "Guid" => Guid(),
                "EmptyRegion" => EmptyRegion(),
                _ => EmptyRegion()
            };
        }

        public static int HelloWorld()
        {
            #region HelloWorld
            Console.WriteLine("Hello World!");
            #endregion
            return 0;
        }

        public static int DateTime()
        {
            #region DateTime
            Console.WriteLine(System.DateTime.Now);
            #endregion
            return 0;
        }

        public static int Guid()
        {
            #region Guid
            Console.WriteLine(System.Guid.NewGuid());
            #endregion
            return 0;
        }
        
        public static int EmptyRegion()
        {
            #region EmptyRegion
            #endregion
            return 0;
        }
    }
}