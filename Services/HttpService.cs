using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ZadalBot.Services
{
    public class HttpService
    {
        private readonly HttpListener _listener;
        public HttpService(ZadalBotConfig config)
        {
            _listener = new HttpListener {Prefixes = {config.HttpUri}};
        }

        public event Func<Task<JArray>> DataProvider;
        public event Func<JObject, Task> OnDataReceived; 

        private async Task HandleGet(HttpListenerContext ctx)
        {
            if (DataProvider == null)
            {
                Console.WriteLine("HTTP > DataProvider is null");
                ctx.Response.StatusCode = (int) HttpStatusCode.InternalServerError;
                return;
            }

            var data = await DataProvider();
            var text = data.ToString(Formatting.None);

            await ctx.Response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes(text));
            ctx.Response.StatusCode = (int) HttpStatusCode.OK;

            ctx.Response.OutputStream.Close();
        }

        private async Task HandlePost(HttpListenerContext ctx)
        {
            if (OnDataReceived == null)
                Console.WriteLine("HTTP > OnDataReceived is null");
            else
            {
                string text;
                await using (var textStream = ctx.Request.InputStream)
                using (var textReader = new StreamReader(textStream, Encoding.UTF8))
                {
                    text = await textReader.ReadToEndAsync();
                }

                await OnDataReceived(JObject.Parse(text));
            }

            ctx.Response.StatusCode = (int) HttpStatusCode.OK;
        }

        public async Task Startup()
        {
            _listener.Start();
            await Task.Run(async () =>
            {
                while (true)
                {
                    var ctx = await _listener.GetContextAsync();
                    var request = ctx.Request;

                    switch (request.HttpMethod)
                    {
                        case "GET":
                            await HandleGet(ctx);
                            break;
                        case "POST":
                            await HandlePost(ctx);
                            break;
                        default:
                            ctx.Response.StatusCode = (int) HttpStatusCode.MethodNotAllowed;
                            Console.WriteLine("HTTP > Bad request type {0}", request.HttpMethod);
                            break;
                    }
                }
            });
        }
    }
}