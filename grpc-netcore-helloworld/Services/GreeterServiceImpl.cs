using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using grpc_netcore_helloworld_proto;

namespace grpc_netcore_helloworld
{
    public class GreeterServiceImpl : GreeterService.GreeterServiceBase
    {
        private readonly ILogger<GreeterServiceImpl> _logger;

        public GreeterServiceImpl(ILogger<GreeterServiceImpl> logger)
        {
            _logger = logger;
        }

        public override Task<HelloResponse> SayHelloUnary(HelloRequest request, ServerCallContext context)
        {
            _logger.LogInformation("server received {}", request);
            return Task.FromResult(new HelloResponse()
            {
                Message = "Hello " + request.FirstName + " " + request.LastName
            });
        }

        public override async Task SayHelloBidi(IAsyncStreamReader<HelloRequest> reqStream, IServerStreamWriter<HelloResponse> respStream, ServerCallContext context)
        {
            var timer = new Timer(5000);
            timer.Elapsed += OnTimedEvent;

            List<HelloResponse> responses = new List<HelloResponse>();
            
            async void OnTimedEvent(object source, ElapsedEventArgs e)
            {
                await respStream.WriteAsync(new HelloResponse() {Message = "Wake up!!"});
                timer.Enabled = false;
                timer.Close();
            }

            try
            {
                while (await reqStream.MoveNext())
                {
                    timer.Enabled = true;
                    var request = reqStream.Current;
                    _logger.LogInformation("server received {}", request);
                    responses.Add(new HelloResponse() { Message = "Hello " + request.FirstName + " " + request.LastName });
                    await respStream.WriteAsync(new HelloResponse()
                        {Message = "Received data from client, total: " + responses.Count});
                }
            }
            finally
            {
                foreach (var response in responses)
                    await respStream.WriteAsync(response);   
            }
        }
    }
}