using System;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Try.ProjectTemplate.Tutorial
{
    ///<param name="region">Takes in the --region option from the code fence options in markdown</param>
    ///<param name="session">Takes in the --session option from the code fence options in markdown</param>
    ///<param name="package">Takes in the --package option from the code fence options in markdown</param>
    ///<param name="project">Takes in the --project option from the code fence options in markdown</param>
    ///<param name="args">Takes in any additional arguments passed in the code fence options in markdown</param>
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

        public static int EmptyRegion()
        {
            #region EmptyRegion
            #endregion
            return 0;
        }
    }
}