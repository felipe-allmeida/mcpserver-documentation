namespace mcpserver.Rules;

/// <summary>
/// Problema relacionado a lógica ou performance.
/// </summary>
public class LogicIssue : IssueBase
{
    public string IssueType { get; internal set; }
    public string CodeSnippet { get; internal set; }
    public string PatternName { get; internal set; }
    public string Complexity { get; internal set; }
}
