# CodeInsights IA - Servidor MCP para Análise de Código

Este projeto implementa um servidor Model Context Protocol (MCP) especializado em análise de código-fonte, oferecendo ferramentas para identificação de problemas em bases de código .NET.

## Sobre o Projeto

O CodeInsights IA é uma ferramenta de análise estática de código que utiliza o protocolo MCP (Model Context Protocol) para fornecer serviços de análise de código inteligente. O servidor expõe ferramentas que podem ser chamadas por clientes MCP, como modelos de linguagem ou assistentes de programação, para analisar bases de código em busca de problemas relacionados a:

- **Padrões de código**: Verifica conformidade com padrões e boas práticas de codificação
- **Qualidade de documentação**: Analisa completude e qualidade da documentação de código
- **Problemas de lógica e performance**: Identifica possíveis gargalos de desempenho e problemas de design

## Funcionalidades Principais

O servidor oferece três ferramentas principais de análise, cada uma focada em um aspecto diferente da qualidade de código:

### 1. Análise de Padrões de Código

```
AnalyzeCodePatterns(projectPath, specificPatterns)
```

Analisa o código-fonte em busca de problemas relacionados a padrões e convenções de codificação. Utiliza definições de padrões armazenadas no diretório `Assets/PerformancePatterns` e outras regras internas.

### 2. Análise de Documentação

```
AnalyzeDocumentation(projectPath, includePrivateMembers)
```

Verifica a documentação do código-fonte identificando problemas como métodos não documentados, parâmetros sem descrição, e outras falhas de documentação.

### 3. Análise de Lógica e Performance

```
AnalyzeLogic(projectPath, focusAreas)
```

Examina o código em busca de problemas de desempenho, complexidade excessiva e padrões de design inadequados.

### 4. Obtenção de Resultados

```
GetAnalysisResults(filterBySeverity, maxResults)
```

Recupera resultados detalhados da última análise realizada, com possibilidade de filtrar por nível de severidade.

## Integração com o Protocolo MCP

O projeto utiliza o pacote [ModelContextProtocol](https://modelcontextprotocol.io/) para implementar um servidor MCP que pode ser consumido por qualquer cliente compatível com o protocolo. O servidor expõe suas ferramentas de análise como endpoints MCP que podem ser invocados por:

- Assistentes de IA baseados em LLM
- Extensões de IDE
- Aplicações cliente específicas

## Requisitos

- .NET 9.0
- Pacotes:
  - Microsoft.Extensions.Hosting (9.0.4)
  - ModelContextProtocol (0.1.0-preview.7)

## Como Executar

1. Clone o repositório
2. Execute o comando:

```bash
dotnet run
```

O servidor será iniciado e estará pronto para receber conexões de clientes MCP através do protocolo de transporte stdio.

## Estrutura de Diretórios

- `Assets/`: Contém definições de padrões e regras para análise
  - `ArchitecturalPatterns/`: Padrões arquiteturais a serem verificados
  - `PerformancePatterns/`: Padrões de otimização de desempenho
  - `ProjectStructure/`: Definições de estrutura de projeto esperada
- `Models/`: Classes de modelo para representação de resultados de análise
- `Rules/`: Implementações de regras de análise para diferentes aspectos do código

## Como Estender

### Adicionando Novos Padrões de Análise

Você pode adicionar novos padrões de análise criando arquivos JSON nos respectivos diretórios em `Assets/`. Cada tipo de padrão segue uma estrutura específica:

1. Para padrões de performance: Adicione arquivos JSON em `Assets/PerformancePatterns/`
2. Para padrões arquiteturais: Adicione arquivos JSON em `Assets/ArchitecturalPatterns/`
3. Para padrões de estrutura: Adicione arquivos JSON em `Assets/ProjectStructure/`

Para mais informações sobre o formato esperado, consulte os arquivos README.md em cada diretório de padrões.

## Licença

Este projeto é distribuído sob a licença MIT.