namespace Koala.MessageSenderService.Services.Interfaces;

public interface IMessageService
{
    Task InitializeAsync();
    Task DisposeAsync();
}