"""
Lab02S02 — Análise estatística: RQ01-RQ04
Lê data/repositorios_processo.csv e produz:
  - Estatísticas descritivas (mediana, média, DP) das variáveis de processo e produto
  - Correlações de Spearman para cada RQ
  - Scatter plots salvos em analysis/figures/
"""

from __future__ import annotations

import os
import sys
from pathlib import Path

import matplotlib.pyplot as plt
import pandas as pd
import seaborn as sns
from scipy import stats

# ---------------------------------------------------------------------------
# Paths
# ---------------------------------------------------------------------------
REPO_ROOT = Path(__file__).resolve().parent.parent
CSV_PATH = REPO_ROOT / "data" / "repositorios_processo.csv"
FIGURES_DIR = Path(__file__).resolve().parent / "figures"
FIGURES_DIR.mkdir(exist_ok=True)

# ---------------------------------------------------------------------------
# RQs definition
# ---------------------------------------------------------------------------
QUALITY_METRICS = ["AvgCbo", "AvgDit", "AvgLcom"]
QUALITY_LABELS = {"AvgCbo": "CBO (médio)", "AvgDit": "DIT (médio)", "AvgLcom": "LCOM (médio)"}

RQS = [
    {
        "id": "RQ01",
        "title": "Popularidade × Qualidade",
        "x_col": "Stars",
        "x_label": "Estrelas (Stars)",
        "filter_col": None,
    },
    {
        "id": "RQ02",
        "title": "Maturidade × Qualidade",
        "x_col": "AgeInYears",
        "x_label": "Idade (anos)",
        "filter_col": None,
    },
    {
        "id": "RQ03",
        "title": "Atividade × Qualidade",
        "x_col": "ReleasesCount",
        "x_label": "Releases",
        "filter_col": None,
    },
    {
        "id": "RQ04",
        "title": "Tamanho × Qualidade",
        "x_col": "TotalLoc",
        "x_label": "LOC total (linhas .java)",
        "filter_col": "TotalLoc",  # requer TotalLoc > 0
    },
]


def load_data() -> pd.DataFrame:
    if not CSV_PATH.exists():
        sys.exit(f"CSV não encontrado: {CSV_PATH}\nRode a coleta antes: dotnet run … -- --collect-only")

    df = pd.read_csv(CSV_PATH)
    before = len(df)
    df = df[df["CkClassRows"] > 0].copy()
    print(f"CSV: {before} linhas totais; {len(df)} com CK válido (CkClassRows > 0).\n")

    loc_ok = (df["TotalLoc"] > 0).sum() if "TotalLoc" in df.columns else 0
    if loc_ok == 0:
        print(
            "Aviso: coluna TotalLoc ausente ou zerada em todas as linhas.\n"
            "  RQ04 ficará vazia. Rode: dotnet run … -- --loc-only --loc-all --loc-resume\n"
        )
    else:
        print(f"TotalLoc disponível em {loc_ok}/{len(df)} linhas com CK.\n")

    return df


def descriptive_stats(df: pd.DataFrame) -> None:
    cols = ["Stars", "AgeInYears", "ReleasesCount"] + QUALITY_METRICS
    if "TotalLoc" in df.columns:
        cols += ["TotalLoc", "CommentLines"]

    print("=" * 60)
    print("ESTATÍSTICAS DESCRITIVAS")
    print("=" * 60)
    summary = df[cols].agg(["median", "mean", "std"]).round(4)
    summary.index = ["Mediana", "Média", "Desvio Padrão"]
    print(summary.to_string())
    print()


def spearman_rq(df: pd.DataFrame, rq: dict) -> None:
    x_col = rq["x_col"]
    filter_col = rq["filter_col"]

    subset = df.copy()
    if filter_col and filter_col in subset.columns:
        subset = subset[subset[filter_col] > 0]

    if x_col not in subset.columns or len(subset) < 5:
        print(f"[{rq['id']}] {rq['title']}: dados insuficientes (n={len(subset)}). Pulando.\n")
        return

    print(f"[{rq['id']}] {rq['title']} (n={len(subset)})")
    print("-" * 50)

    fig, axes = plt.subplots(1, len(QUALITY_METRICS), figsize=(5 * len(QUALITY_METRICS), 4))
    fig.suptitle(f"{rq['id']}: {rq['title']}", fontsize=12)

    for ax, y_col in zip(axes, QUALITY_METRICS):
        valid = subset[[x_col, y_col]].dropna()
        r, p = stats.spearmanr(valid[x_col], valid[y_col])
        sig = "***" if p < 0.001 else ("**" if p < 0.01 else ("*" if p < 0.05 else "ns"))
        print(f"  {QUALITY_LABELS[y_col]}: r={r:.4f}, p={p:.4e} {sig}")

        sns.regplot(
            data=valid,
            x=x_col,
            y=y_col,
            ax=ax,
            scatter_kws={"alpha": 0.3, "s": 15},
            line_kws={"color": "red"},
        )
        ax.set_xlabel(rq["x_label"])
        ax.set_ylabel(QUALITY_LABELS[y_col])
        ax.set_title(f"r={r:.3f} {sig}")

    plt.tight_layout()
    out = FIGURES_DIR / f"{rq['id'].lower()}.png"
    plt.savefig(out, dpi=120)
    plt.close()
    print(f"  -> figura: {out.relative_to(REPO_ROOT)}\n")


def main() -> None:
    df = load_data()
    descriptive_stats(df)

    print("=" * 60)
    print("CORRELAÇÕES DE SPEARMAN POR RQ")
    print("=" * 60)
    print("Legenda: *** p<0.001  ** p<0.01  * p<0.05  ns não significativo\n")

    for rq in RQS:
        spearman_rq(df, rq)

    print(f"Figuras salvas em: {FIGURES_DIR.relative_to(REPO_ROOT)}/")


if __name__ == "__main__":
    main()
