using System.Net.Http;
using System.Text;

namespace Recipes
{
    internal class JsonContent : StringContent
    {
        public JsonContent(object content)
            : base(content.ToJson(),
                   Encoding.UTF8,
                   "application/json")
        {
        }

        public JsonContent(string content)
            : base(content,
                   Encoding.UTF8,
                   "application/json")
        {
        }
    }
}
