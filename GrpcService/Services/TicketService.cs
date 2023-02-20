using Grpc.Core;
using GrpcService.Protos;
using Newtonsoft.Json;

namespace GrpcService.Services
{
    public class TicketService : TicketServer.TicketServerBase
    {
        private readonly ILogger<TicketService> _logger;

        public TicketService(ILogger<TicketService> logger)
        {
            _logger = logger;
        }

        //简单说个hello
        public override Task<HelloResponse> SayHello(HelloRequest request, ServerCallContext context)
        {
            _logger.LogInformation($"Saying hello to {request.Name}");
            return Task.FromResult(new HelloResponse
            {
                Message = $"Hello{request.Name}"
            });
        }

        //查询票价
        public override async Task QueryTicket(TicketRequest request, IServerStreamWriter<TicketResponse> responseStream, ServerCallContext context)
        {
            int price = 10;
            while (!context.CancellationToken.IsCancellationRequested)
            {
                price += 10;
                await responseStream.WriteAsync(new TicketResponse
                {
                    Result = $"票价为{price}"
                });

                await Task.Delay(TimeSpan.FromSeconds(3), context.CancellationToken);
            }
        }

        //买票
        public override async Task<TicketResponse> BuyTicket(IAsyncStreamReader<TicketRequest> requestStream, ServerCallContext context)
        {
            int num = 100000;
            string name = "";
            //处理请求
            await foreach (var req in requestStream.ReadAllAsync())
            {
                num--;

                Console.WriteLine($"剩余{num}");
                var user = JsonConvert.DeserializeObject<TicketInfo>(req.TicketInfo.ToString());
                name = user.Name;
                Console.WriteLine(name);

            }

            return new TicketResponse { Result = $"成功，最后用户：{name}，剩余：{num}" };
        }

        public override async Task StramingBothWay(IAsyncStreamReader<TicketRequest> requestStream, IServerStreamWriter<TicketResponse> responseStream, ServerCallContext context)
        {
            // 服务器响应客户端一次
            // 处理请求
            //await foreach (var req in requestStream.ReadAllAsync())
            //{
            //    Console.WriteLine(req);
            //}

            // 请求处理完成之后只响应一次
            //await responseStream.WriteAsync(new TicketResponse
            //{
            //    Code = 200,
            //    Result = true,
            //    Message = $"StramingBothWay 单次响应: {Guid.NewGuid()}"
            //});
            //await Task.Delay(TimeSpan.FromSeconds(3), context.CancellationToken);

            // 服务器响应客户端多次
            // 处理请求
            var readTask = Task.Run(async () =>
            {
                await foreach (var req in requestStream.ReadAllAsync())
                {
                    Console.WriteLine(req);
                }
            });

            // 请求未处理完之前一直响应
            while (!readTask.IsCompleted)
            {
                await responseStream.WriteAsync(new TicketResponse
                {
                   
                    Result = $"StramingBothWay 请求处理完之前的响应: {Guid.NewGuid()}"
                });
                await Task.Delay(TimeSpan.FromSeconds(3), context.CancellationToken);
            }

            // 也可以无限响应客户端
            //while (!context.CancellationToken.IsCancellationRequested)
            //{
            //    await responseStream.WriteAsync(new TicketResponse
            //    {
            //        Code = 200,
            //        Result = true,
            //        Message = $"StreamingFromServer 无限响应: {Guid.NewGuid()}"
            //    });
            //    await Task.Delay(TimeSpan.FromSeconds(3), context.CancellationToken);
            //}
        }
    }

    public class TicketInfo 
    {
        public string Name { get; set; }

        public bool Sex { get; set; }
    }
}
