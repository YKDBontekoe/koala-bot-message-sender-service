using Azure.Messaging.ServiceBus;
using Discord;
using Discord.WebSocket;
using Koala.MessageSenderService.Services;
using Koala.MessageSenderService.Services.Interfaces;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Koala.MessageSenderService;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        var host = Host
            .CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                var config = new DiscordSocketConfig
                {
                    AlwaysDownloadUsers = true,
                    GatewayIntents = GatewayIntents.All
                };
                var client = new DiscordSocketClient(config);

                client.LoginAsync(TokenType.Bot, hostContext.Configuration["Discord:Token"]);
                client.StartAsync();
                
                services.AddAzureClients(builder =>
                {
                    builder.AddServiceBusClient(hostContext.Configuration["ServiceBus:ConnectionString"]);
                });

                services.AddScoped<IMessageService>(provider =>
                {
                    var serviceBusClient = provider.GetRequiredService<ServiceBusClient>();
                    return new MessageService(client, serviceBusClient, hostContext.Configuration);
                });
                
                services.AddHostedService<MessageSenderWorker>();
            })
            .UseConsoleLifetime()
            .Build();

        await host.RunAsync();
    }
}