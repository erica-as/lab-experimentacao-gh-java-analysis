# Lab - Análise de Repositórios Java (Lab 02)

Coletor em C#: lista **1.000** repositórios Java (REST Search), exporta `data/repositorios_processo.csv` e integra o **CK** (CBO/DIT/LCOM) com clone automático e evidências em `data/lab02s01_ck_evidence/`. Os modos de execução estão descritos abaixo.

## Dependências

- **.NET 10** + **Java** (8+; o CK usa JDT alinhado a Java 11 no upstream).
- **Git** no `PATH` (clone).
- **JAR do [CK](https://github.com/mauricioaniche/ck)** em `tools/ck/ck-0.7.0-jar-with-dependencies.jar` (instruções em [`tools/ck/README.md`](tools/ck/README.md); o `.jar` não é versionado no Git).

### O que é cada coisa (e SDKMAN)

- **`CK_JAR`** — Não tem nada a ver com Java SDK. É o ficheiro **`.jar`** que descarregas (link acima → `jar-with-dependencies`). Grava-o onde quiseres (ex. `~/tools/ck-0.7.0-jar-with-dependencies.jar`) e no `.env` pões o **caminho absoluto** a esse ficheiro.
- **Java com [SDKMAN](https://sdkman.io/)** — Instala e activa uma versão, por exemplo:
  ```bash
  sdk install java 17.0.13-tem
  sdk use java 17.0.13-tem
  ```
  Na mesma shell, `java -version` deve funcionar. O SDKMAN costuma exportar **`JAVA_HOME`** (algo como `~/.sdkman/candidates/java/17.0.13-tem`).  
  **Se correres `dotnet run` noutro sítio** (IDE, CI) onde o SDKMAN não carregou, coloca no `.env`:
  ```bash
  JAVA_HOME="/Users/teuuser/.sdkman/candidates/java/17.0.13-tem"
  ```
  O programa usa primeiro `JAVA_HOME/bin/java`; senão procura `java` no `PATH`.
- **Git** — `git` global (Homebrew, Xcode CLT, etc.), não é o SDKMAN.

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

O comando base é sempre o mesmo projeto; o que muda são os argumentos após `--`.

```bash
dotnet run --project src/MetricsCollector/MetricsCollector.csproj
```

Sem flags adicionais, o programa faz o **percurso completo do Lab02S01**: chama a Search API até mil repositórios Java, grava `data/repositorios_processo.csv`, clona **uma** amostra (`git clone --depth 1`, por omissão a última linha da lista), corre o CK e devolve médias CBO/DIT/LCOM nessa linha. Em paralelo copia os `*.csv` do CK e o `SUMARIO.txt` para `data/lab02s01_ck_evidence/<nome_SAFE>/`. Precisas de `GITHUB_TOKEN` (ou `GH_TOKEN`) no `.env` para a coleta e de `CK_JAR` + Java para o CK.

Se quiseres **apenas** popular ou atualizar o CSV a partir da API, sem clone nem CK, usa `--collect-only`—útil para iterar na lista dos mil sem gastar tempo em métricas.

```bash
dotnet run --project src/MetricsCollector/MetricsCollector.csproj -- --collect-only
```

Quando o CSV já existe e não queres **voltar a bater na Search**, `--ck-only` lê `data/repositorios_processo.csv` e executa só a parte de Git + CK. **Não** precisas de token GitHub neste modo; mantém `CK_JAR` e Java configurados. O comportamento por omissão continua a ser **uma** amostra com evidência na pasta acima.

```bash
dotnet run --project src/MetricsCollector/MetricsCollector.csproj -- --ck-only
```

Para correr o CK **em todas as linhas** a partir desse CSV (sem nova coleta), acrescenta `--ck-all`. O programa grava o ficheiro principal após cada repositório com sucesso e preenche `CkClassRows`, o que permite retomar um batch longo: com `--ck-resume` ignoras entradas em que `CkClassRows` já indica um CK concluído. Por omissão o batch só atualiza métricas no CSV; se precisares da **mesma estrutura de evidência que na amostra** (`*.csv` + `SUMARIO.txt` por repositório), junta `--ck-evidence` (há custo grande em disco e tempo face a um único clone).

```bash
dotnet run --project src/MetricsCollector/MetricsCollector.csproj -- --ck-only --ck-all --ck-evidence
dotnet run --project src/MetricsCollector/MetricsCollector.csproj -- --ck-only --ck-all --ck-evidence --ck-resume
```

A variável `LAB02_CK_REPO_INDEX` continua a aplicar-se ao modo **amostra** (sem `--ck-all`). Para a Search entre pedidos podes afinar `GITHUB_SEARCH_PAGE_DELAY_MS` se aparecerem limites secundários da API.

**Nota rápida:** na REST de Search, `ReleasesCount` no CSV costuma ficar a zero porque o campo não vem no JSON; é uma limitação da coleta, não do CK. Tempos indicativos: alguns minutos para as dez páginas de resultados; cada CK depende do tamanho do repo; mil vezes CK pode levar muitas horas.

## Artefactos

| Caminho | Conteúdo |
|---------|-----------|
| `data/repositorios_processo.csv` | 1000 linhas + métricas; `AvgCbo`/`AvgDit`/`AvgLcom` + `CkClassRows` (amostra ou todas com `--ck-all`). |
| `data/lab02s01_ck_evidence/*/` | `class.csv`, `method.csv`, … + `SUMARIO.txt` (médias e nº de classes). |
| `artifacts/` | Clones e saída bruta do CK (**gitignored**). |

## Próximos passos (Lab02S02+)

Análise sobre as métricas agregadas (hipóteses, correlações, relatório), eventualmente após um `--ck-all` completo.
