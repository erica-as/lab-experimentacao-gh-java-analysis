import csv
import os
import argparse
import subprocess
import sys


def parse_args():
    p = argparse.ArgumentParser(description="Clonar repositórios listados em um CSV")
    p.add_argument("--csv", default="data/repositorios_processo.csv", help="Caminho para o CSV gerado")
    p.add_argument("--out", default="clones", help="Diretório de saída onde os repositórios serão clonados")
    p.add_argument("--limit", type=int, default=0, help="Limitar número de repositórios a clonar (0 = sem limite)")
    p.add_argument("--skip-existing", action="store_true", help="Pular repositórios que já existem na pasta de saída")
    return p.parse_args()


def read_clone_urls(csv_path):
    urls = []
    if not os.path.exists(csv_path):
        print(f"Arquivo CSV não encontrado: {csv_path}")
        return urls

    with open(csv_path, newline='', encoding='utf-8') as f:
        reader = csv.DictReader(f)
        if 'Url' not in reader.fieldnames and 'url' not in [h.lower() for h in reader.fieldnames]:
            print("CSV não contém coluna 'Url'. Verifique o arquivo.")
            print("Cabeçalhos encontrados:", reader.fieldnames)
            return urls

        url_key = next((h for h in reader.fieldnames if h.lower() == 'url'), 'Url')

        for row in reader:
            val = row.get(url_key)
            if val:
                urls.append(val.strip())
    return urls


def repo_dir_name_from_url(url):
    name = url.rstrip('/').split('/')[-1]
    if name.endswith('.git'):
        name = name[:-4]
    return name


def clone_repo(url, out_dir):
    name = repo_dir_name_from_url(url)
    dest = os.path.join(out_dir, name)
    if os.path.exists(dest):
        return False, f"exists: {dest}"

    cmd = ['git', 'clone', url, dest]
    try:
        subprocess.check_call(cmd)
        return True, f"cloned: {name}"
    except subprocess.CalledProcessError as e:
        return False, f"error cloning {url}: {e}"


def main():
    args = parse_args()
    urls = read_clone_urls(args.csv)
    if not urls:
        print("Nenhuma URL para clonar. Abortando.")
        sys.exit(1)

    os.makedirs(args.out, exist_ok=True)

    total = len(urls)
    limit = args.limit if args.limit and args.limit > 0 else total
    print(f"Serão processadas até {limit} de {total} repositórios (pasta de saída: {args.out})")

    count = 0
    for i, url in enumerate(urls):
        if count >= limit:
            break
        print(f"[{i+1}/{total}] {url}")
        name = repo_dir_name_from_url(url)
        dest = os.path.join(args.out, name)
        if args.skip_existing and os.path.exists(dest):
            print(f"  Pulando (já existe): {dest}")
            continue

        ok, msg = clone_repo(url, args.out)
        print(" ", msg)
        if ok:
            count += 1

    print(f"Concluído. {count} repositórios clonados.")


if __name__ == '__main__':
    main()
