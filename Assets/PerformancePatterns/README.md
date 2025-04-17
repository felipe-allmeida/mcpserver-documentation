# Performance Optimization Patterns

Este diretório contém arquivos de definição de padrões de otimização de performance que serão usados pela regra PerformanceOptimizationRule para identificar problemas de desempenho no código-fonte.

## Estrutura dos Arquivos de Padrão

Cada padrão é definido em um arquivo JSON com a seguinte estrutura:

```json
{
  "patternName": "Nome do padrão",
  "description": "Descrição do padrão de performance",
  "severity": "Warning|Error|Info",
  "rules": [
    {
      "type": "Algoritmo",
      "complexity": "O(n²)|O(n)|O(log n)|O(1)",
      "message": "Mensagem a ser exibida quando o padrão for violado"
    },
    {
      "type": "DataStructure",
      "recommendedStructures": ["List", "Dictionary", "HashSet"],
      "antiPatterns": ["Array", "ArrayList"],
      "message": "Mensagem sobre o uso inadequado de estruturas de dados"
    },
    {
      "type": "ResourceUsage",
      "patterns": ["using.*Disposable", "new MemoryStream"],
      "message": "Mensagem sobre o uso inadequado de recursos"
    }
  ],
  "codeExamples": {
    "good": "// Exemplo de código otimizado",
    "bad": "// Exemplo de código não otimizado"
  }
}
```

## Padrões Disponíveis

1. **LoopOptimization.json** - Identifica loops ineficientes e sugere otimizações
2. **MemoryManagement.json** - Verifica o uso adequado de recursos e gerenciamento de memória
3. **AsyncAwait.json** - Analisa o uso correto de padrões assíncronos
4. **DatabaseAccess.json** - Identifica problemas de acesso a banco de dados
5. **CollectionEfficiency.json** - Avalia o uso eficiente de coleções

## Como Adicionar Novos Padrões

1. Crie um novo arquivo JSON seguindo a estrutura acima
2. Coloque-o neste diretório
3. A regra PerformanceOptimizationRule carregará automaticamente o novo padrão

## Como os Padrões São Aplicados

A regra PerformanceOptimizationRule irá:
1. Carregar todas as definições de padrões deste diretório
2. Para cada arquivo analisado, verificar padrões de código ineficientes
3. Gerar objetos LogicIssue para qualquer violação encontrada
4. Sugerir melhorias baseadas nas definições dos padrões