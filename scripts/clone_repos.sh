#!/usr/bin/env bash
# Clona repositórios cujas URLs vêm do CSV gerado pelo MetricsCollector (coluna Url).
set -euo pipefail

CSV="data/repositorios_processo.csv"
OUT="clones"
LIMIT=0
SKIP_EXISTING=0

usage() {
  echo "Uso: $0 [--csv caminho] [--out diretorio] [--limit N] [--skip-existing]"
  exit 1
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --csv) CSV="$2"; shift 2 ;;
    --out) OUT="$2"; shift 2 ;;
    --limit) LIMIT="$2"; shift 2 ;;
    --skip-existing) SKIP_EXISTING=1; shift ;;
    -h|--help) usage ;;
    *) echo "Opção desconhecida: $1"; usage ;;
  esac
done

[[ -f "$CSV" ]] || { echo "CSV não encontrado: $CSV"; exit 1; }
mkdir -p "$OUT"

mapfile -t HDR < <(head -1 "$CSV" | tr ',' '\n')
URL_COL=-1
for i in "${!HDR[@]}"; do
  h="${HDR[$i]//$'\r'/}"
  [[ "${h,,}" == "url" ]] && URL_COL=$i && break
done
[[ "$URL_COL" -ge 0 ]] || { echo "CSV sem coluna Url."; exit 1; }

repo_name_from_url() {
  local u="$1"
  u="${u%.git}"
  u="${u%/}"
  echo "${u##*/}"
}

cloned=0
while IFS= read -r line; do
  [[ -z "${line//[[:space:]]/}" ]] && continue
  IFS=',' read -ra FIELDS <<< "$line"
  url="${FIELDS[$URL_COL]//$'\r'/}"
  [[ -z "$url" || "$url" != *"github.com"* ]] && continue

  if [[ "$LIMIT" -gt 0 && "$cloned" -ge "$LIMIT" ]]; then
    break
  fi

  name="$(repo_name_from_url "$url")"
  dest="${OUT}/${name}"
  if [[ "$SKIP_EXISTING" -eq 1 && -d "$dest" ]]; then
    echo "[skip] $dest"
    continue
  fi
  echo "[clone] $url"
  if git clone --depth 1 "$url" "$dest"; then
    ((cloned++)) || true
  else
    echo "  falha: $url" >&2
  fi
done < <(tail -n +2 "$CSV")

echo "Concluído. Repositórios clonados com sucesso: $cloned."
