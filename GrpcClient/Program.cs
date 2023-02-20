// See https://aka.ms/new-console-template for more information
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using GrpcService.Protos;
using System;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;



//GrpcTest.SayHello();
//GrpcTest.QueryTicket();
//GrpcTest.BuyTicket();
GrpcTest.StreamingBothWay();

Console.ReadLine();

public static class GrpcTest
{


    // 建立连接
    private static TicketServer.TicketServerClient CreateClient(bool enableSsl = false)
    {
        GrpcChannel channel;
        if (enableSsl)
        {
            string url = "https://localhost:7000";

            var handle = new HttpClientHandler();
            // 添加证书
            handle.ClientCertificates.Add(new X509Certificate2("Certs\\cert.pfx", "1234.com"));

            // 忽略证书
            handle.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            channel = GrpcChannel.ForAddress(url, new GrpcChannelOptions
            {
                HttpClient = new HttpClient(handle)
            });
        }
        else
        {
            string url = "http://localhost:5000";
            channel = GrpcChannel.ForAddress(url);
        }

        return new TicketServer.TicketServerClient(channel);
    }

    //调用就说hello
    public static async void SayHello()
    {

        Console.WriteLine("单次调用模式，参数:Dennis");
        var client = CreateClient(true);

        var result = await client.SayHelloAsync(new HelloRequest { Name = "Dennis" });

        Console.WriteLine($"服务器响应Message={result.Message}");
    }

    //服务端流模式
    public static async void QueryTicket()
    {
        Console.WriteLine("服务端流模式");
        var client = CreateClient(true);
        var result = client.QueryTicket(new TicketRequest
        {
            TicketInfo = new Struct
            {
                Fields =
                {
                    ["Name"] = Value.ForString("Den"),
                    ["Sex"] = Value.ForBool(true)
                }
            }
        }); ;

        await foreach (var resp in result.ResponseStream.ReadAllAsync())
        {

            Console.WriteLine($"服务器响应结果：{resp.Result}");
        }

    }

    //客户端流模式
    public static async void BuyTicket()
    {
        Console.WriteLine("客户端流模式");
        var client = CreateClient(true);
        var result = client.BuyTicket();

        for (int i = 0; i < 10; i++)
        {
            await result.RequestStream.WriteAsync(new TicketRequest
            {
                TicketInfo = new Struct
                {
                    Fields =
                {
                    ["Name"] = Value.ForString("Den"+i),
                    ["Sex"] = Value.ForBool(true)
                }
                }
            });
            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        await result.RequestStream.CompleteAsync();

        var resp = result.ResponseAsync.Result;
        Console.WriteLine($"服务器响应结果：{resp.Result}");
    }

    //双向流模式
    public static async void StreamingBothWay()
    {
        Console.WriteLine("双向流模式");
        var client = CreateClient();
        var result = client.StramingBothWay();

        // 发送请求
        for (var i = 0; i < 5; i++)
        {
            await result.RequestStream.WriteAsync(new TicketRequest
            {
                TicketInfo = new Struct
                {
                    Fields =
                {
                    ["Name"] = Value.ForString("Den"+i),
                    ["Sex"] = Value.ForBool(true)
                }
                }
            });
            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        // 处理响应
        var respTask = Task.Run(async () =>
        {
            await foreach (var resp in result.ResponseStream.ReadAllAsync())
            {
                Console.WriteLine($"Result={resp.Result}");
            }
        });

        // 等待请求发送完毕
        await result.RequestStream.CompleteAsync();

        // 等待响应处理
        await respTask;
    }
}