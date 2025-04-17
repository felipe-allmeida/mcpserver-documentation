namespace mcpserver.Rules;

public interface ILogicAnalysisRule
{
    List<LogicIssue> Analyze(string filePath, string fileContent);
}