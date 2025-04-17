namespace mcpserver.Rules;

/// <summary>
/// Resultado da análise de código.
/// </summary>
public class AnalysisResult
{
    /// <summary>
    /// Caminho do projeto analisado.
    /// </summary>
    public string ProjectPath { get; set; } = string.Empty;
    
    /// <summary>
    /// Lista de problemas de código encontrados.
    /// </summary>
    public List<CodeIssue> CodeIssues { get; set; } = new();
    
    /// <summary>
    /// Lista de problemas de documentação encontrados.
    /// </summary>
    public List<DocumentationIssue> DocumentationIssues { get; set; } = new();
    
    /// <summary>
    /// Lista de problemas de lógica encontrados.
    /// </summary>
    public List<LogicIssue> LogicIssues { get; set; } = new();
    
    /// <summary>
    /// Mensagem de erro, se houver.
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;
    
    /// <summary>
    /// Resumo da análise.
    /// </summary>
    public string Summary { get; set; } = string.Empty;
    
    /// <summary>
    /// Data e hora da análise.
    /// </summary>
    public DateTime Timestamp { get; set; }
    
    /// <summary>
    /// Número total de problemas encontrados.
    /// </summary>
    public int TotalIssueCount => CodeIssues.Count + DocumentationIssues.Count + LogicIssues.Count;
    
    /// <summary>
    /// Número total de arquivos analisados.
    /// </summary>
    public int TotalFilesAnalyzed { get; set; }
    
    /// <summary>
    /// Número total de linhas de código analisadas.
    /// </summary>
    public int TotalLinesOfCode { get; set; }
    
    /// <summary>
    /// Estatísticas de severidade dos problemas encontrados.
    /// </summary>
    public Dictionary<string, int> SeverityStats { get; set; } = new();
}
