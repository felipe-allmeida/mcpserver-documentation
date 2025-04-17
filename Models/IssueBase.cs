namespace mcpserver.Rules;

/// <summary>
/// Problema de código base.
/// </summary>
public abstract class IssueBase
{
    /// <summary>
    /// Caminho do arquivo onde o problema foi encontrado.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;
    
    /// <summary>
    /// Número da linha onde o problema foi encontrado.
    /// </summary>
    public int LineNumber { get; set; }
    
    /// <summary>
    /// Severidade do problema.
    /// </summary>
    public IssueSeverity Severity { get; set; }
    
    /// <summary>
    /// Mensagem descritiva do problema.
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// Sugestão para correção do problema.
    /// </summary>
    public string Suggestion { get; set; } = string.Empty;
}
