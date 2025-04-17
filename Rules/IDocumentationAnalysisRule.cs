namespace mcpserver.Rules;

public interface IDocumentationAnalysisRule
{
    List<DocumentationIssue> Analyze(string filePath, string fileContent);
}
