# directus_import.py
# Script para importar dados de orientadores e bolseiros para Directus
# Carrega os CSVs gerados por dataprocess.py e cria usuários e itens nas coleções:
# orientadores, persons, persons_coorientadores, Bolsas e teams.

import requests
import pandas as pd
import logging
from datetime import datetime
from typing import Optional, Tuple
import unicodedata


# Configuração da API Directus
API_TOKEN = "SMP_7T_m9JYqepUfCWShKy3lB5HReZzv"
API_URL = "http://localhost:8055"
HEADERS = {"Authorization": f"Bearer {API_TOKEN}", "Content-Type": "application/json"}

# Setup de Logging
logging.basicConfig(level=logging.DEBUG, format='%(asctime)s - %(levelname)s - %(message)s')

# IDs de Roles no Directus
ROLE_TEAM_LEADER = "8bc6e86f-9c88-4e76-a17a-cc46e021700a"
ROLE_ORIENTADOR = "0d538cd8-1612-418c-b6dc-0c44b18171c6"
ROLE_BOLSEIRO   = "9b5a5c2b-9eaa-43d1-8c41-6e6210bb84fe"
ROLE_TECNICO    = "74dd1f57-5ca0-4421-8358-9cc076d69ba9"


def normalize(txt: str | None) -> str:
    """Remove acentos, pontuação, minúsculas, espaços redundantes."""
    if txt is None or pd.isna(txt):
        return ""
    s = str(txt)
    s = unicodedata.normalize("NFKD", s)
    s = "".join(ch for ch in s if not unicodedata.combining(ch))
    s = "".join(ch if ch.isalnum() or ch.isspace() else " " for ch in s)
    return " ".join(s.lower().split())

def parse_name(full_name: str) -> Tuple[str, str]:
    """Separa nome completo em first_name e last_name"""
    parts = full_name.strip().split()
    first = parts[0]
    last = " ".join(parts[1:]) if len(parts) > 1 else ""
    logging.debug(f"Parsed name '{full_name}' => first_name='{first}', last_name='{last}'")
    return first, last


def get_existing_user(email: str) -> Optional[str]:
    """Retorna o ID de um usuário existente baseado no email"""
    params = {"filter[email][_eq]": email}
    resp = requests.get(f"{API_URL}/users", params=params, headers=HEADERS)
    resp.raise_for_status()
    data = resp.json().get("data", [])
    if data:
        user_id = data[0]["id"]
        logging.info(f"Found existing user by email: {email} -> id={user_id}")
        return user_id
    return None


def create_user(email: str, password: str, role: str,
                first_name: Optional[str] = None,
                last_name: Optional[str] = None) -> str:
    """Cria ou obtém um usuário no Directus e retorna o ID"""
    email = email.strip()
    payload = {"email": email, "password": password, "status": "active", "role": role}
    if first_name: payload["first_name"] = first_name
    if last_name:  payload["last_name"] = last_name

    logging.info(f"[create_user] Attempt payload: {payload}")
    resp = requests.post(f"{API_URL}/users", json=payload, headers=HEADERS)
    try:
        resp.raise_for_status()
        user_id = resp.json()["data"]["id"]
        logging.info(f"[create_user] Created user id={user_id}")
        return user_id
    except requests.exceptions.HTTPError:
        if resp.status_code == 400 and "RECORD_NOT_UNIQUE" in resp.text:
            logging.warning(f"Email já existe, buscando usuário existente: {email}")
            existing = get_existing_user(email)
            if existing:
                return existing
        logging.error(f"[create_user] Fail: status={resp.status_code}, response={resp.text}")
        raise


def create_item(collection: str, data: dict) -> str:
    """Cria um item em uma coleção e retorna o ID gerado"""
    clean_data = {k: (None if pd.isna(v) else v) for k, v in data.items()}
    logging.info(f"[create_item] Collection='{collection}', Data={clean_data}")
    resp = requests.post(f"{API_URL}/items/{collection}", json=clean_data, headers=HEADERS)
    try:
        resp.raise_for_status()
        item_id = resp.json()["data"]["id"]
        logging.info(f"[create_item] Success: {collection} id={item_id}")
        return item_id
    except requests.exceptions.HTTPError:
        logging.error(f"[create_item] Fail for '{collection}': status={resp.status_code}, response={resp.text}")
        raise


def get_or_create_team(name: Optional[str]) -> Optional[str]:
    """Busca equipe por nome ou cria uma nova"""
    logging.debug(f"[get_or_create_team] Input name: {name}")
    if not name or pd.isna(name):
        return None
    name_str = str(name).strip()
    if not name_str:
        return None
    logging.info(f"[get_or_create_team] Lookup: {name_str}")
    params = {"filter[name][_eq]": name_str}
    resp = requests.get(f"{API_URL}/items/teams", params=params, headers=HEADERS)
    resp.raise_for_status()
    items = resp.json().get("data", [])
    if items:
        team_id = items[0]["id"]
        logging.info(f"Found team id={team_id}")
        return team_id
    logging.info(f"Creating team: {name_str}")
    return create_item("teams", {"name": name_str})


def main():
    logging.info("=== Import Process Started ===")

    # Importar orientadores
    ori_df = pd.read_csv("orientadores.csv")
    logging.info(f"Loaded orientadores: {len(ori_df)}")
    orient_map: dict[str, str] = {}
    for _, row in ori_df.iterrows():
        orig = row["orientador"]
        key  = normalize(orig)
        first, last = parse_name(orig)
        user_id = create_user(row["email"], row["password"], ROLE_ORIENTADOR, first, last)
        orient_id = create_item("orientadores", {
            "user": user_id,
            "first_name": first,
            "last_name": last,
            "email": row["email"]
        })
        orient_map[key] = orient_id
    # Importar bolseiros
    bol_df = pd.read_csv("bolseiros.csv")
    bol_df['email'] = bol_df['email'].astype(str).str.strip()
    logging.info(f"Loaded bolseiros: {len(bol_df)}")
    for idx, row in bol_df.iterrows():
        first, last = parse_name(row["bolseiro"])
        if row.get("person_type") == "tecnico":
            user_role = ROLE_TECNICO
        elif row.get("is_team_leader") == True:
            user_role = ROLE_TEAM_LEADER
        else:
            user_role = ROLE_BOLSEIRO

        user_id = create_user(row["email"], row["password"], user_role, first, last)
        team_id = get_or_create_team(row.get("equipa"))

        # Normaliza o nome do orientador antes de procurar no dicionário
        ori_name = row.get("orientador")
        ori_key  = normalize(ori_name)
        orient_id = orient_map.get(ori_key)
        if orient_id is None:
            logging.warning(f"[Import] Não encontrei orientador para '{ori_name}' (chave='{ori_key}')")
        person_id = create_item("persons", {
            "user": user_id,
            "person_type": "bolseiro",
            "first_name": first,
            "last_name": last,
            "academic_level": row.get("tipo"),
            "email": row["email"],
            "team": team_id,
            "orientador": orient_id
        })
        # Coorientadores
        for co in ["coorientador1", "coorientador2"]:
            co_name = row.get(co)
            if pd.notna(co_name):
                co_key = normalize(co_name)                               # <- normaliza aqui
                co_orient_id = orient_map.get(co_key)
                if co_orient_id:
                    create_item("persons_coorientadores", {
                        "person_id": person_id,
                        "orientador_id": co_orient_id
                    })
                else:
                    logging.warning(
                        f"[Import] Não encontrei co-orientador para '{co_name}' (chave='{co_key}')"
                    )
        # Principal Bolsa
        if pd.notna(row.get("termino")) and pd.notna(row.get("inicio")):
            try:
                end_date = datetime.strptime(str(row["termino"]), "%Y-%m-%d").date()
                start_date = datetime.strptime(str(row["inicio"]), "%Y-%m-%d").date()
            except ValueError:
                logging.error(f"Invalid date format for row {idx}: inicio={row.get('inicio')}, termino={row.get('termino')}")
                continue
            status = "activa" if end_date >= datetime.today().date() else "concluída"
            bolsa_id = create_item("Bolsas", {
                "person_id": person_id,
                "level": row.get("tipo"),
                "team_id": team_id,
                "start_date": row.get("inicio"),
                "end_date": row.get("termino"),
                "status": status
            })
            # Renovação
            if pd.notna(row.get("termino_renova")) and pd.notna(row.get("inicio_renova")):
                try:
                    end_r = datetime.strptime(str(row["termino_renova"]), "%Y-%m-%d").date()
                    start_r = datetime.strptime(str(row["inicio_renova"]), "%Y-%m-%d").date()
                except ValueError:
                    logging.error(f"Invalid renewal date format for row {idx}: inicio_renova={row.get('inicio_renova')}, termino_renova={row.get('termino_renova')}")
                    continue
                status_r = "activa" if end_r >= datetime.today().date() else "concluída"
                create_item("Bolsas", {
                    "person_id": person_id,
                    "level": row.get("tipo"),
                    "team_id": team_id,
                    "start_date": row.get("inicio_renova"),
                    "end_date": row.get("termino_renova"),
                    "status": status_r,
                    "renewal_of": bolsa_id
                })
        else:
            logging.warning(f"Skipping Bolsa creation for row {idx} due to missing dates: inicio={row.get('inicio')}, termino={row.get('termino')}")

    logging.info("=== Import Process Completed Successfully ===")

if __name__ == "__main__":
    main()
