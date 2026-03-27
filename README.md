# Lab - Análise de Repositórios Java (Lab 02)

Este repositório contém o coletor de métricas de processo de repositórios Java no GitHub e exporta resultados em CSV.

Resumo:

- Código em C#: `src/MetricsCollector` — busca **GraphQL** com **paginação por cursor** até 1.000 repositórios Java (estrelas, idade, contagem de releases).
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
- A coleta usa o endpoint GraphQL (`/graphql`), pagina com `after`/`pageInfo.endCursor` e faz uma pausa curta entre páginas. Com um PAT válido o limite da API costuma ser confortável para ~10 páginas (1000 repos).

## Próximos passos e notas

- Para métricas de qualidade (CBO/DIT/LCOM) e LOC, é preciso clonar os repositórios e rodar análise estática (ex.: CK).

