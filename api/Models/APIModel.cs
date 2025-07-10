namespace AgentHub.Api.Models
{
    public record ChatThreadRequest(string Message, string? ThreadId = null, IEnumerable<ChatFile>? Files = null);

    public record ChatFile(string Name, string DataUrl);

    public record ChatChunkResponse(ChatChunkContentType ContentType, string Content, ChatChunkResponseResult? FinalResult = null);

    public record ChatChunkResponseResult(string Answer, string ThreadId = null, string Error = null);

    public enum ChatChunkContentType
    {
        Text,
        Image,
        Code
    }
}
