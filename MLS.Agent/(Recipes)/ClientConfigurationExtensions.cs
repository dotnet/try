using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Microsoft.DotNet.Try.Protocol;
using Microsoft.DotNet.Try.Protocol.ClientApi;
using Microsoft.DotNet.Try.Protocol.ClientApi.GitHub;

namespace Recipes
{
    internal static class ClientConfigurationExtensions
    {
        private static readonly Regex OptionalRouteFilter = new Regex(@"/\{.+\?\}", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        private static string ToSha256(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            var inputBytes = Encoding.UTF8.GetBytes(value);

            byte[] hash;
            using (var sha256 = SHA256.Create())
            {
                hash = sha256.ComputeHash(inputBytes);
            }

            return Convert.ToBase64String(hash);
        }
        public static string ComputeHash(this RequestDescriptors links)
        {
            return ToSha256(links.ToJson());
        }

        public static string BuildUrl(this RequestDescriptor requestDescriptor, Dictionary<string, object> context = null)
        {
            var url = requestDescriptor.Href;
            if (requestDescriptor.Templated && context?.Count > 0)
            {
                foreach (var entry in context)
                {
                    var filter = new Regex(@"\{" + entry.Key + @"\??\}", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
                    url = filter.Replace(url, $"{UrlEncode(entry.Value.ToString())}");
                }
            }

            return OptionalRouteFilter.Replace(url, string.Empty);
        }

        public static string BuildQueryString(this RequestDescriptor requestDescriptor, Dictionary<string, object> context = null)
        {
            var parts = new List<string>();
            if (context?.Count > 0)
            {
                if (context.TryGetValue("hostOrigin", out var hostOrigin))
                {
                    parts.Add($"hostOrigin={UrlEncode(hostOrigin.ToString())}");
                }

                foreach (var property in requestDescriptor.Properties ?? Enumerable.Empty<RequestDescriptorProperty>())
                {
                    if (context.TryGetValue(property.Name, out var propertyValue))
                    {
                        parts.Add($"{property.Name}={UrlEncode(propertyValue.ToString())}");
                    }
                }
            }

            return string.Join("&", parts);
        }

        public static HttpRequestMessage BuildRequest(this RequestDescriptor requestDescriptor, Dictionary<string, object> context = null)
        {
            var fullUrl = requestDescriptor.BuildFullUri(context);

            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(fullUrl, UriKind.RelativeOrAbsolute)
            };

            switch (requestDescriptor.Method)
            {
                case "POST":
                    request.Method = HttpMethod.Post;
                    break;
                case "DELETE":
                    request.Method = HttpMethod.Delete;
                    break;
                case "PUT":
                    request.Method = HttpMethod.Put;
                    break;
                case "HEAD":
                    request.Method = HttpMethod.Head;
                    break;
                case "OPTIONS":
                    request.Method = HttpMethod.Options;
                    break;
                case "TRACE":
                    request.Method = HttpMethod.Trace;
                    break;
                default:
                    request.Method = HttpMethod.Get;
                    break;
            }

            return request;
        }

        public static string BuildFullUri(this RequestDescriptor requestDescriptor, Dictionary<string, object> context = null)
        {
            var url = requestDescriptor.BuildUrl(context);
            var queryString = requestDescriptor.BuildQueryString(context);

            var fullUrl = url;
            if (!string.IsNullOrWhiteSpace(queryString))
            {
                var joinSymbol = "?";
                if (url.IndexOf("?", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    joinSymbol = "&";
                }

                fullUrl = $"{url}{joinSymbol}{queryString}";
            }

            return fullUrl;
        }

        public static HttpRequestMessage BuildLoadFromRequest(this ClientConfiguration configuration, string codeUrl, string hostOrigin)
        {
            var api = configuration.Links.Snippet;
            var context = new Dictionary<string, object>
            {
                { "from", codeUrl }
            };

            var request = BuildRequestWithHeaders(configuration, api, hostOrigin, context);

            return request;
        }

        private static HttpRequestMessage BuildRequestWithHeaders(this ClientConfiguration configuration, RequestDescriptor requestDescriptor, string hostOrigin, Dictionary<string, object> context = null)
        {
            var safeContext = context ?? new Dictionary<string, object>();

            if (hostOrigin != null)
            {
                safeContext["hostOrigin"] = hostOrigin;
            }

            var request = requestDescriptor.BuildRequest(safeContext);

            configuration.AddConfigurationVersionIdHeader(request);
            configuration.AddTimeoutHeader(request, requestDescriptor);

            return request;
        }

        public static HttpRequestMessage BuildLoadFromGistRequest(this ClientConfiguration configuration, string gist, string hash = null, string workspaceType = null,
            bool? extractBuffers = null, string hostOrigin = null)
        {
            var api = configuration.Links.LoadFromGist;
            var context = new Dictionary<string, object>
            {
                { "gistId", gist }
            };

            if (hash != null)
            {
                context["commitHash"] = hash;
            }

            if (workspaceType != null)
            {
                context["workspaceType"] = workspaceType;
            }

            if (extractBuffers != null)
            {
                context["extractBuffers"] = extractBuffers;
            }

            return BuildRequestWithHeaders(configuration, api, hostOrigin, context);
        }

        public static HttpRequestMessage BuildRegionFromFilesRequest(this ClientConfiguration configuration, IEnumerable<SourceFile> files, string hostOrigin, string requestId = null)
        {
            var api = configuration.Links.RegionsFromFiles;

            var request = BuildRequestWithHeaders(configuration, api, hostOrigin);
            var payload = new CreateRegionsFromFilesRequest(requestId ?? (Guid.NewGuid().ToString()), files?.ToArray());
            SetRequestContent(payload, request);

            return request;
        }

        public static HttpRequestMessage BuildProjectFromGistRequest(this ClientConfiguration configuration, string gistId, string projectTemplate, string hostOrigin, string hash = null, string requestId = null)
        {
            var api = configuration.Links.ProjectFromGist;

            var request = BuildRequestWithHeaders(configuration, api, hostOrigin);
            var payload = new CreateProjectFromGistRequest(requestId ?? (Guid.NewGuid().ToString()), gistId, projectTemplate, hash);
            SetRequestContent(payload, request);

            return request;
        }

        public static HttpRequestMessage BuildCompletionRequest(this ClientConfiguration configuration, object workspaceRequest, string hostOrigin)
        {
            var api = configuration.Links.Completion;

            var request = BuildRequestWithHeaders(configuration, api, hostOrigin);

            SetRequestContent(workspaceRequest, request);

            return request;
        }

        public static HttpRequestMessage BuildCompileRequest(this ClientConfiguration configuration, object workspaceRequest, string hostOrigin)
        {
            var api = configuration.Links.Compile;

            var request = BuildRequestWithHeaders(configuration, api, hostOrigin);

            SetRequestContent(workspaceRequest, request);

            return request;
        }

        public static HttpRequestMessage BuildSignatureHelpRequest(this ClientConfiguration configuration, object workspaceRequest, string hostOrigin)
        {
            var api = configuration.Links.SignatureHelp;

            var request = BuildRequestWithHeaders(configuration, api, hostOrigin);

            SetRequestContent(workspaceRequest, request);

            return request;
        }

        public static HttpRequestMessage BuildDiagnosticsRequest(this ClientConfiguration configuration, object workspace, string hostOrigin)
        {
            var api = configuration.Links.Diagnostics;

            var request = BuildRequestWithHeaders(configuration, api, hostOrigin);

            SetRequestContent(workspace, request);

            return request;
        }

        public static HttpRequestMessage BuildRunRequest(this ClientConfiguration configuration, object workspaceRequest, string hostOrigin)
        {
            var api = configuration.Links.Run;

            var request = BuildRequestWithHeaders(configuration, api, hostOrigin);

            SetRequestContent(workspaceRequest, request);

            return request;
        }

        public static HttpRequestMessage BuildVersionRequest(this ClientConfiguration configuration, string hostOrigin)
        {
            var api = configuration.Links.Version;

            return BuildRequestWithHeaders(configuration, api, hostOrigin);
        }

        public static HttpRequestMessage BuildGetPackagesRequest(this ClientConfiguration configuration, string packageName, string packageVersion, string hostOrigin)
        {
            var api = configuration.Links.GetPackage;
            var context = new Dictionary<string, object>
            {
                { "name", packageName },
                { "version", packageVersion }
            };

            var request = BuildRequestWithHeaders(configuration, api, hostOrigin, context);


            return request;
        }

        private static string UrlEncode(string source)
        {
            return HttpUtility.UrlEncode(HttpUtility.UrlDecode(source));
        }

        private static void AddConfigurationVersionIdHeader(this ClientConfiguration configuration, HttpRequestMessage request)
        {
            request.Headers.Remove("ClientConfigurationVersionId");
            request.Headers.Add("ClientConfigurationVersionId", configuration.VersionId);
        }

        private static void AddTimeoutHeader(this ClientConfiguration configuration, HttpRequestMessage request, RequestDescriptor requestDescriptor = null)
        {
            request.Headers.Remove("Timeout");
            var timeoutMs = configuration.DefaultTimeoutMs.ToString(CultureInfo.InvariantCulture);

            if (requestDescriptor != null && requestDescriptor.TimeoutMs > 0)
            {
                timeoutMs = requestDescriptor.TimeoutMs.ToString(CultureInfo.InvariantCulture);
            }

            request.Headers.Add("Timeout", timeoutMs);
        }

        private static void SetRequestContent(object content, HttpRequestMessage request)
        {
            switch (content)
            {
                case string text:
                    request.Content = new JsonContent(text);
                    break;
                default:
                    request.Content = new JsonContent(content);
                    break;
            }
        }
    }
}
