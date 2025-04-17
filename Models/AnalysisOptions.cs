using System;
using System.Collections.Generic;

namespace mcpserver.Rules;

public class AnalysisOptions
{
    /// <summary>
    /// Indica se deve analisar padrões de código.
    /// </summary>
    public bool AnalyzeCode { get; set; } = true;
    
    /// <summary>
    /// Indica se deve analisar documentação.
    /// </summary>
    public bool AnalyzeDocumentation { get; set; } = true;
    
    /// <summary>
    /// Indica se deve analisar lógica.
    /// </summary>
    public bool AnalyzeLogic { get; set; } = true;
    
    /// <summary>
    /// Custom file or directory patterns to exclude from analysis
    /// </summary>
    public List<string> ExclusionPatterns { get; set; } = new();
}
