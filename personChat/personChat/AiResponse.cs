public class AiResponse
{
    /// <summary>
    /// The response from the AI model
    /// </summary>
    /// <example>I am Albert Einstein, a theoretical physicist who developed the theory of relativity.</example>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// The index of the MD file used for the system prompt
    /// </summary>
    /// <example>0</example>
    public int FileIndex { get; set; }
    
    /// <summary>
    /// The name of the MD file used for the system prompt
    /// </summary>
    /// <example>einstein.md</example>
    public string FileName { get; set; } = string.Empty;
}