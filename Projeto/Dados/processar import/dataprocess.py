#!/usr/bin/env python
"""
Cria:
  • bolseiros.csv
  • orientadores.csv

Resiliente a variações como "José Silva", "Jose Silva." ou "jose-silva".
Requisitos: pip install pandas openpyxl
"""

from __future__ import annotations

import csv
import secrets
import string
import unicodedata
from collections import defaultdict
from pathlib import Path

import pandas as pd


# ---------------------------------------------------------------------------
# Utilidades
# ---------------------------------------------------------------------------

def normalise(txt: str | None) -> str:
    """Sem acentos/pontuação, minúsculas, 1 espaço: ideal p/ matching."""
    if not txt:
        return ""
    txt = unicodedata.normalize("NFKD", txt)
    txt = "".join(ch for ch in txt if not unicodedata.combining(ch))
    txt = "".join(ch if ch.isalnum() or ch.isspace() else " " for ch in txt)
    return " ".join(txt.lower().split())


def generate_password(length: int = 12) -> str:
    alphabet = string.ascii_letters + string.digits
    return "".join(secrets.choice(alphabet) for _ in range(length))


def parse_emails(path: Path) -> dict[str, str]:
    """
    Lê emails.txt  (email-nome; …)  →  {nome_normalizado: email}
    """
    mapping: dict[str, str] = {}
    if not path.is_file():
        return mapping
    for chunk in path.read_text("utf-8", "ignore").split(";"):
        if "-" in chunk:
            email, name = chunk.split("-", 1)
            mapping[normalise(name)] = email.strip()
    return mapping


# ---------------------------------------------------------------------------
# Principal
# ---------------------------------------------------------------------------

def main() -> None:
    base = Path(__file__).resolve().parent
    excel = base / "bolseiros_atualizado.xlsx"
    emails_txt = base / "emails.txt"

    out_bols = base / "bolseiros.csv"
    out_adv = base / "orientadores.csv"

    # 1) Ler Excel
    bol = pd.read_excel(excel, sheet_name="Bolseiros")
    teams = pd.read_excel(excel, sheet_name="Equipas")

    eq_map: dict[str, list[str]] = defaultdict(list)
    for _, row in teams.iterrows():
        team = row["Equipa"]
        for nome in str(row.get("Membros", "")).split(","):
            nome = nome.strip()
            if nome:
                eq_map[normalise(nome)].append(team)

    bol["Equipa"] = bol["Bolseiro"].apply(
        lambda n: "; ".join(eq_map.get(normalise(n), []))
    )

    bol["password"] = [generate_password() for _ in range(len(bol))]

    rename = {
        "Bolseiro": "bolseiro",
        "email": "email",
        "Telefone": "telefone",
        "Início": "inicio",
        "Término": "termino",
        "Início Renova": "inicio_renova",
        "Término Renova": "termino_renova",
        "Orientador": "orientador",
        "Coorientador 1": "coorientador1",
        "Coorientador 2": "coorientador2",
        "Tipo": "tipo",
        "Equipa": "equipa",
    }
    bol = bol.rename(columns=rename)
    bol = bol[list(rename.values()) + ["password"]]

    bol["telefone"] = bol["telefone"].astype(str).str.replace(r"\s+", "", regex=True)
    for c in ["inicio", "termino", "inicio_renova", "termino_renova"]:
        bol[c] = pd.to_datetime(bol[c], errors="coerce").dt.strftime("%Y-%m-%d")

    bol.to_csv(out_bols, index=False, encoding="utf-8-sig",
               quoting=csv.QUOTE_NONNUMERIC)


    adv_to_students: dict[str, set[str]] = defaultdict(set)
    adv_original: dict[str, str] = {}          # nome_norm → primeiro nome visto

    for _, r in bol.iterrows():
        aluno = r["bolseiro"]
        for col in ["orientador", "coorientador1", "coorientador2"]:
            nome = r.get(col)
            if pd.notna(nome) and nome:
                key = normalise(nome)
                adv_to_students[key].add(aluno)
                adv_original.setdefault(key, nome)  # guarda 1.º original

    # 9) emails
    email_map = parse_emails(emails_txt)

    # 10) DataFrame
    rows = []
    for key, alunos in adv_to_students.items():
        rows.append({
            "orientador": adv_original[key],
            "orientandos": "; ".join(sorted(alunos)),
            "email": email_map.get(key, ""),
            "password": generate_password(),
        })

    adv_df = pd.DataFrame(rows)

    # aviso faltas de email
    miss = adv_df[adv_df["email"] == ""]
    if not miss.empty:
        print("⚠️  Orientadores sem email no emails.txt:")
        for n in miss["orientador"]:
            print(f"   - {n}")

    # 11) orientadores.csv
    adv_df.to_csv(out_adv, index=False, encoding="utf-8-sig",
                  quoting=csv.QUOTE_NONNUMERIC)

    print(f"✔  bolseiros.csv -> {out_bols}")
    print(f"✔  orientadores.csv -> {out_adv}")


if __name__ == "__main__":
    main()
