using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace APIService
{
    internal class FileHandler : HttpMessageHandler
    {
        protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\");
            string requestPath = basePath + request.RequestUri.LocalPath.Replace(@"/html", @"/ClientApp/").Replace(@"/", @"\");
            if (string.IsNullOrEmpty(Path.GetFileName(requestPath)))
            {
                requestPath = Path.Combine(basePath, "ClientApp", "index.html");
            }
            if (File.Exists(requestPath) == false)
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }

            var stream = new FileStream(requestPath, FileMode.Open);

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(stream),

            };
            string ext = Path.GetExtension(requestPath);

            response.Content.Headers.ContentType = mimeMap[ext];

            return response;
        }

        public static Dictionary<string, MediaTypeHeaderValue> mimeMap = new Dictionary<string, MediaTypeHeaderValue>()
        {
            {
                ".html", new MediaTypeHeaderValue("text/html")
            },
             {
                ".js", new MediaTypeHeaderValue("text/javascript")
            },
              {
                ".css", new MediaTypeHeaderValue("text/css")
            },
        };

    }
}