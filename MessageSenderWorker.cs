using Koala.MessageSenderService.Services.Interfaces;
using Microsoft.Extensions.Hosting;

namespace Koala.MessageSenderService;

public class MessageSenderWorker : IHostedService, IDisposable
{
    private readonly IMessageService _messageService;

    public MessageSenderWorker(IMessageService messageService)
    {
        _messageService = messageService;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _messageService.InitializeAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _messageService.DisposeAsync();
    }

    public async void Dispose()
    {
        await _messageService.DisposeAsync();
    }
}