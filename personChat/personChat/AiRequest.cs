public class AiRequest
{
    /// <summary>
    /// The message to send to the AI
    /// </summary>
    /// <example>Can you guess who I am?</example>
    public string? UserMessage { get; set; }
    
    /// <summary>
    /// Optional index of the MD file to use as system prompt.
    /// If not provided, a random file will be selected.
    /// </summary>
    /// <example>0</example>
    public int? FileIndex { get; set; }
}