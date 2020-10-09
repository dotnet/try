using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;

namespace Recipes
{
    [DebuggerStepThrough]
    internal class HttpResponseMessageAssertions : ObjectAssertions
    {
        private readonly HttpResponseMessage _subject;

        public HttpResponseMessageAssertions(HttpResponseMessage subject) : base(subject)
        {
            _subject = subject ?? throw new ArgumentNullException(nameof(subject));
        }

        public AndConstraint<HttpResponseMessageAssertions> BeSuccessful()
        {
            string content = "";

            if (!_subject.IsSuccessStatusCode)
            {
                content = _subject.Content.ReadAsStringAsync().Result;
            }

            Execute.Assertion
                   .ForCondition(_subject.IsSuccessStatusCode)
                   .FailWith("Expected successful response but received: {0}\nResponse body:\n{1}", _subject, content);

            return new AndConstraint<HttpResponseMessageAssertions>(this);
        }

        public AndConstraint<HttpResponseMessageAssertions> BeNotFound() 
        { 
            Execute.Assertion 
                .ForCondition(_subject.StatusCode == HttpStatusCode.NotFound) 
                .FailWith($"Expected Forbidden response but received: {_subject.ToString().Replace("{", "{{").Replace("}", "}}")}"); 
 
            return new AndConstraint<HttpResponseMessageAssertions>(this); 
        } 
    }

    internal static class HttpResponseMessageAssertionExtensions
    {
        public static HttpResponseMessage EnsureSuccess(this HttpResponseMessage subject)
        {
            subject.Should().BeSuccessful();

            return subject;
        }

        public static HttpResponseMessageAssertions Should(this HttpResponseMessage subject) => 
            new HttpResponseMessageAssertions(subject);
    }
}
