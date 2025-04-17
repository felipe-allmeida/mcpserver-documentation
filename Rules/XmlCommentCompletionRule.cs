using System;
using System.Text.RegularExpressions;

namespace mcpserver.Rules;

public class XmlCommentCompletionRule : IDocumentationAnalysisRule
{
    public List<DocumentationIssue> Analyze(string filePath, string fileContent)
    {
        var issues = new List<DocumentationIssue>();
        
        // Verifica se classes públicas possuem documentação XML
        var classRegex = new Regex(@"public\s+class\s+(\w+)");
        var classMatches = classRegex.Matches(fileContent);
        
        foreach (Match match in classMatches)
        {
            var classPos = match.Index;
            var prevContent = fileContent.Substring(Math.Max(0, classPos - 500), 
                Math.Min(500, classPos));
            
            if (!prevContent.Contains("/// <summary>"))
            {
                issues.Add(new DocumentationIssue
                {
                    FilePath = filePath,
                    LineNumber = GetLineNumber(fileContent, classPos),
                    Severity = IssueSeverity.Warning,
                    Message = $"A classe '{match.Groups[1].Value}' não possui documentação XML completa",
                    Suggestion = "Adicione comentários XML (///) com as tags <summary>, <remarks> se necessário"
                });
            }
        }
        
        return issues;
    }
    
    private int GetLineNumber(string content, int position)
    {
        return content.Substring(0, position).Count(c => c == '\n') + 1;
    }
}
