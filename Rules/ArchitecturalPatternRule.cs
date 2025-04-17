using System.Text.Json;
using System.Text.RegularExpressions;

namespace mcpserver.Rules;

public class ArchitecturalPatternRule : ICodeAnalysisRule
{
    private readonly string _patternsDirectory;
    private List<PatternDefinition> _patternDefinitions;
    
    public ArchitecturalPatternRule()
    {
        _patternsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Assets", "ArchitecturalPatterns");
        LoadPatternDefinitions();
    }
    
    public List<CodeIssue> Analyze(string filePath, string fileContent)
    {
        var issues = new List<CodeIssue>();
        
        // Skip if no patterns loaded
        if (_patternDefinitions == null || !_patternDefinitions.Any())
        {
            return issues;
        }
        
        // For each pattern, check if this file should be analyzed against it
        foreach (var pattern in _patternDefinitions)
        {
            if (ShouldApplyPattern(filePath, fileContent, pattern))
            {
                issues.AddRange(AnalyzeWithPattern(filePath, fileContent, pattern));
            }
        }
        
        return issues;
    }
    
    private void LoadPatternDefinitions()
    {
        _patternDefinitions = new List<PatternDefinition>();
        
        if (!Directory.Exists(_patternsDirectory))
        {
            Console.WriteLine($"Warning: Pattern directory not found: {_patternsDirectory}");
            return;
        }
        
        foreach (var file in Directory.GetFiles(_patternsDirectory, "*.json"))
        {
            try
            {
                var jsonContent = File.ReadAllText(file);
                var pattern = JsonSerializer.Deserialize<PatternDefinition>(jsonContent, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                if (pattern != null)
                {
                    _patternDefinitions.Add(pattern);
                    Console.WriteLine($"Loaded pattern: {pattern.PatternName}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading pattern from {file}: {ex.Message}");
            }
        }
    }
    
    private bool ShouldApplyPattern(string filePath, string fileContent, PatternDefinition pattern)
    {
        // If the pattern has indicators, check if any are present in the file
        if (pattern.PatternDetection != null)
        {
            // Get all indicators from the pattern detection property
            var allIndicators = new List<string>();
            
            // Extract indicators from each property in PatternDetection
            foreach (var prop in pattern.PatternDetection.GetType().GetProperties())
            {
                var value = prop.GetValue(pattern.PatternDetection);
                if (value is List<string> indicators)
                {
                    allIndicators.AddRange(indicators);
                }
            }
            
            // Check if any indicator is present in the file
            return allIndicators.Any(indicator => 
                fileContent.Contains(indicator, StringComparison.OrdinalIgnoreCase) ||
                Path.GetFileName(filePath).Contains(indicator, StringComparison.OrdinalIgnoreCase));
        }
        
        // If no indicators defined, apply to all files
        return true;
    }
    
    private List<CodeIssue> AnalyzeWithPattern(string filePath, string fileContent, PatternDefinition pattern)
    {
        var issues = new List<CodeIssue>();
        
        foreach (var rule in pattern.Rules)
        {
            switch (rule.Type)
            {
                case "Structure":
                    issues.AddRange(AnalyzeStructure(filePath, fileContent, pattern, rule));
                    break;
                case "Naming":
                    issues.AddRange(AnalyzeNaming(filePath, fileContent, pattern, rule));
                    break;
                case "Dependency":
                case "LayerDependency":
                    issues.AddRange(AnalyzeDependencies(filePath, fileContent, pattern, rule));
                    break;
                case "Responsibility":
                    issues.AddRange(AnalyzeResponsibilities(filePath, fileContent, pattern, rule));
                    break;
            }
        }
        
        return issues;
    }
    
    private List<CodeIssue> AnalyzeStructure(string filePath, string fileContent, PatternDefinition pattern, Rule rule)
    {
        var issues = new List<CodeIssue>();
        
        // Simple implementation - check if class Structure matches expected identifiers
        if (rule.Identifiers != null)
        {
            foreach (var identifier in rule.Identifiers)
            {
                var key = identifier.Key;
                var regex = new Regex(identifier.Value);
                
                var className = Path.GetFileNameWithoutExtension(filePath);
                if (regex.IsMatch(className))
                {
                    // This file is identified as a specific component type (e.g., Controller)
                    // We should check if it follows the structure rules for that component
                    // For now, this is a placeholder for more detailed analysis
                }
            }
        }
        
        // Check component types and their responsibilities
        if (rule.ComponentType != null)
        {
            foreach (var component in rule.ComponentType)
            {
                var componentType = component.Key;
                var responsibilities = component.Value;
                
                // Check if this file is of this component type
                // For now, just use filename as a simple heuristic
                if (Path.GetFileNameWithoutExtension(filePath).Contains(componentType, StringComparison.OrdinalIgnoreCase))
                {
                    // Check if responsibilities are fulfilled
                    // This is a simplified check - in a real implementation, you'd do deeper analysis
                    bool issueFound = false;
                    foreach (var responsibility in responsibilities)
                    {
                        // Simple check: see if there's any indication the responsibility is being fulfilled
                        if (!fileContent.Contains(responsibility.Replace("Should ", ""), StringComparison.OrdinalIgnoreCase))
                        {
                            issueFound = true;
                        }
                    }
                    
                    if (issueFound)
                    {
                        issues.Add(new CodeIssue
                        {
                            FilePath = filePath,
                            LineNumber = 1, // Would need deeper parsing to get exact line
                            Message = $"{rule.Message} - {componentType} component missing expected responsibilities",
                            Severity = ParseSeverity(pattern.Severity)
                        });
                    }
                }
            }
        }
        
        return issues;
    }
    
    private List<CodeIssue> AnalyzeNaming(string filePath, string fileContent, PatternDefinition pattern, Rule rule)
    {
        var issues = new List<CodeIssue>();
        
        if (rule.Pattern != null)
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            
            foreach (var namingPattern in rule.Pattern)
            {
                var componentType = namingPattern.Key;
                var regex = new Regex(namingPattern.Value);
                
                // Check if this file name indicates it's a particular component type
                if (fileName.Contains(componentType, StringComparison.OrdinalIgnoreCase) && !regex.IsMatch(fileName))
                {
                    issues.Add(new CodeIssue
                    {
                        FilePath = filePath,
                        LineNumber = 1,
                        Message = $"{rule.Message} - {fileName} should match pattern {namingPattern.Value}",
                        Severity = ParseSeverity(pattern.Severity)
                    });
                }
            }
        }
        
        return issues;
    }
    
    private List<CodeIssue> AnalyzeDependencies(string filePath, string fileContent, PatternDefinition pattern, Rule rule)
    {
        var issues = new List<CodeIssue>();
        
        // For LayerDependency type, check if the file respects the layer dependencies
        if (rule.Type == "LayerDependency" && rule.Layers != null)
        {
            // Identify which layer this file belongs to
            var currentLayer = IdentifyLayer(filePath, fileContent, rule.Layers);
            if (currentLayer != null)
            {
                // Extract all using statements and other references
                var dependencies = ExtractDependencies(fileContent);
                
                // For each dependency, check if it's allowed
                foreach (var dependency in dependencies)
                {
                    // Find which layer the dependency belongs to
                    var dependencyLayer = rule.Layers.FirstOrDefault(l => 
                        l.Identifiers.Any(id => Regex.IsMatch(dependency, id)));
                    
                    if (dependencyLayer != null && 
                        !currentLayer.AllowedDependencies.Contains(dependencyLayer.Name))
                    {
                        issues.Add(new CodeIssue
                        {
                            FilePath = filePath,
                            LineNumber = 1, // Would need deeper parsing for exact line
                            Message = $"{rule.Message} - {currentLayer.Name} layer should not depend on {dependencyLayer.Name}",
                            Severity = ParseSeverity(pattern.Severity)
                        });
                    }
                }
            }
        }
        
        // For regular Dependency type
        if (rule.Rules != null)
        {
            foreach (var dependencyRule in rule.Rules)
            {
                var componentType = dependencyRule.Key;
                var restrictions = dependencyRule.Value;
                
                // Simple check: if filename indicates component type
                if (Path.GetFileNameWithoutExtension(filePath).Contains(componentType, StringComparison.OrdinalIgnoreCase))
                {
                    // Check if any dependency restrictions are violated
                    foreach (var restriction in restrictions)
                    {
                        // This is a simplified check
                        if (restriction.Contains("should not") && 
                            fileContent.Contains(restriction.Replace("should not ", ""), StringComparison.OrdinalIgnoreCase))
                        {
                            issues.Add(new CodeIssue
                            {
                                FilePath = filePath,
                                LineNumber = 1, // Would need deeper parsing
                                Message = $"{rule.Message} - {restriction}",
                                Severity = ParseSeverity(pattern.Severity)
                            });
                        }
                    }
                }
            }
        }
        
        return issues;
    }
    
    private List<CodeIssue> AnalyzeResponsibilities(string filePath, string fileContent, PatternDefinition pattern, Rule rule)
    {
        var issues = new List<CodeIssue>();
        
        if (rule.Rules != null)
        {
            foreach (var responsibilityRule in rule.Rules)
            {
                var componentType = responsibilityRule.Key;
                var responsibilities = responsibilityRule.Value;
                
                // Check if this file represents the component type
                if (Path.GetFileNameWithoutExtension(filePath).Contains(componentType, StringComparison.OrdinalIgnoreCase))
                {
                    // For each responsibility, check if there's evidence it's being followed
                    foreach (var responsibility in responsibilities)
                    {
                        // For responsibilities that say "should not"
                        if (responsibility.Contains("should not"))
                        {
                            var prohibited = responsibility.Replace("should not ", "");
                            if (fileContent.Contains(prohibited, StringComparison.OrdinalIgnoreCase))
                            {
                                issues.Add(new CodeIssue
                                {
                                    FilePath = filePath,
                                    LineNumber = 1, // Would need deeper parsing
                                    Message = $"{rule.Message} - {componentType} {responsibility}",
                                    Severity = ParseSeverity(pattern.Severity)
                                });
                            }
                        }
                    }
                }
            }
        }
        
        return issues;
    }
    
    private Layer IdentifyLayer(string filePath, string fileContent, List<Layer> layers)
    {
        foreach (var layer in layers)
        {
            if (layer.Identifiers.Any(id => 
                Regex.IsMatch(filePath, id) || 
                Regex.IsMatch(fileContent, id)))
            {
                return layer;
            }
        }
        
        return null;
    }
    
    private List<string> ExtractDependencies(string fileContent)
    {
        var dependencies = new List<string>();
        
        // Extract using statements
        var usingRegex = new Regex(@"using\s+([^;]+);", RegexOptions.Multiline);
        var matches = usingRegex.Matches(fileContent);
        
        foreach (Match match in matches)
        {
            if (match.Groups.Count > 1)
            {
                dependencies.Add(match.Groups[1].Value.Trim());
            }
        }
        
        // Could add more sophisticated dependency extraction here
        
        return dependencies;
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
    private class PatternDefinition
    {
        public string PatternName { get; set; }
        public string Description { get; set; }
        public string Severity { get; set; }
        public List<Rule> Rules { get; set; }
        public PatternDetection PatternDetection { get; set; }
    }
    
    private class Rule
    {
        public string Type { get; set; } = string.Empty;
        public string ExpectedPattern { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public Dictionary<string, string> Identifiers { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> Pattern { get; set; } = new Dictionary<string, string>();
        public List<string> AllowedDependencies { get; set; } = new List<string>();
        public Dictionary<string, List<string>> Rules { get; set; } = new Dictionary<string, List<string>>();
        public Dictionary<string, List<string>> ComponentType { get; set; } = new Dictionary<string, List<string>>();
        public List<Layer> Layers { get; set; } = new List<Layer>();
    }
    
    private class Layer
    {
        public string Name { get; set; } = string.Empty;
        public List<string> Identifiers { get; set; } = new List<string>();
        public List<string> AllowedDependencies { get; set; } = new List<string>();
    }
    
    private class PatternDetection
    {
        public List<string> MvcIndicators { get; set; } = new List<string>();
        public List<string> RepositoryIndicators { get; set; } = new List<string>();
        public List<string> CleanArchitectureIndicators { get; set; } = new List<string>();
    }
}
