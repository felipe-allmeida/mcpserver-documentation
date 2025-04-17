using System.Text.RegularExpressions;
using mcpserver.Rules;

namespace mcpserver.Rules;

public class NamingConventionRule : ICodeAnalysisRule
{
    public List<CodeIssue> Analyze(string filePath, string fileContent)
    {
        var issues = new List<CodeIssue>();
        
        // Verifica convenções de nomenclatura para classes (PascalCase)
        var classRegex = new Regex(@"class\s+([a-z][A-Za-z0-9]*)");
        var classMatches = classRegex.Matches(fileContent);
        
        foreach (Match match in classMatches)
        {
            issues.Add(new CodeIssue
            {
                FilePath = filePath,
                LineNumber = GetLineNumber(fileContent, match.Index),
                Severity = IssueSeverity.Warning,
                Message = $"A classe '{match.Groups[1].Value}' não segue o padrão PascalCase",
                Suggestion = $"Renomeie para '{char.ToUpper(match.Groups[1].Value[0])}{match.Groups[1].Value.Substring(1)}'"
            });
        }
        
        // Verifica convenções de nomenclatura para métodos (PascalCase)
        // Outras verificações...
        
        return issues;
    }
    
    private int GetLineNumber(string content, int position)
    {
        return content.Substring(0, position).Count(c => c == '\n') + 1;
    }
}
