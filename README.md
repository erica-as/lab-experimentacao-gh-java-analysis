# Lab - Análise de Repositórios Java (Lab 02)

Coletor em C#: lista **1.000** repositórios Java (REST Search), exporta `data/repositorios_processo.csv`, e (Lab02S01) **clona** uma amostra, corre o **CK** (CBO/DIT/LCOM) e atualiza o CSV + evidências.

## Dependências

- **.NET 10** + **Java** (8+; o CK usa JDT alinhado a Java 11 no upstream).
- **Git** no `PATH` (clone).
- **JAR do [CK](https://github.com/mauricioaniche/ck)** (com dependências), ex. [Maven Central 0.7.0](https://repo1.maven.org/maven2/com/github/mauricioaniche/ck/0.7.0/): ficheiro `ck-0.7.0-jar-with-dependencies.jar`.

## Configuração (`.env`)

Copie `.env.example` → `.env` e preencha:

| Variável | Descrição |
|----------|-----------|
| `GITHUB_TOKEN` ou `GH_TOKEN` | PAT com `public_repo` (recomendado para Search). |
| `CK_JAR` | Caminho **absoluto** ao JAR do CK (obrigatório para pipeline completo Lab02S01). |
| `JAVA_HOME` | Opcional; senão usa `java` no `PATH`. |
| `LAB02_CK_REPO_INDEX` | Opcional: índice da linha no CSV usada para clone+CK (`0` = mais estrelas; **`-1`** = última linha, em geral projeto mais leve). |
| `GITHUB_SEARCH_PAGE_DELAY_MS` | Opcional: pausa entre páginas Search (predefinido 8000 ms). |

## Executar

```bash
dotnet run --project src/MetricsCollector/MetricsCollector.csproj
```

Fluxo predefinido (**Lab02S01 completo**):

1. Obtém até **1000** repos (`language:java`, `sort=stars`), grava `data/repositorios_processo.csv`.
2. **Clone** automático (`git clone --depth 1`) para `artifacts/clones/<repo>/`.
3. Corre **CK** sobre o clone; médias **CBO / DIT / LCOM** (por classe em `class.csv`) escrita na linha da amostra no CSV principal.
4. Copia **todos os `*.csv` gerados pelo CK** + `SUMARIO.txt` para `data/lab02s01_ck_evidence/<repo>/`.

Só coleta API (sem clone/CK):

```bash
dotnet run --project src/MetricsCollector/MetricsCollector.csproj -- --collect-only
```

Só **clone + CK** (usa o `data/repositorios_processo.csv` já existente, sem chamar a API):

```bash
dotnet run --project src/MetricsCollector/MetricsCollector.csproj -- --ck-only
```

Ainda precisas de `CK_JAR` e Java; `GITHUB_TOKEN` não é obrigatório neste modo.

Tempos: ~2–3 min para 10 páginas Search; CK depende do tamanho do repo (a amostra predefinida é a **última** linha da lista para ser mais rápida).

**Nota:** `ReleasesCount` na Search REST fica **0** (não vem no JSON); podes enriquecer depois se precisares para RQ03.

## Artefactos

| Caminho | Conteúdo |
|---------|-----------|
| `data/repositorios_processo.csv` | 1000 linhas + métricas de processo; **uma** linha com `AvgCbo`/`AvgDit`/`AvgLcom` preenchidos após CK. |
| `data/lab02s01_ck_evidence/*/` | `class.csv`, `method.csv`, … + `SUMARIO.txt` (médias e nº de classes). |
| `artifacts/` | Clones e saída bruta do CK (**gitignored**). |

## Próximos passos (Lab02S02+)

Réplica CK nos 1000 repositórios, hipóteses, relatório, etc.
