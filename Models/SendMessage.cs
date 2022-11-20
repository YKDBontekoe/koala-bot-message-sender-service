namespace Koala.MessageSenderService.Models;

public class SendMessage
{
    public ulong ChannelId { get; set; }
    public string Content { get; set; }
    public bool IsReaction { get; set; } = false;

    public ulong? OriginalMessageId { get; set; } = default;
}