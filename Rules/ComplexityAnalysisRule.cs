using System;

namespace mcpserver.Rules;

public class ComplexityAnalysisRule : ILogicAnalysisRule
{
    public List<LogicIssue> Analyze(string filePath, string fileContent)
    {
        // Implementação da análise de complexidade ciclomática
        return new List<LogicIssue>();
    }
}
