using System;
using System.Text.RegularExpressions;

namespace mcpserver.Rules;

public class CodeSmellRule : ILogicAnalysisRule
{
    public List<LogicIssue> Analyze(string filePath, string fileContent)
    {
        var issues = new List<LogicIssue>();
        
        // Verifica métodos muito longos (code smell)
        var methodRegex = new Regex(@"(public|private|protected|internal)\s+\w+\s+\w+\s*\([^)]*\)\s*{", 
            RegexOptions.Compiled);
        
        var methodMatches = methodRegex.Matches(fileContent);
        
        foreach (Match methodMatch in methodMatches)
        {
            var methodStart = methodMatch.Index + methodMatch.Length;
            var methodEndPos = FindMatchingCloseBrace(fileContent, methodStart);
            
            if (methodEndPos > methodStart)
            {
                var methodContent = fileContent.Substring(methodStart, methodEndPos - methodStart);
                var lineCount = methodContent.Count(c => c == '\n');
                
                if (lineCount > 30)
                {
                    issues.Add(new LogicIssue
                    {
                        FilePath = filePath,
                        LineNumber = GetLineNumber(fileContent, methodMatch.Index),
                        Severity = IssueSeverity.Warning,
                        Message = $"Método muito longo com {lineCount} linhas",
                        Suggestion = "Considere refatorar este método em métodos menores e mais específicos"
                    });
                }
            }
        }
        
        return issues;
    }
    
    private int FindMatchingCloseBrace(string content, int startPos)
    {
        int depth = 1;
        for (int i = startPos; i < content.Length; i++)
        {
            if (content[i] == '{') depth++;
            else if (content[i] == '}') depth--;
            
            if (depth == 0) return i;
        }
        
        return -1;
    }
    
    private int GetLineNumber(string content, int position)
    {
        return content.Substring(0, position).Count(c => c == '\n') + 1;
    }
}

