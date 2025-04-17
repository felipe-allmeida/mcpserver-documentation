using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using System.Collections.Generic;
using System.Threading.Tasks;
using mcpserver.Rules;

namespace mcpserver.Tools;

/// <summary>
/// Classe que fornece ferramentas de análise de código através do Model Context Protocol.
/// Esta classe implementa as APIs de análise de código, documentação e lógica expostas como ferramentas MCP.
/// </summary>
[McpServerToolType]
public class AnalyzeCodeTools
{
    private readonly ILogger<AnalyzeCodeTools> _logger;
    private readonly CodeInsightsIA _codeInsights;

    /// <summary>
    /// Inicializa uma nova instância da classe <see cref="AnalyzeCodeTools"/>
    /// </summary>
    /// <param name="logger">Logger para registrar informações e erros</param>
    /// <param name="codeInsights">Serviço de análise de código</param>
    public AnalyzeCodeTools(
        ILogger<AnalyzeCodeTools> logger,
        CodeInsightsIA codeInsights)
    {
        _logger = logger;
        _codeInsights = codeInsights;
    }

    /// <summary>
    /// Analisa o código-fonte em busca de problemas relacionados a padrões de código da empresa
    /// </summary>
    /// <param name="server">Servidor MCP</param>
    /// <param name="projectPath">Caminho para o projeto ou diretório que contém o código-fonte a ser analisado</param>
    /// <param name="specificPatterns">Padrões específicos a serem verificados (se vazio, verifica todos os padrões)</param>
    /// <returns>Resultado da análise com os principais problemas encontrados</returns>
    [McpServerTool, Description("Analisa o código-fonte em busca de problemas relacionados a padrões de código da empresa")]
    public async Task<object> AnalyzeCodePatterns(
        IMcpServer server,
        [Description("Caminho para o projeto ou diretório que contém o código-fonte a ser analisado")] string projectPath,
        [Description("Padrões específicos a serem verificados (se vazio, verifica todos os padrões)")] string[]? specificPatterns = null)
    {
        if (string.IsNullOrEmpty(projectPath))
        {
            return new
            {
                Error = true,
                Message = "O caminho do projeto não pode ser vazio"
            };
        }

        var options = new AnalysisOptions
        {
            AnalyzeCode = true,
            AnalyzeDocumentation = false,
            AnalyzeLogic = false
        };

        try
        {
            _logger.LogInformation("Iniciando análise de padrões de código em: {ProjectPath}", projectPath);
            
            // Send progress update
            await server.SendNotificationAsync("Iniciando análise de padrões de código...", 0);
            
            // Execute analysis with progress reporting
            var result = await _codeInsights.AnalyzeProjectAsync(
                projectPath, 
                options,
                (message, percent) => server.SendNotificationAsync(message, percent).GetAwaiter().GetResult()
            );
            
            var issueCount = result.CodeIssues.Count;
            var summary = $"Análise concluída com {issueCount} problemas de padrões de código encontrados.";
            
            return new
            {
                Summary = summary,
                ProjectPath = projectPath,
                IssueCount = issueCount,
                // Retorna apenas os 5 problemas mais críticos para não sobrecarregar a resposta
                TopIssues = result.CodeIssues
                    .OrderByDescending(i => (int)i.Severity)
                    .Take(5)
                    .Select(i => new
                    {
                        i.FilePath,
                        i.LineNumber,
                        i.Message,
                        i.Suggestion,
                        Severity = i.Severity.ToString()
                    })
                    .ToArray()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao analisar padrões de código");
            return new
            {
                Error = true,
                Message = $"Erro ao analisar padrões de código: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Analisa a documentação do código-fonte em busca de problemas ou melhorias
    /// </summary>
    /// <param name="server">Servidor MCP</param>
    /// <param name="projectPath">Caminho para o projeto ou diretório que contém o código-fonte a ser analisado</param>
    /// <param name="includePrivateMembers">Se verdadeiro, também verifica a documentação de membros privados</param>
    /// <returns>Resultado da análise com os principais problemas de documentação encontrados</returns>
    [McpServerTool, Description("Analisa a documentação do código-fonte em busca de problemas ou melhorias")]
    public async Task<object> AnalyzeDocumentation(
        IMcpServer server,
        [Description("Caminho para o projeto ou diretório que contém o código-fonte a ser analisado")] string projectPath,
        [Description("Se verdadeiro, também verifica a documentação de membros privados")] bool includePrivateMembers = false)
    {
        if (string.IsNullOrEmpty(projectPath))
        {
            return new
            {
                Error = true,
                Message = "O caminho do projeto não pode ser vazio"
            };
        }

        var options = new AnalysisOptions
        {
            AnalyzeCode = false,
            AnalyzeDocumentation = true,
            AnalyzeLogic = false
        };

        try
        {
            _logger.LogInformation("Iniciando análise de documentação em: {ProjectPath}", projectPath);
            
            // Send progress update
            await server.SendNotificationAsync("Iniciando análise de documentação...", 0);
            
            // Execute analysis with progress reporting
            var result = await _codeInsights.AnalyzeProjectAsync(
                projectPath, 
                options,
                (message, percent) => server.SendNotificationAsync(message, percent).GetAwaiter().GetResult()
            );
            
            var issueCount = result.DocumentationIssues.Count;
            var summary = $"Análise concluída com {issueCount} problemas de documentação encontrados.";
            
            return new
            {
                Summary = summary,
                ProjectPath = projectPath,
                IssueCount = issueCount,
                TopIssues = result.DocumentationIssues
                    .OrderByDescending(i => (int)i.Severity)
                    .Take(5)
                    .Select(i => new
                    {
                        i.FilePath,
                        i.LineNumber,
                        i.Message,
                        i.Suggestion,
                        Severity = i.Severity.ToString()
                    })
                    .ToArray()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao analisar documentação");
            return new
            {
                Error = true,
                Message = $"Erro ao analisar documentação: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Analisa a lógica do código-fonte em busca de problemas de performance, complexidade ou design
    /// </summary>
    /// <param name="server">Servidor MCP</param>
    /// <param name="projectPath">Caminho para o projeto ou diretório que contém o código-fonte a ser analisado</param>
    /// <param name="focusAreas">Áreas específicas a serem analisadas</param>
    /// <returns>Resultado da análise com os principais problemas de lógica encontrados</returns>
    [McpServerTool, Description("Analisa a lógica do código-fonte em busca de problemas de performance, complexidade ou design")]
    public async Task<object> AnalyzeLogic(
        IMcpServer server,
        [Description("Caminho para o projeto ou diretório que contém o código-fonte a ser analisado")] string projectPath,
        [Description("Áreas específicas a serem analisadas")] string[]? focusAreas = null)
    {
        if (string.IsNullOrEmpty(projectPath))
        {
            return new
            {
                Error = true,
                Message = "O caminho do projeto não pode ser vazio"
            };
        }

        var options = new AnalysisOptions
        {
            AnalyzeCode = false,
            AnalyzeDocumentation = false,
            AnalyzeLogic = true
        };

        try
        {
            _logger.LogInformation("Iniciando análise de lógica em: {ProjectPath}", projectPath);
            
            // Send progress update
            await server.SendNotificationAsync("Iniciando análise de lógica e performance...", 0);
            
            // Execute analysis with progress reporting
            var result = await _codeInsights.AnalyzeProjectAsync(
                projectPath, 
                options,
                (message, percent) => server.SendNotificationAsync(message, percent).GetAwaiter().GetResult()
            );
            
            var issueCount = result.LogicIssues.Count;
            var summary = $"Análise concluída com {issueCount} problemas de lógica/performance encontrados.";
            
            return new
            {
                Summary = summary,
                ProjectPath = projectPath,
                IssueCount = issueCount,
                TopIssues = result.LogicIssues
                    .OrderByDescending(i => (int)i.Severity)
                    .Take(5)
                    .Select(i => new
                    {
                        i.FilePath,
                        i.LineNumber,
                        i.Message,
                        i.Suggestion,
                        Severity = i.Severity.ToString()
                    })
                    .ToArray()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao analisar lógica");
            return new
            {
                Error = true,
                Message = $"Erro ao analisar lógica: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Obtém os resultados detalhados da última análise de código realizada
    /// </summary>
    /// <param name="server">Servidor MCP</param>
    /// <param name="filterBySeverity">Filtra os resultados pelo nível de severidade (info, warning, error, all)</param>
    /// <param name="maxResults">Número máximo de resultados a retornar</param>
    /// <returns>Resultados detalhados da análise</returns>
    [McpServerTool, Description("Obtém os resultados detalhados da última análise de código realizada")]
    public object GetAnalysisResults(
        IMcpServer server,
        [Description("Filtra os resultados pelo nível de severidade (info, warning, error, all)")] string? filterBySeverity = "all",
        [Description("Número máximo de resultados a retornar")] int maxResults = 100)
    {
        try
        {
            // Utilize o último resultado armazenado no CodeInsightsIA
            var result = _codeInsights.LastAnalysisResult;
            
            if (result == null)
            {
                return new
                {
                    Error = true,
                    Message = "Nenhuma análise foi realizada ainda. Execute uma análise primeiro."
                };
            }

            // Filtragem por severidade, se especificada
            IssueSeverity? severityFilter = null;

            if (!string.IsNullOrEmpty(filterBySeverity) && filterBySeverity != "all")
            {
                severityFilter = filterBySeverity switch
                {
                    "info" => IssueSeverity.Info,
                    "warning" => IssueSeverity.Warning,
                    "error" => IssueSeverity.Error,
                    _ => null
                };
            }

            // Aplica os filtros
            var allIssues = new List<object>();
            
            // Função para adicionar issues com tipo
            void AddIssuesWithType<T>(List<T> issues, string type) where T : IssueBase
            {
                var filtered = issues.Where(i => !severityFilter.HasValue || i.Severity == severityFilter.Value)
                    .Select(i => new
                    {
                        Type = type,
                        i.FilePath,
                        i.LineNumber,
                        i.Message,
                        i.Suggestion,
                        Severity = i.Severity.ToString()
                    })
                    .Cast<object>()
                    .ToList();
                
                allIssues.AddRange(filtered);
            }
            
            // Adiciona os diferentes tipos de issues à lista combinada
            AddIssuesWithType(result.CodeIssues, "Código");
            AddIssuesWithType(result.DocumentationIssues, "Documentação");
            AddIssuesWithType(result.LogicIssues, "Lógica/Performance");
            
            // Ordena e limita os resultados
            var finalResults = allIssues
                .Take(maxResults)
                .ToList();
            
            return new
            {
                Summary = result.Summary,
                ProjectPath = result.ProjectPath,
                Timestamp = result.Timestamp,
                TotalIssues = result.TotalIssueCount,
                FilteredResults = finalResults,
                FilteredCount = finalResults.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao recuperar resultados da análise");
            return new
            {
                Error = true,
                Message = $"Erro ao recuperar resultados da análise: {ex.Message}"
            };
        }
    }
}