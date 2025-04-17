using System.Text.Json;
using System.Text.RegularExpressions;

namespace mcpserver.Rules;

public class PerformanceOptimizationRule : ILogicAnalysisRule
{
    private readonly string _patternsDirectory;
    private List<PerformancePattern> _performancePatterns;
    
    public PerformanceOptimizationRule()
    {
        _patternsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Assets", "PerformancePatterns");
        LoadPerformancePatterns();
    }
    
    public List<LogicIssue> Analyze(string filePath, string fileContent)
    {
        var issues = new List<LogicIssue>();
        
        // Skip if no patterns loaded
        if (_performancePatterns == null || !_performancePatterns.Any())
        {
            return issues;
        }
        
        // For each pattern, analyze the file content
        foreach (var pattern in _performancePatterns)
        {
            issues.AddRange(AnalyzeWithPattern(filePath, fileContent, pattern));
        }
        
        return issues;
    }
    
    private void LoadPerformancePatterns()
    {
        _performancePatterns = new List<PerformancePattern>();
        
        if (!Directory.Exists(_patternsDirectory))
        {
            Console.WriteLine($"Warning: Performance patterns directory not found: {_patternsDirectory}");
            return;
        }
        
        foreach (var file in Directory.GetFiles(_patternsDirectory, "*.json"))
        {
            try
            {
                var jsonContent = File.ReadAllText(file);
                var pattern = JsonSerializer.Deserialize<PerformancePattern>(jsonContent, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                if (pattern != null)
                {
                    _performancePatterns.Add(pattern);
                    Console.WriteLine($"Loaded performance pattern: {pattern.PatternName}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading performance pattern from {file}: {ex.Message}");
            }
        }
    }
    
    private List<LogicIssue> AnalyzeWithPattern(string filePath, string fileContent, PerformancePattern pattern)
    {
        var issues = new List<LogicIssue>();
        
        foreach (var rule in pattern.Rules)
        {
            switch (rule.Type)
            {
                case "Algoritmo":
                    issues.AddRange(AnalyzeAlgorithm(filePath, fileContent, pattern, rule));
                    break;
                case "DataStructure":
                    issues.AddRange(AnalyzeDataStructure(filePath, fileContent, pattern, rule));
                    break;
                case "ResourceUsage":
                    issues.AddRange(AnalyzeResourceUsage(filePath, fileContent, pattern, rule));
                    break;
            }
        }
        
        return issues;
    }
    
    private List<LogicIssue> AnalyzeAlgorithm(string filePath, string fileContent, PerformancePattern pattern, PerformanceRule rule)
    {
        var issues = new List<LogicIssue>();
        
        if (!string.IsNullOrEmpty(rule.Pattern))
        {
            var regex = new Regex(rule.Pattern, RegexOptions.Compiled | RegexOptions.Multiline);
            var matches = regex.Matches(fileContent);
            
            foreach (Match match in matches)
            {
                var issue = CreateLogicIssue(filePath, fileContent, pattern, rule, match);
                issues.Add(issue);
            }
        }
        
        return issues;
    }
    
    private List<LogicIssue> AnalyzeDataStructure(string filePath, string fileContent, PerformancePattern pattern, PerformanceRule rule)
    {
        var issues = new List<LogicIssue>();
        
        if (rule.AntiPatterns != null && rule.AntiPatterns.Any())
        {
            foreach (var antiPattern in rule.AntiPatterns)
            {
                var regex = new Regex($"new {antiPattern}", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                var matches = regex.Matches(fileContent);
                
                foreach (Match match in matches)
                {
                    var recommendedStructures = string.Join(", ", rule.RecommendedStructures ?? new List<string>());
                    var suggestion = !string.IsNullOrEmpty(recommendedStructures) 
                        ? $"Considere usar: {recommendedStructures}" 
                        : rule.Suggestion;
                        
                    var issue = CreateLogicIssue(filePath, fileContent, pattern, rule, match, suggestion);
                    issues.Add(issue);
                }
            }
        }
        
        return issues;
    }
    
    private List<LogicIssue> AnalyzeResourceUsage(string filePath, string fileContent, PerformancePattern pattern, PerformanceRule rule)
    {
        var issues = new List<LogicIssue>();
        
        if (!string.IsNullOrEmpty(rule.Pattern))
        {
            var regex = new Regex(rule.Pattern, RegexOptions.Compiled | RegexOptions.Multiline);
            var matches = regex.Matches(fileContent);
            
            foreach (Match match in matches)
            {
                var issue = CreateLogicIssue(filePath, fileContent, pattern, rule, match);
                issues.Add(issue);
            }
        }
        
        return issues;
    }
    
    private LogicIssue CreateLogicIssue(string filePath, string fileContent, PerformancePattern pattern, PerformanceRule rule, Match match, string overrideSuggestion = null)
    {
        // Find the line number for the issue
        int lineNumber = 1;
        int position = 0;
        
        for (int i = 0; i < match.Index; i++)
        {
            if (fileContent[i] == '\n')
            {
                lineNumber++;
                position = i + 1;
            }
        }
        
        // Construct the line of code with the issue
        string lineOfCode = string.Empty;
        int lineStart = Math.Max(0, match.Index);
        while (lineStart > 0 && fileContent[lineStart - 1] != '\n')
        {
            lineStart--;
        }
        
        int lineEnd = Math.Min(fileContent.Length - 1, match.Index + match.Length);
        while (lineEnd < fileContent.Length - 1 && fileContent[lineEnd + 1] != '\n')
        {
            lineEnd++;
        }
        
        if (lineStart <= lineEnd)
        {
            lineOfCode = fileContent.Substring(lineStart, lineEnd - lineStart + 1).Trim();
        }
        
        // Create the issue with the pattern information
        var suggestion = overrideSuggestion ?? rule.Suggestion;
        
        return new LogicIssue
        {
            FilePath = filePath,
            LineNumber = lineNumber,
            Message = $"{rule.Message}",
            Severity = ParseSeverity(pattern.Severity),
            IssueType = "Performance",
            Suggestion = suggestion,
            CodeSnippet = lineOfCode,
            PatternName = rule.Name ?? pattern.PatternName,
            Complexity = rule.Complexity
        };
    }
    
    private IssueSeverity ParseSeverity(string severity)
    {
        return severity?.ToLower() switch
        {
            "error" => IssueSeverity.Error,
            "warning" => IssueSeverity.Warning,
            "info" => IssueSeverity.Info,
            _ => IssueSeverity.Warning
        };
    }
    
    // Internal classes to match the JSON structure of pattern definitions
    private class PerformancePattern
    {
        public string PatternName { get; set; }
        public string Description { get; set; }
        public string Severity { get; set; }
        public List<PerformanceRule> Rules { get; set; }
        public CodeExamples CodeExamples { get; set; }
    }
    
    private class PerformanceRule
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public string Pattern { get; set; }
        public string Complexity { get; set; }
        public string Message { get; set; }
        public string Suggestion { get; set; }
        public List<string> RecommendedStructures { get; set; }
        public List<string> AntiPatterns { get; set; }
    }
    
    private class CodeExamples
    {
        public string Good { get; set; }
        public string Bad { get; set; }
    }
}
