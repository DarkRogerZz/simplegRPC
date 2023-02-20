

using GrpcService.Services;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

// Additional configuration is required to successfully run gRPC on macOS.
// For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682

// Add services to the container.
builder.Services.AddGrpc();

builder.WebHost.ConfigureKestrel(opt =>
{
    var httpPort = builder.Configuration.GetValue<int>("port:http");

    var httpsPort = builder.Configuration.GetValue<int>("port:https");

    opt.Listen(IPAddress.Any, httpPort, listenOpt => listenOpt.UseConnectionLogging());
    opt.Listen(IPAddress.Any, httpsPort, listenOpt =>
    {
        var enableSsl = builder.Configuration.GetValue<bool>("enableSsl");
        if (enableSsl)
        {
            listenOpt.UseHttps("Certs\\cert.pfx","1234.com");
        }
        else
        {
            listenOpt.UseHttps();
        }
        listenOpt.UseConnectionLogging();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<TicketService>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();
