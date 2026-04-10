# Lab - Análise de Repositórios Java (Lab 02)

Coletor em C#: lista **1.000** repositórios Java (REST Search), exporta `data/repositorios_processo.csv` e integra o **CK** (CBO/DIT/LCOM) com clone automático e evidências em `data/lab02s01_ck_evidence/`. Os modos de execução estão descritos abaixo.

## Dependências

- **.NET 10** + **Java** (8+; o CK usa JDT alinhado a Java 11 no upstream).
- **Git** no `PATH` (clone).
- **JAR do [CK](https://github.com/mauricioaniche/ck)** em `tools/ck/ck-0.7.0-jar-with-dependencies.jar` (instruções em [`tools/ck/README.md`](tools/ck/README.md); o `.jar` não é versionado no Git).

### O que é cada coisa (e SDKMAN)

- **`CK_JAR`** — Não tem nada a ver com Java SDK. É o arquivo **`.jar`** que você baixa (link acima → `jar-with-dependencies`). Salve onde quiser (ex.: `~/tools/ck-0.7.0-jar-with-dependencies.jar`) e no `.env` coloque o **caminho absoluto** para esse arquivo.
- **Java com [SDKMAN](https://sdkman.io/)** — Instale e ative uma versão, por exemplo:
  ```bash
  sdk install java 17.0.13-tem
  sdk use java 17.0.13-tem
  ```
  Na mesma shell, `java -version` deve funcionar. O SDKMAN costuma exportar **`JAVA_HOME`** (algo como `~/.sdkman/candidates/java/17.0.13-tem`).  
  **Se rodar `dotnet run` em outro lugar** (IDE, CI) onde o SDKMAN não carregou, coloque no `.env`:
  ```bash
  JAVA_HOME="/Users/seuusuario/.sdkman/candidates/java/17.0.13-tem"
  ```
  O programa usa primeiro `JAVA_HOME/bin/java`; caso contrário procura `java` no `PATH`.
- **Git** — `git` global (Homebrew, Xcode CLT, etc.), não é o SDKMAN.

## Configuração (`.env`)

Copie `.env.example` → `.env` e preencha:

| Variável | Obrigatória | Descrição |
|----------|:-----------:|-----------|
| `GITHUB_TOKEN` ou `GH_TOKEN` | Sim¹ | PAT com `public_repo` (recomendado para Search). |
| `CK_JAR` | Sim² | Caminho **absoluto** ao JAR do CK. |
| `JAVA_HOME` | Não | Caminho ao JDK; caso contrário usa `java` no `PATH`. |
| `LAB02_CK_REPO_INDEX` | Não | Índice da linha para amostra CK (`0` = mais estrelas; `-1` = última). |
| `GITHUB_SEARCH_PAGE_DELAY_MS` | Não | Pausa entre páginas Search (padrão 8000 ms; mín. 1000 ms). |
| `LAB02_KEEP_ARTIFACTS` | Não | `1` para preservar clones e saída CK em `artifacts/` após execução. |
| `LAB02_CK_PARALLEL` | Não | Nº de repos processados em paralelo em `--ck-all` e `--loc-all` (padrão `1`). |
| `LAB02_JVM_MAX_HEAP` | Não | Heap máximo por JVM ao rodar o CK (ex.: `1g`, `512m`). Necessário com `LAB02_CK_PARALLEL > 1` para evitar OOM. |

> ¹ Dispensável nos modos `--ck-only` e `--loc-only`.  
> ² Obrigatório em qualquer modo que execute o CK.

#### Configuração recomendada para batch rápido (ex.: MacBook M4 16 GB)

```bash
LAB02_CK_PARALLEL=8
LAB02_JVM_MAX_HEAP=1g
```

Conta de memória: 8 workers × 1 GB heap = 8 GB para JVMs; o restante fica para o SO, clones e .NET. Reduza `LAB02_CK_PARALLEL` se aparecer swap. Para `--loc-only` (sem JVM) pode usar até `LAB02_CK_PARALLEL=12`.

## Executar

O comando base é sempre o mesmo projeto; o que muda são os argumentos após `--`.

```bash
dotnet run --project src/MetricsCollector/MetricsCollector.csproj
```

### Matriz de modos

| Flags | Chama Search API | Pré-requisitos | Saída principal |
|-------|-----------------|----------------|-----------------|
| _(nenhuma)_ | Sim | `GITHUB_TOKEN`, `CK_JAR`, Java | CSV + amostra CK + evidência |
| `--collect-only` | Sim | `GITHUB_TOKEN` | CSV (sem CK) |
| `--ck-only` | Não | `CK_JAR`, Java | Amostra CK (usa CSV existente) |
| `--ck-only --ck-all` | Não | `CK_JAR`, Java | CK em todas as linhas do CSV |
| `--ck-only --ck-all --ck-resume` | Não | `CK_JAR`, Java | Idem, pulando linhas com `CkClassRows > 0` |
| `--ck-only --ck-all --ck-evidence` | Não | `CK_JAR`, Java, disco | Idem + evidência por repo |
| `--loc-only --loc-all` | Não | Git | LOC e comentários em todas as linhas |
| `--loc-only --loc-all --loc-resume` | Não | Git | Idem, pulando linhas com `TotalLoc > 0` |

> **Atenção:** `--ck-all` **sem** `--ck-only` chama a Search API primeiro, atualiza/sobrescreve o CSV e só então roda o CK. Use sempre `--ck-only --ck-all` quando quiser batch sobre o CSV existente.

Sem flags adicionais, o programa faz o **percurso completo do Lab02S01**: chama a Search API até mil repositórios Java, grava `data/repositorios_processo.csv`, clona **uma** amostra (`git clone --depth 1`, por padrão a última linha da lista), roda o CK e retorna as médias CBO/DIT/LCOM nessa linha. Copia também os `*.csv` do CK e o `SUMARIO.txt` para `data/lab02s01_ck_evidence/<nome_SAFE>/`. Você precisa de `GITHUB_TOKEN` (ou `GH_TOKEN`) no `.env` para a coleta e de `CK_JAR` + Java para o CK.

Se quiser **apenas** popular ou atualizar o CSV a partir da API, sem clone nem CK, use `--collect-only`.

```bash
dotnet run --project src/MetricsCollector/MetricsCollector.csproj -- --collect-only
```

Quando o CSV já existe e você não quer **voltar a bater na Search**, `--ck-only` lê `data/repositorios_processo.csv` e executa só a parte de Git + CK.

```bash
dotnet run --project src/MetricsCollector/MetricsCollector.csproj -- --ck-only
```

Para rodar o CK **em todas as linhas** a partir do CSV existente, combine `--ck-only --ck-all`. O programa grava o arquivo principal após cada repositório com sucesso e preenche `CkClassRows`; com `--ck-resume` pula linhas onde `CkClassRows > 0`. Se precisar da evidência por repositório, inclua `--ck-evidence`.

```bash
dotnet run --project src/MetricsCollector/MetricsCollector.csproj -- --ck-only --ck-all --ck-evidence
dotnet run --project src/MetricsCollector/MetricsCollector.csproj -- --ck-only --ck-all --ck-evidence --ck-resume
```

Para preencher as colunas `TotalLoc` e `CommentLines` (necessárias para RQ04), use o modo LOC. Ele faz clone shallow por repositório, conta as linhas em `*.java` e apaga o clone (a menos que `LAB02_KEEP_ARTIFACTS=1`).

```bash
dotnet run --project src/MetricsCollector/MetricsCollector.csproj -- --loc-only --loc-all
dotnet run --project src/MetricsCollector/MetricsCollector.csproj -- --loc-only --loc-all --loc-resume
```

**Códigos de saída:** `0` = sucesso, `1` = flags inválidas, `2` = CSV ausente/vazio, `3` = CK_JAR ou Java não encontrado, `4` = batch com falhas parciais (use `--ck-resume` / `--loc-resume` para retomar).

**Nota:** na REST de Search, `ReleasesCount` costuma ficar 0 porque o campo não vem no JSON; é limitação da coleta, não do CK.

**Tempos indicativos** (M4 16 GB, `LAB02_CK_PARALLEL=8`, `LAB02_JVM_MAX_HEAP=1g`):

| Operação | Estimativa |
|----------|-----------|
| `--collect-only` (10 páginas Search) | ~2–5 min |
| `--ck-only --ck-all` (1000 repos) | ~3–5 h |
| `--loc-only --loc-all` (1000 repos, `PARALLEL=12`) | ~25–35 min |

Monitoramento de progresso (outro terminal):
```bash
while true; do
  awk -F',' 'NR>1 && $10>0 {n++} END{print n"/1000 com CK"}' data/repositorios_processo.csv
  sleep 30
done
```

## Artefatos

| Caminho | Conteúdo | Git |
|---------|-----------|-----|
| `data/repositorios_processo.csv` | 1000 linhas + métricas; `AvgCbo`/`AvgDit`/`AvgLcom`, `CkClassRows`, `TotalLoc`, `CommentLines`. | versionado |
| `data/lab02s01_ck_evidence/*/` | `class.csv`, `method.csv`, … + `SUMARIO.txt` (só com `--ck-evidence`). | **gitignored** |
| `data/*.zip` | Arquivos compactados de evidência — artefatos pesados locais. | **gitignored** |
| `artifacts/` | Clones e saída bruta do CK (apagados por padrão; `LAB02_KEEP_ARTIFACTS=1` retém). | **gitignored** |

### Dados pesados locais (zip/deszip)

Os arquivos de evidência CK são grandes demais para versionar no GitHub. O fluxo recomendado para trabalhar localmente:

```bash
# Descompactar evidência na pasta correta (só local, não commitar)
unzip data/lab02s01_ck_evidence.zip -d data/

# Verificar estrutura (deve listar subpastas por repo)
ls data/lab02s01_ck_evidence/ | head -5
```

O `.gitignore` já exclui `data/lab02s01_ck_evidence/` e `data/*.zip`. Não adicione esses caminhos ao staging.

## Análise (Lab02S02)

Com o CSV completo, rode o pipeline de análise em `analysis/`:

```bash
cd analysis
pip3 install -r requirements.txt
python3 analyze.py
```

O script gera estatísticas descritivas e correlações de Spearman para RQ01–RQ04, além de gráficos em `analysis/figures/`. Veja [`docs/hipoteses.md`](docs/hipoteses.md) para o enunciado formal das hipóteses e [`docs/relatorio_lab02s02.md`](docs/relatorio_lab02s02.md) para o relatório final.