namespace mcpserver.Rules;

public interface ICodeAnalysisRule
{
    List<CodeIssue> Analyze(string filePath, string fileContent);
}
