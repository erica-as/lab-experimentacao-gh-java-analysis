Coloca aqui o **`ck-0.7.0-jar-with-dependencies.jar`** (ferramenta CK, não é o JDK).

**Download:**

```bash
curl -fsSL -O "https://repo1.maven.org/maven2/com/github/mauricioaniche/ck/0.7.0/ck-0.7.0-jar-with-dependencies.jar"
```

No `.env`, na raiz do repositório:

```bash
CK_JAR="/caminho/absoluto/.../lab-experimentacao-gh-java-analysis/tools/ck/ck-0.7.0-jar-with-dependencies.jar"
```

O ficheiro `.jar` está no `.gitignore` para não ir para o Git (binário grande).
