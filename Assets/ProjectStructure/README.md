# Padrões de Estrutura de Projeto

Este diretório contém arquivos de definição para padrões de estrutura de projeto que serão usados pela regra ProjectStructureRule para identificar problemas relacionados à organização e estrutura do código-fonte.

## Estrutura dos Arquivos de Padrão

Cada padrão é definido em um arquivo JSON com a seguinte estrutura:

```json
{
  "patternName": "Nome do padrão",
  "description": "Descrição do padrão de estrutura",
  "severity": "Warning|Error|Info",
  "rules": [
    {
      "type": "FolderStructure",
      "expectedFolders": ["Models", "Controllers", "Views"],
      "message": "Mensagem a ser exibida quando pastas obrigatórias estiverem faltando"
    },
    {
      "type": "FileLocation",
      "filePatterns": {
        "Model$": "Models/",
        "Controller$": "Controllers/",
        "View$": "Views/"
      },
      "message": "Mensagem sobre arquivos em locais incorretos"
    },
    {
      "type": "Namespace",
      "namespacePattern": "^Company\\.Product(\\..*)?$",
      "message": "Mensagem sobre namespaces irregulares"
    }
  ],
  "exemptionPatterns": [
    "test/.*",
    "example/.*"
  ]
}
```

## Padrões Disponíveis

1. **StandardProject.json** - Define a estrutura padrão para projetos .NET
2. **MVCStructure.json** - Define a estrutura esperada para projetos MVC
3. **MicroserviceStructure.json** - Define a estrutura recomendada para microserviços
4. **DomainDrivenDesign.json** - Define a estrutura para projetos que seguem DDD

## Como Adicionar Novos Padrões

1. Crie um novo arquivo JSON seguindo a estrutura acima
2. Coloque-o neste diretório
3. A regra ProjectStructureRule carregará automaticamente o novo padrão

## Como os Padrões São Aplicados

A regra ProjectStructureRule irá:
1. Carregar todas as definições de padrões deste diretório
2. Analisar a estrutura de diretórios e arquivos do projeto
3. Verificar se os arquivos estão em conformidade com os padrões definidos
4. Gerar objetos CodeIssue para qualquer violação encontrada
5. Fornecer sugestões sobre como corrigir os problemas de estrutura