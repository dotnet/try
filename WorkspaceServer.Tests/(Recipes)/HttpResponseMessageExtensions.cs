using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;

namespace Recipes
{
    internal static class HttpResponseMessageExtensions
    {
        public static async Task<T> DeserializeAs<T>(this HttpResponseMessage response)
        {
            var json = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<T>(json);
        }

        public static IEnumerable<SetCookieHeaderValue> GetSetCookieHeaderValues(this HttpResponseMessage subject)
        {
            subject = subject ?? throw new ArgumentNullException(nameof(subject));

            IEnumerable<string> values;

            if (!subject.Headers.TryGetValues("Set-Cookie", out values))
            {
                return Enumerable.Empty<SetCookieHeaderValue>();
            }

            return SetCookieHeaderValue.ParseList(values.ToList()).ToList();
        }

        public static string GetSetCookieHeaderValue(this HttpResponseMessage subject, string cookieName)
        {
            subject = subject ?? throw new ArgumentNullException(nameof(subject));

            cookieName = cookieName ?? throw new ArgumentNullException(nameof(cookieName));

            return subject
                .GetSetCookieHeaderValues()
                .FirstOrDefault(c => c.Name == cookieName)
                ?.Value
                .ToString();
        }
    }
}
