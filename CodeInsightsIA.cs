using System.Collections.Concurrent;
using mcpserver.Rules;
using Microsoft.Extensions.Logging;

namespace mcpserver;

/// <summary>
/// Serviço de análise de código que verifica padrões da empresa,
/// documentação e lógica, apresentando sugestões de melhorias.
/// </summary>
public class CodeInsightsIA
{
    private readonly ILogger<CodeInsightsIA> _logger;
    
    /// <summary>
    /// Lista de regras para análise de código.
    /// </summary>
    private readonly List<ICodeAnalysisRule> _codeRules;
    
    /// <summary>
    /// Lista de regras para análise de documentação.
    /// </summary>
    private readonly List<IDocumentationAnalysisRule> _documentationRules;
    
    /// <summary>
    /// Lista de regras para análise de lógica.
    /// </summary>
    private readonly List<ILogicAnalysisRule> _logicRules;

    /// <summary>
    /// Armazena o resultado da última análise realizada.
    /// </summary>
    public AnalysisResult LastAnalysisResult { get; private set; }

    private readonly Dictionary<string, List<string>> _sourceFilesCache = new();
    private AnalysisOptions? _lastOptions;

    public CodeInsightsIA(
        ILogger<CodeInsightsIA> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Inicializa as regras de análise
        _codeRules = new List<ICodeAnalysisRule>
        {
            new NamingConventionRule(),
            new ArchitecturalPatternRule(),
            new ProjectStructureRule()
        };
        
        _documentationRules = new List<IDocumentationAnalysisRule>
        {
            new XmlCommentCompletionRule(),
            new DocumentationQualityRule()
        };
        
        _logicRules = new List<ILogicAnalysisRule>
        {
            new CodeSmellRule(),
            new ComplexityAnalysisRule(),
            new PerformanceOptimizationRule()
        };
    }

    /// <summary>
    /// Analisa um projeto de código de acordo com as regras configuradas.
    /// </summary>
    /// <param name="projectPath">Caminho para o projeto a ser analisado</param>
    /// <param name="configOptions">Opções de configuração da análise</param>
    /// <param name="progressCallback">Callback opcional para reportar progresso da análise</param>
    /// <returns>Resultado da análise com sugestões</returns>
    public async Task<AnalysisResult> AnalyzeProjectAsync(
        string projectPath, 
        AnalysisOptions configOptions,
        Action<string, int>? progressCallback = null)
    {
        if (string.IsNullOrEmpty(projectPath))
        {
            throw new ArgumentNullException(nameof(projectPath));
        }

        if (configOptions == null)
        {
            throw new ArgumentNullException(nameof(configOptions));
        }

        _logger.LogInformation("Iniciando análise do projeto: {ProjectPath}", projectPath);
        
        // Store options for use in other methods
        _lastOptions = configOptions;
        
        var result = new AnalysisResult
        {
            ProjectPath = projectPath,
            CodeIssues = new List<CodeIssue>(100),
            DocumentationIssues = new List<DocumentationIssue>(50),
            LogicIssues = new List<LogicIssue>(100),
            Timestamp = DateTime.UtcNow,
            Summary = string.Empty,
            ErrorMessage = string.Empty
        };

        try
        {
            if (configOptions.AnalyzeCode)
            {
                progressCallback?.Invoke("Starting code standards analysis...", 0);
                await AnalyzeCodeStandardsAsync(projectPath, result, progressCallback);
                progressCallback?.Invoke("Code standards analysis completed", 33);
            }
            
            if (configOptions.AnalyzeDocumentation)
            {
                progressCallback?.Invoke("Starting documentation analysis...", 33);
                await AnalyzeDocumentationAsync(projectPath, result, progressCallback);
                progressCallback?.Invoke("Documentation analysis completed", 66);
            }
            
            if (configOptions.AnalyzeLogic)
            {
                progressCallback?.Invoke("Starting logic and performance analysis...", 66);
                await AnalyzeLogicAsync(projectPath, result, progressCallback);
                progressCallback?.Invoke("All analysis completed", 100);
            }
            
            await CalculateFileStatisticsAsync(projectPath, result);
            
            result.Summary = GenerateAnalysisSummary(result);
            _logger.LogInformation("Análise concluída com {IssueCount} problemas encontrados", 
                result.TotalIssueCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao analisar o projeto");
            result.ErrorMessage = ex.Message;
        }
        
        // Store the analysis result for later retrieval
        LastAnalysisResult = result;
        return result;
    }

    private async Task AnalyzeCodeStandardsAsync(string projectPath, AnalysisResult result, Action<string, int>? progressCallback = null)
    {
        if (string.IsNullOrEmpty(projectPath))
        {
            throw new ArgumentNullException(nameof(projectPath));
        }

        if (result == null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        _logger.LogDebug("Analisando padrões de código");
        progressCallback?.Invoke("Iniciando análise de padrões de código...", 0);
        
        var files = GetSourceFiles(projectPath, _lastOptions!);
        var issues = new ConcurrentBag<CodeIssue>();
        
        // Progress tracking
        var totalFiles = files.Count;
        var processedFiles = 0;
        var progressLock = new object();
        
        // Process files in parallel
        await Parallel.ForEachAsync(
            files, 
            new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, 
            async (file, token) =>
            {
                try
                {
                    var fileContent = await File.ReadAllTextAsync(file, token);
                
                    foreach (var rule in _codeRules)
                    {
                        try
                        {
                            var fileIssues = rule.Analyze(file, fileContent);
                            foreach (var issue in fileIssues)
                            {
                                issues.Add(issue);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Erro ao aplicar regra de análise de código {RuleType} no arquivo {FilePath}", 
                                rule.GetType().Name, file);
                            
                            // Add a special issue for rule errors
                            issues.Add(new CodeIssue 
                            { 
                                FilePath = file,
                                LineNumber = 1,
                                Message = $"Erro ao analisar com regra {rule.GetType().Name}: {ex.Message}",
                                Severity = IssueSeverity.Error,
                                Suggestion = "Verifique o arquivo manualmente ou corrija o erro para permitir análise"
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao processar o arquivo {FilePath}", file);
                    
                    // Add a special issue for file processing errors
                    issues.Add(new CodeIssue 
                    { 
                        FilePath = file,
                        LineNumber = 1,
                        Message = $"Erro ao processar arquivo: {ex.Message}",
                        Severity = IssueSeverity.Error,
                        Suggestion = "Verifique se o arquivo pode ser aberto e está em formato válido"
                    });
                }
                finally
                {
                    lock (progressLock)
                    {
                        processedFiles++;
                        var progressPercentage = (int)((processedFiles / (double)totalFiles) * 100);
                        _logger.LogDebug("Progresso da análise de padrões de código: {ProgressPercentage}%", progressPercentage);
                        progressCallback?.Invoke($"Analisando arquivo {processedFiles} de {totalFiles}", progressPercentage);
                    }
                }
            });
        
        // Add all issues to the result
        result.CodeIssues.AddRange(issues);
    }

    private async Task AnalyzeDocumentationAsync(string projectPath, AnalysisResult result, Action<string, int>? progressCallback = null)
    {
        if (string.IsNullOrEmpty(projectPath))
        {
            throw new ArgumentNullException(nameof(projectPath));
        }

        if (result == null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        _logger.LogDebug("Analisando documentação");
        progressCallback?.Invoke("Iniciando análise de documentação...", 0);
        
        var files = GetSourceFiles(projectPath, _lastOptions!);
        var issues = new ConcurrentBag<DocumentationIssue>();
        
        // Progress tracking
        var totalFiles = files.Count;
        var processedFiles = 0;
        var progressLock = new object();
        
        // Process files in parallel
        await Parallel.ForEachAsync(
            files, 
            new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, 
            async (file, token) =>
            {
                try
                {
                    var fileContent = await File.ReadAllTextAsync(file, token);
                
                    foreach (var rule in _documentationRules)
                    {
                        try
                        {
                            var fileIssues = rule.Analyze(file, fileContent);
                            foreach (var issue in fileIssues)
                            {
                                issues.Add((DocumentationIssue)issue);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Erro ao aplicar regra de documentação {RuleType} no arquivo {FilePath}", 
                                rule.GetType().Name, file);
                            
                            // Add a special issue for rule errors
                            issues.Add(new DocumentationIssue 
                            { 
                                FilePath = file,
                                LineNumber = 1,
                                Message = $"Erro ao analisar com regra {rule.GetType().Name}: {ex.Message}",
                                Severity = IssueSeverity.Error,
                                Suggestion = "Verifique o arquivo manualmente ou corrija o erro para permitir análise"
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao processar o arquivo {FilePath} para análise de documentação", file);
                    
                    // Add a special issue for file processing errors
                    issues.Add(new DocumentationIssue 
                    { 
                        FilePath = file,
                        LineNumber = 1,
                        Message = $"Erro ao processar arquivo: {ex.Message}",
                        Severity = IssueSeverity.Error,
                        Suggestion = "Verifique se o arquivo pode ser aberto e está em formato válido"
                    });
                }
                finally
                {
                    lock (progressLock)
                    {
                        processedFiles++;
                        var progressPercentage = (int)((processedFiles / (double)totalFiles) * 100);
                        _logger.LogDebug("Progresso da análise de documentação: {ProgressPercentage}%", progressPercentage);
                        progressCallback?.Invoke($"Analisando arquivo {processedFiles} de {totalFiles}", progressPercentage);
                    }
                }
            });
        
        // Add all issues to the result
        result.DocumentationIssues.AddRange(issues);
    }

    private async Task AnalyzeLogicAsync(string projectPath, AnalysisResult result, Action<string, int>? progressCallback = null)
    {
        if (string.IsNullOrEmpty(projectPath))
        {
            throw new ArgumentNullException(nameof(projectPath));
        }

        if (result == null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        _logger.LogDebug("Analisando lógica e buscando sugestões de melhorias");
        progressCallback?.Invoke("Iniciando análise de lógica e performance...", 0);
        
        var files = GetSourceFiles(projectPath, _lastOptions!);
        var issues = new ConcurrentBag<LogicIssue>();
        
        // Progress tracking
        var totalFiles = files.Count;
        var processedFiles = 0;
        var progressLock = new object();
        
        // Process files in parallel
        await Parallel.ForEachAsync(
            files, 
            new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, 
            async (file, token) =>
            {
                try
                {
                    var fileContent = await File.ReadAllTextAsync(file, token);
                
                    foreach (var rule in _logicRules)
                    {
                        try
                        {
                            var fileIssues = rule.Analyze(file, fileContent);
                            foreach (var issue in fileIssues)
                            {
                                issues.Add((LogicIssue)issue);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Erro ao aplicar regra de análise de lógica {RuleType} no arquivo {FilePath}", 
                                rule.GetType().Name, file);
                            
                            // Add a special issue for rule errors
                            issues.Add(new LogicIssue 
                            { 
                                FilePath = file,
                                LineNumber = 1,
                                Message = $"Erro ao analisar com regra {rule.GetType().Name}: {ex.Message}",
                                Severity = IssueSeverity.Error,
                                Suggestion = "Verifique o arquivo manualmente ou corrija o erro para permitir análise"
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao processar o arquivo {FilePath} para análise de lógica", file);
                    
                    // Add a special issue for file processing errors
                    issues.Add(new LogicIssue 
                    { 
                        FilePath = file,
                        LineNumber = 1,
                        Message = $"Erro ao processar arquivo: {ex.Message}",
                        Severity = IssueSeverity.Error,
                        Suggestion = "Verifique se o arquivo pode ser aberto e está em formato válido"
                    });
                }
                finally
                {
                    lock (progressLock)
                    {
                        processedFiles++;
                        var progressPercentage = (int)((processedFiles / (double)totalFiles) * 100);
                        _logger.LogDebug("Progresso da análise de lógica: {ProgressPercentage}%", progressPercentage);
                        progressCallback?.Invoke($"Analisando arquivo {processedFiles} de {totalFiles}", progressPercentage);
                    }
                }
            });
        
        // Add all issues to the result
        result.LogicIssues.AddRange(issues);
    }

    private async Task CalculateFileStatisticsAsync(string projectPath, AnalysisResult result)
    {
        var files = GetSourceFiles(projectPath, _lastOptions!);
        result.TotalFilesAnalyzed = files.Count;
        
        long totalLines = 0;
        foreach (var file in files)
        {
            try
            {
                var lineCount = await File.ReadAllLinesAsync(file).ContinueWith(t => t.Result.Length);
                totalLines += lineCount;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error counting lines in file: {FilePath}", file);
            }
        }
        
        result.TotalLinesOfCode = (int)totalLines;
        
        // Calculate severity statistics
        var severityStats = new Dictionary<string, int>
        {
            { "Error", 0 },
            { "Warning", 0 },
            { "Info", 0 }
        };
        
        // Count code issues by severity
        foreach (var issue in result.CodeIssues)
        {
            var severity = issue.Severity.ToString();
            if (severityStats.ContainsKey(severity))
            {
                severityStats[severity]++;
            }
        }
        
        // Count documentation issues by severity
        foreach (var issue in result.DocumentationIssues)
        {
            var severity = issue.Severity.ToString();
            if (severityStats.ContainsKey(severity))
            {
                severityStats[severity]++;
            }
        }
        
        // Count logic issues by severity
        foreach (var issue in result.LogicIssues)
        {
            var severity = issue.Severity.ToString();
            if (severityStats.ContainsKey(severity))
            {
                severityStats[severity]++;
            }
        }
        
        result.SeverityStats = severityStats;
    }

    private List<string> GetSourceFiles(string projectPath, AnalysisOptions options)
    {
        if (string.IsNullOrEmpty(projectPath))
        {
            throw new ArgumentNullException(nameof(projectPath));
        }

        // Generate a cache key combining the path and exclusion patterns to ensure
        // we don't reuse cached results if exclusion patterns have changed
        var cacheKey = projectPath;
        if (options.ExclusionPatterns.Count > 0)
        {
            cacheKey = $"{projectPath}:{string.Join("|", options.ExclusionPatterns)}";
        }

        // Use cached result if available for the same options
        if (_sourceFilesCache.TryGetValue(cacheKey, out var cachedFiles))
        {
            return cachedFiles;
        }

        // Default exclusion patterns
        var exclusionPatterns = new List<string> {
            "\\obj\\", 
            "\\bin\\",
            "\\.git\\",
            "\\.vs\\",
            "\\node_modules\\",
            "\\packages\\",
            "\\TestResults\\"
        };
        
        // Add custom exclusion patterns
        if (options.ExclusionPatterns.Count > 0)
        {
            exclusionPatterns.AddRange(options.ExclusionPatterns);
        }
        
        var files = Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories)
            .Where(f => !exclusionPatterns.Any(pattern => f.Contains(pattern)))
            .ToList();
            
        // Cache the result for future calls
        _sourceFilesCache[cacheKey] = files;
        
        return files;
    }

    private string GenerateAnalysisSummary(AnalysisResult result)
    {
        if (result == null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        return $"Análise concluída com {result.TotalIssueCount} problemas encontrados em {result.TotalFilesAnalyzed} arquivos ({result.TotalLinesOfCode} linhas de código):\n" +
               $"- {result.CodeIssues.Count} problemas de padrões de código\n" +
               $"- {result.DocumentationIssues.Count} problemas de documentação\n" +
               $"- {result.LogicIssues.Count} problemas de lógica/performance\n" +
               $"\nSeveridade dos problemas:\n" +
               $"- Erros: {result.SeverityStats.GetValueOrDefault("Error", 0)}\n" +
               $"- Avisos: {result.SeverityStats.GetValueOrDefault("Warning", 0)}\n" +
               $"- Informações: {result.SeverityStats.GetValueOrDefault("Info", 0)}";
    }
}