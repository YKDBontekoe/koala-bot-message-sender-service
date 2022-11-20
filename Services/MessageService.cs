using Azure.Messaging.ServiceBus;
using Discord;
using Discord.WebSocket;
using Koala.MessageSenderService.Models;
using Koala.MessageSenderService.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace Koala.MessageSenderService.Services;

public class MessageService : IMessageService
{
    private readonly BaseSocketClient _discordClient;
    private readonly ServiceBusClient _serviceBusClient;
    private readonly IConfiguration _configuration;
    private ServiceBusProcessor? _processor;

    public MessageService(BaseSocketClient discordClient, ServiceBusClient serviceBusClient, IConfiguration configuration)
    {
        _discordClient = discordClient;
        _serviceBusClient = serviceBusClient;
        _configuration = configuration;
    }

    public async Task InitializeAsync()
    {
        _processor = _serviceBusClient.CreateProcessor(_configuration["ServiceBus:QueueName"], new ServiceBusProcessorOptions
        {
            AutoCompleteMessages = true,
            MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(15),
            PrefetchCount = 100,
        });
        
        try
        {
            // add handler to process messages
            _processor.ProcessMessageAsync += MessagesHandler;

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
    
    private Task ErrorHandler(ProcessErrorEventArgs args)
    {
        // Process the error.
        Console.WriteLine(args.Exception.ToString());
        return Task.CompletedTask;
    }
}