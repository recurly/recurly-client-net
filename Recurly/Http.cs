using System;
using System.Collections.Generic;
using System.Net;
using RestSharp;

namespace Recurly.Http
{
    public class Response
    {
        public string RawResponse { get; set; }

        public HttpStatusCode StatusCode { get; set; }

        public IList<Header> Headers { get; set; }

        public string RequestId { get { return GetHeader("X-Request-Id"); } }

        public int? RateLimit { get { return GetIntHeader("X-RateLimit-Limit"); } }

        public int? RateLimitRemaining { get { return GetIntHeader("X-RateLimit-Remaining"); } }

        public int? RateLimitReset { get { return GetIntHeader("X-RateLimit-Reset"); } }

        public string ContentType { get { return GetHeader("Content-Type"); } }

        public int? RecordCount { get { return GetIntHeader("Recurly-Total-Records"); } }

        public Response() { }

        public static Response Build(IRestResponse resp)
        {
            // Map List<Parameter> to List<Header>
            var headers = new List<Header>();
            foreach (var header in resp.Headers)
            {
                headers.Add(new Header(header.Name, (string)header.Value));
            }
            return new Response()
            {
                RawResponse = resp.Content,
                StatusCode = resp.StatusCode,
                Headers = headers,
            };
        }

        private string GetHeader(string name)
        {
            foreach (var header in Headers)
                if (header.Name == name)
                    return header.Value;
            return null;
        }

        private int? GetIntHeader(string name)
        {
            var header = GetHeader(name);
            if (header is null)
                return null;
            try
            {
                return Int32.Parse(header);
            }
            catch (Exception)
            {
                return null;
            }
        }

    }

    public class Header
    {
        public string Name { get; set; }
        public string Value { get; set; }

        public Header(string name, string value)
        {
            Name = name;
            Value = value;
        }
    }
}