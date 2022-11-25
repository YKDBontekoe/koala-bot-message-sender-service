using Azure.Messaging.ServiceBus;
using Discord;
using Discord.WebSocket;
using Koala.MessageSenderService.Models;
using Koala.MessageSenderService.Options;
using Koala.MessageSenderService.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Koala.MessageSenderService.Services;

public class MessageService : IMessageService
{
    private readonly DiscordSocketClient _discordClient;
    private readonly ServiceBusProcessor? _processor;
    private readonly DiscordOptions _discordOptions;

    public MessageService(DiscordSocketClient discordClient, ServiceBusClient serviceBusClient, IOptions<ServiceBusOptions> serviceBusOptions, IOptions<DiscordOptions> discordOptions)
    {
        _discordClient = discordClient;
        _discordOptions = discordOptions != null ? discordOptions.Value : throw new ArgumentNullException(nameof(discordOptions));
        _processor = serviceBusClient.CreateProcessor(serviceBusOptions.Value.QueueName, new ServiceBusProcessorOptions());
        
        InitializeDiscordClient();
    }

    public async Task InitializeAsync()
    {
        try
        {
            // add handler to process messages
            _processor!.ProcessMessageAsync += MessagesHandler;

            // add handler to process any errors
            _processor.ProcessErrorAsync += ErrorHandler;
            await _processor.StartProcessingAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    public async Task DisposeAsync()
    {
        await _processor!.DisposeAsync();
    }
    
    private async Task MessagesHandler(ProcessMessageEventArgs args)
    {
        // Process the message.
        var body = args.Message.Body.ToString();
        if (string.IsNullOrEmpty(body)) return;

        var message = JsonConvert.DeserializeObject<SendMessage>(body);
        if (message is null) return;

        if (_discordClient.GetChannel(message.ChannelId) is not SocketTextChannel channel) return;
        
        if (message.IsReaction && message.OriginalMessageId.HasValue)
        {
            await channel!.GetMessageAsync(message.OriginalMessageId.Value).ContinueWith(async task =>
            {
                var result = await task;
                await result.AddReactionAsync(new Emoji(message.Content));
            });
            return;
        }
        
        await channel.SendMessageAsync(message.Content);
    }
    
    private static Task ErrorHandler(ProcessErrorEventArgs args)
    {
        // Process the error.
        Console.WriteLine(args.Exception.ToString());
        return Task.CompletedTask;
    }
    
    // Initialize the Discord client and connect to the gateway
    private void InitializeDiscordClient()
    {
        _discordClient.LoginAsync(TokenType.Bot, _discordOptions.Token);
        _discordClient.StartAsync();
    }
}