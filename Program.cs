using Azure.Messaging.ServiceBus;
using Discord;
using Discord.WebSocket;
using Koala.MessageSenderService.Options;
using Koala.MessageSenderService.Services;
using Koala.MessageSenderService.Services.Interfaces;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Koala.MessageSenderService;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        var host = Host
            .CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, builder) =>
                {
                    var env = context.HostingEnvironment;

                    builder
                        .SetBasePath(env.ContentRootPath)
                        .AddJsonFile("appsettings.json", true, true)
                        .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true, true)
                        .AddEnvironmentVariables();
                }
            )
            .ConfigureServices((hostContext, services) =>
            {
                ConfigureOptions(services, hostContext.Configuration);
                ConfigureDiscordClient(services);
                ConfigureServiceBus(services);
                
                services.AddTransient<IMessageService, MessageService>();
                services.AddHostedService<MessageSenderWorker>();
            })
            .UseConsoleLifetime()
            .Build();

        await host.RunAsync();
    }
    
    // Configure options for the application to use in the services
    private static void ConfigureOptions(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions();
        services.Configure<ServiceBusOptions>(configuration.GetSection(ServiceBusOptions.ServiceBus));
        services.Configure<DiscordOptions>(configuration.GetSection(DiscordOptions.Discord));
    }
    
    // Configure Discord client and add it to the service collection
    private static void ConfigureDiscordClient(IServiceCollection services)
    {
        var config = new DiscordSocketConfig
        {
            AlwaysDownloadUsers = true,
            GatewayIntents = GatewayIntents.All
        };

        services.AddSingleton(_ => new DiscordSocketClient(config));
    }

    // Configure the Azure Service Bus client with the connection string
    private static void ConfigureServiceBus(IServiceCollection services)
    {
        services.AddAzureClients(builder =>
        {
            builder.AddServiceBusClient(services.BuildServiceProvider().GetRequiredService<IOptions<ServiceBusOptions>>().Value.ConnectionString);
        });
    }
}