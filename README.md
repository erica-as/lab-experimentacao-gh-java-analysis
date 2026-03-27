# Lab - Análise de Repositórios Java (Lab 02)

Este repositório contém o coletor de métricas de processo de repositórios Java no GitHub e exporta resultados em CSV.

Resumo:

- Código em C#: `src/MetricsCollector` — metadados (estrelas, idade, releases).
- Configuração: `.env` (não comitar) com `GITHUB_TOKEN`.

## Sobre o Lab 02

Objetivo: coletar os top-1.000 repositórios Java (ou uma amostra representativa) e extrair métricas de processo que serão usadas nas questões de pesquisa:

- RQ01: relação entre popularidade (estrelas) e qualidade
- RQ02: relação entre maturidade (idade) e qualidade
- RQ03: relação entre atividade (releases) e qualidade
- RQ04: relação entre tamanho (LOC) e qualidade

O código atual coleta popularidade, maturidade e número de releases. Métricas de qualidade (CBO/DIT/LCOM) e LOC exigem clonar o código e executar análise estática (próximas tarefas).

## Configuração

1. Gere um Personal Access Token (PAT) no GitHub com o escopo `public_repo`.
2. Crie um arquivo `.env` na raiz do repositório com o conteúdo:

```
GITHUB_TOKEN="seu_token_aqui"
```

O arquivo `.env` já está listado em `.gitignore`.

## Executar o coletor C#

Abra um terminal na raiz do repositório e execute:

```bash
dotnet run --project src/MetricsCollector/MetricsCollector.csproj
```

Observações:

- O coletor procura o `.env` automaticamente subindo a árvore de diretórios, então execute o comando a partir da pasta raiz ou de `src/MetricsCollector`.
- O CSV gerado será salvo em `data/repositorios_processo.csv` (pasta criada automaticamente na raiz do repositório).
- A coleta faz chamadas à API do GitHub e respeita pausas para ajudar a evitar rate limits; com um token válido o limite é maior (5000/h).

## Próximos passos e notas

- Para coletar métricas de qualidade (CBO/DIT/LCOM) e LOC, será necessário clonar os repositórios e rodar uma ferramenta de análise de código Java (por exemplo, `ckjm`, `sonar` ou `scitools`).
- Também é possível migrar a coleta para a API GraphQL (mais flexível para consultas) ou adicionar paginação dentro de cada intervalo de estrelas para aumentar a amostra.

