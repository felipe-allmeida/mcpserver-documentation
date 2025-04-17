using System;

namespace mcpserver.Rules;

public class DocumentationQualityRule : IDocumentationAnalysisRule
{
    public List<DocumentationIssue> Analyze(string filePath, string fileContent)
    {
        // Implementação da análise de qualidade da documentação
        return new List<DocumentationIssue>();
    }
}
