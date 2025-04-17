using System.Text.Json;
using System.Text.RegularExpressions;

namespace mcpserver.Rules;

public class ProjectStructureRule : ICodeAnalysisRule
{
    private readonly string _patternsDirectory;
    private readonly string _projectRootDirectory;
    private List<ProjectPattern> _projectPatterns;
    private Dictionary<string, bool> _foldersCache;
    
    public ProjectStructureRule()
    {
        _patternsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Assets", "ProjectStructure");
        _projectRootDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..");
        _foldersCache = new Dictionary<string, bool>();
        LoadProjectPatterns();
    }
    
    public List<CodeIssue> Analyze(string filePath, string fileContent)
    {
        var issues = new List<CodeIssue>();
        
        // Skip if no patterns loaded
        if (_projectPatterns == null || !_projectPatterns.Any())
        {
            return issues;
        }
        
        // Skip files in exempted paths
        foreach (var pattern in _projectPatterns)
        {
            if (ShouldExempt(filePath, pattern.ExemptionPatterns))
            {
                continue;
            }
            
            issues.AddRange(AnalyzeWithPattern(filePath, fileContent, pattern));
        }
        
        return issues;
    }
    
    private void LoadProjectPatterns()
    {
        _projectPatterns = new List<ProjectPattern>();
        
        if (!Directory.Exists(_patternsDirectory))
        {
            Console.WriteLine($"Warning: Project patterns directory not found: {_patternsDirectory}");
            return;
        }
        
        foreach (var file in Directory.GetFiles(_patternsDirectory, "*.json"))
        {
            try
            {
                var jsonContent = File.ReadAllText(file);
                var pattern = JsonSerializer.Deserialize<ProjectPattern>(jsonContent, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                if (pattern != null)
                {
                    _projectPatterns.Add(pattern);
                    Console.WriteLine($"Loaded project pattern: {pattern.PatternName}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading project pattern from {file}: {ex.Message}");
            }
        }
    }
    
    private bool ShouldExempt(string filePath, List<string> exemptionPatterns)
    {
        if (exemptionPatterns == null || !exemptionPatterns.Any())
        {
            return false;
        }
        
        // Get relative path from project root
        var relativePath = GetRelativePath(filePath, _projectRootDirectory);
        
        return exemptionPatterns.Any(pattern => Regex.IsMatch(relativePath, pattern));
    }
    
    private string GetRelativePath(string filePath, string basePath)
    {
        if (string.IsNullOrEmpty(filePath) || string.IsNullOrEmpty(basePath))
        {
            return filePath;
        }
        
        // Normalize paths for comparison
        var normalizedFile = filePath.Replace('\\', '/');
        var normalizedBase = basePath.Replace('\\', '/');
        
        if (!normalizedBase.EndsWith("/"))
        {
            normalizedBase += "/";
        }
        
        if (normalizedFile.StartsWith(normalizedBase, StringComparison.OrdinalIgnoreCase))
        {
            return normalizedFile.Substring(normalizedBase.Length);
        }
        
        return filePath;
    }
    
    private List<CodeIssue> AnalyzeWithPattern(string filePath, string fileContent, ProjectPattern pattern)
    {
        var issues = new List<CodeIssue>();
        
        foreach (var rule in pattern.Rules)
        {
            switch (rule.Type)
            {
                case "FolderStructure":
                    issues.AddRange(AnalyzeFolderStructure(filePath, pattern, rule));
                    break;
                case "FileLocation":
                    issues.AddRange(AnalyzeFileLocation(filePath, pattern, rule));
                    break;
                case "Namespace":
                    issues.AddRange(AnalyzeNamespace(filePath, fileContent, pattern, rule));
                    break;
                case "FileNaming":
                    issues.AddRange(AnalyzeFileNaming(filePath, pattern, rule));
                    break;
                case "DomainIntegrity":
                    issues.AddRange(AnalyzeDomainIntegrity(filePath, fileContent, pattern, rule));
                    break;
            }
        }
        
        return issues;
    }
    
    private List<CodeIssue> AnalyzeFolderStructure(string filePath, ProjectPattern pattern, ProjectRule rule)
    {
        var issues = new List<CodeIssue>();
        
        if (rule.ExpectedFolders != null && rule.ExpectedFolders.Any())
        {
            foreach (var folder in rule.ExpectedFolders)
            {
                var folderPath = Path.Combine(_projectRootDirectory, folder);
                
                // Use cache to avoid checking the same folder multiple times
                if (!_foldersCache.ContainsKey(folderPath))
                {
                    _foldersCache[folderPath] = Directory.Exists(folderPath);
                }
                
                if (!_foldersCache[folderPath])
                {
                    issues.Add(new CodeIssue
                    {
                        FilePath = filePath,
                        LineNumber = 1, // Not applicable for folder structure
                        Message = $"{rule.Message} - Pasta \"{folder}\" não encontrada",
                        Severity = ParseSeverity(pattern.Severity),
                        Suggestion = rule.Suggestion ?? $"Crie a pasta \"{folder}\" no diretório raiz do projeto"
                    });
                }
            }
        }
        
        return issues;
    }
    
    private List<CodeIssue> AnalyzeFileLocation(string filePath, ProjectPattern pattern, ProjectRule rule)
    {
        var issues = new List<CodeIssue>();
        
        if (rule.FilePatterns != null && rule.FilePatterns.Any())
        {
            var fileName = Path.GetFileName(filePath);
            var relativePath = GetRelativePath(filePath, _projectRootDirectory);
            
            foreach (var filePattern in rule.FilePatterns)
            {
                var regex = new Regex(filePattern.Key);
                if (regex.IsMatch(fileName))
                {
                    var expectedLocation = filePattern.Value;
                    if (!relativePath.StartsWith(expectedLocation, StringComparison.OrdinalIgnoreCase))
                    {
                        issues.Add(new CodeIssue
                        {
                            FilePath = filePath,
                            LineNumber = 1, // Not applicable for file location
                            Message = $"{rule.Message} - Arquivo \"{fileName}\" deve estar na pasta \"{expectedLocation}\"",
                            Severity = ParseSeverity(pattern.Severity),
                            Suggestion = rule.Suggestion ?? $"Mova o arquivo para a pasta \"{expectedLocation}\""
                        });
                    }
                }
            }
        }
        
        return issues;
    }
    
    private List<CodeIssue> AnalyzeNamespace(string filePath, string fileContent, ProjectPattern pattern, ProjectRule rule)
    {
        var issues = new List<CodeIssue>();
        
        // Extract namespace from file content
        var namespaceMatch = Regex.Match(fileContent, @"namespace\s+([^{;]+)");
        if (namespaceMatch.Success)
        {
            var namespaceValue = namespaceMatch.Groups[1].Value.Trim();
            
            // Check simple namespace pattern
            if (!string.IsNullOrEmpty(rule.NamespacePattern))
            {
                var regex = new Regex(rule.NamespacePattern);
                if (!regex.IsMatch(namespaceValue))
                {
                    issues.Add(new CodeIssue
                    {
                        FilePath = filePath,
                        LineNumber = GetLineNumber(fileContent, namespaceMatch.Index),
                        Message = $"{rule.Message} - Namespace \"{namespaceValue}\" não segue o padrão esperado",
                        Severity = ParseSeverity(pattern.Severity),
                        Suggestion = rule.Suggestion
                    });
                }
            }
            
            // Check folder-specific namespace patterns
            if (rule.NamespacePatterns != null && rule.NamespacePatterns.Any())
            {
                var relativePath = GetRelativePath(filePath, _projectRootDirectory);
                var parentDir = Path.GetDirectoryName(relativePath)?.Replace('\\', '/');
                
                if (!string.IsNullOrEmpty(parentDir))
                {
                    foreach (var namespacePattern in rule.NamespacePatterns)
                    {
                        if (parentDir.StartsWith(namespacePattern.Key, StringComparison.OrdinalIgnoreCase))
                        {
                            var regex = new Regex(namespacePattern.Value);
                            if (!regex.IsMatch(namespaceValue))
                            {
                                issues.Add(new CodeIssue
                                {
                                    FilePath = filePath,
                                    LineNumber = GetLineNumber(fileContent, namespaceMatch.Index),
                                    Message = $"{rule.Message} - Arquivo na pasta \"{namespacePattern.Key}\" deve usar namespace que corresponda ao padrão",
                                    Severity = ParseSeverity(pattern.Severity),
                                    Suggestion = rule.Suggestion
                                });
                            }
                        }
                    }
                }
            }
        }
        
        return issues;
    }
    
    private List<CodeIssue> AnalyzeFileNaming(string filePath, ProjectPattern pattern, ProjectRule rule)
    {
        var issues = new List<CodeIssue>();
        
        if (rule.FilePatterns != null && rule.FilePatterns.Any())
        {
            var fileName = Path.GetFileName(filePath);
            var fileType = Path.GetExtension(filePath).ToLowerInvariant() == ".cs" ? "classe" : "arquivo";
            
            foreach (var filePattern in rule.FilePatterns)
            {
                var componentType = filePattern.Key;
                
                // Check if the file is a specific component type based on its content or location
                if (fileName.Contains(componentType, StringComparison.OrdinalIgnoreCase))
                {
                    var expectedPattern = filePattern.Value;
                    var regex = new Regex(expectedPattern);
                    
                    if (!regex.IsMatch(fileName))
                    {
                        issues.Add(new CodeIssue
                        {
                            FilePath = filePath,
                            LineNumber = 1,
                            Message = $"{rule.Message} - Nome do {fileType} do tipo \"{componentType}\" deve seguir o padrão \"{expectedPattern}\"",
                            Severity = ParseSeverity(pattern.Severity),
                            Suggestion = rule.Suggestion
                        });
                    }
                }
            }
        }
        
        return issues;
    }
    
    private List<CodeIssue> AnalyzeDomainIntegrity(string filePath, string fileContent, ProjectPattern pattern, ProjectRule rule)
    {
        var issues = new List<CodeIssue>();
        
        if (rule.Rules != null && rule.Rules.Any())
        {
            var relativePath = GetRelativePath(filePath, _projectRootDirectory);
            
            foreach (var integrityRule in rule.Rules)
            {
                if (Regex.IsMatch(relativePath, integrityRule.SourcePattern))
                {
                    // Extract all using statements to check dependencies
                    var usingMatches = Regex.Matches(fileContent, @"using\s+([^;]+);");
                    
                    foreach (Match match in usingMatches)
                    {
                        var dependency = match.Groups[1].Value.Trim();
                        
                        // Check forbidden dependencies
                        if (integrityRule.ForbiddenDependencies != null && integrityRule.ForbiddenDependencies.Any())
                        {
                            foreach (var forbidden in integrityRule.ForbiddenDependencies)
                            {
                                if (dependency.Contains(forbidden, StringComparison.OrdinalIgnoreCase))
                                {
                                    issues.Add(new CodeIssue
                                    {
                                        FilePath = filePath,
                                        LineNumber = GetLineNumber(fileContent, match.Index),
                                        Message = $"{integrityRule.Message} - Dependência proibida: \"{dependency}\"",
                                        Severity = ParseSeverity(pattern.Severity),
                                        Suggestion = "Remova esta dependência ou refatore o código para seguir os princípios de arquitetura"
                                    });
                                }
                            }
                        }
                        
                        // Check if dependency is not in allowed list
                        if (integrityRule.AllowedDependencies != null && integrityRule.AllowedDependencies.Any())
                        {
                            bool isAllowed = false;
                            
                            foreach (var allowed in integrityRule.AllowedDependencies)
                            {
                                if (dependency.Contains(allowed, StringComparison.OrdinalIgnoreCase))
                                {
                                    isAllowed = true;
                                    break;
                                }
                            }
                            
                            // Skip system and framework dependencies
                            if (dependency.StartsWith("System") || dependency.StartsWith("Microsoft"))
                            {
                                isAllowed = true;
                            }
                            
                            if (!isAllowed)
                            {
                                issues.Add(new CodeIssue
                                {
                                    FilePath = filePath,
                                    LineNumber = GetLineNumber(fileContent, match.Index),
                                    Message = $"{integrityRule.Message} - Dependência não permitida: \"{dependency}\"",
                                    Severity = ParseSeverity(pattern.Severity),
                                    Suggestion = "Substitua por uma dependência permitida ou refatore o código"
                                });
                            }
                        }
                    }
                }
            }
        }
        
        return issues;
    }
    
    private int GetLineNumber(string content, int position)
    {
        int lineNumber = 1;
        
        for (int i = 0; i < position && i < content.Length; i++)
        {
            if (content[i] == '\n')
            {
                lineNumber++;
            }
        }
        
        return lineNumber;
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
    private class ProjectPattern
    {
        public string PatternName { get; set; }
        public string Description { get; set; }
        public string Severity { get; set; }
        public List<ProjectRule> Rules { get; set; }
        public List<string> ExemptionPatterns { get; set; }
        public ExampleStructure Examples { get; set; }
    }
    
    private class ProjectRule
    {
        public string Type { get; set; }
        public List<string> ExpectedFolders { get; set; }
        public Dictionary<string, string> FilePatterns { get; set; }
        public string NamespacePattern { get; set; }
        public Dictionary<string, string> NamespacePatterns { get; set; }
        public string Message { get; set; }
        public string Suggestion { get; set; }
        public List<DomainIntegrityRule> Rules { get; set; }
    }
    
    private class DomainIntegrityRule
    {
        public string SourcePattern { get; set; }
        public List<string> ForbiddenDependencies { get; set; }
        public List<string> AllowedDependencies { get; set; }
        public string Message { get; set; }
    }
    
    private class ExampleStructure
    {
        public List<string> GoodStructure { get; set; }
        public List<string> BadStructure { get; set; }
    }
}
