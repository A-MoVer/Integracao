import requests
import logging
import time

# Configurar logging para saída informativa e de erro
logging.basicConfig(level=logging.INFO, format='%(levelname)s: %(message)s')

# Configurações da API Directus
API_URL = "http://localhost:8055"  # URL base da API do seu projeto Directus (ex: "https://meu.directus.server.org")
API_TOKEN = "SMP_7T_m9JYqepUfCWShKy3lB5HReZzv"           # Token de acesso (Bearer) de um administrador no Directus

# Cabeçalhos de autenticação e tipo de conteúdo JSON
headers = {
    "Authorization": f"Bearer {API_TOKEN}",
    "Content-Type": "application/json"
}


def add_field(f):
    collection = f.pop("collection")        # retira a chave "collection"
    post(f"/fields/{collection}", f)        # endpoint correcto


def post(endpoint, data=None, *, expect_json=True):
    """Envia POST para o Directus e lida com respostas sem corpo."""
    url = f"{API_URL}{endpoint}"
    try:
        response = requests.post(url, json=data, headers=headers)
    except Exception as e:
        logging.error(f"Falha ao conectar no POST {endpoint}: {e}")
        return None

    if 200 <= response.status_code < 300:
        logging.info(f"POST {endpoint} - sucesso")
        # Se não esperamos JSON (ou corpo vazio), não tente parsear
        if not expect_json or response.status_code == 204 or not response.content:
            return None
        return response.json().get("data")
    else:
        logging.error(f"Erro POST {endpoint}: {response.status_code} - {response.text}")
        return None
    
def patch(endpoint, data):
    """Envia uma requisição PATCH para o endpoint da API com os dados JSON fornecidos."""
    url = f"{API_URL}{endpoint}"
    try:
        response = requests.patch(url, json=data, headers=headers)
    except Exception as e:
        logging.error(f"Falha ao conectar no PATCH {endpoint}: {e}")
        return None
    if 200 <= response.status_code < 300:
        logging.info(f"PATCH {endpoint} - sucesso")
        return response.json().get("data")
    else:
        logging.error(f"Erro PATCH {endpoint}: {response.status_code} - {response.text}")
        return None

# 1. Criação das coleções e campos
logging.info("Criando coleções e seus campos...")
# -------------------------------------------
# Coleção "teams" – informações de equipes
# -------------------------------------------
teams_fields = [
    {
        "field": "name",
        "type": "string",
        "meta": {
            "interface": "input",
            "display_name": "Nome da Equipe"
        }
    },
    {
        "field": "area",
        "type": "string",
        "meta": {
            "interface": "input",
            "display_name": "Área/Departamento"
        }
    },
    {
        "field": "photo",
        "type": "uuid",
        "meta": {
            "interface": "file",
            "display_name": "Foto da Equipe",
            # se quiser que o Directus reconheça automaticamente:
            # "special": ["file"],
            # "related_collection": "directus_files",
        }
    },
    {
        "field": "email",
        "type": "string",
        "meta": {
            "interface": "input",
            "display_name": "Email de Contato"
        }
    },
    # ---------- líder ----------
    {
        "field": "leader",
        "type": "uuid",
        "meta": {
            "interface": "select-dropdown-m2o",
            "display_name": "Líder de Equipa",
            "special": ["m2o"],                # marca como relação M-to-1
            "related_collection": "directus_users",
            "options": {
                "template": "{{first_name}} {{last_name}}"
            }
        },
        "schema": {
            "foreign_key_table": "directus_users",
            "foreign_key_column": "id",
            "on_delete": "SET NULL",
            "constraint_name": "teams_leader"
        }
    }
]

teams_collection = {
    "collection": "teams",
    "fields": teams_fields,
    "meta": {
        "icon": "people",
        "note": "Grupos de pesquisa/equipes",
        "display_template": "{{name}}"
    },
    "schema": {}
}

post("/collections", teams_collection)


# ─── Campos extra em directus_users ───────────────────────────
logging.info("Criando campos extra em directus_users (team, phone)...")
extra_user_fields = [
    {
        "collection": "directus_users",
        "field": "team",
        "type": "uuid",
        "meta": {
            "interface": "select-dropdown",
            "display_name": "Equipa",
            "options": {"template": "{{name}}"}
        }
    },
    {
        "collection": "directus_users",
        "field": "phone",
        "type": "string",
        "meta": {
            "interface": "input",
            "display_name": "Telefone"
        }
    }
]
for f in extra_user_fields:
    add_field(f)

# Coleção "team_members" – relaciona usuários às suas respectivas equipes (membros de equipe)
team_members_fields = [
    {"field": "team", "type": "uuid", "meta": {
        "interface": "select-dropdown",
        "display_name": "Equipe",
        "options": {"template": "{{name}}"}
    }},
    {"field": "user", "type": "uuid", "meta": {
        "interface": "select-dropdown",
        "display_name": "Usuário",
        "options": {"template": "{{first_name}} {{last_name}}"}
    }},
    {"field": "date_from", "type": "date", "meta": {"interface": "date", "display_name": "Data de Entrada"}},
    {"field": "date_to", "type": "date", "meta": {"interface": "date", "display_name": "Data de Saída"}}
]

team_members_collection = {
    "collection": "team_members",
    "fields": team_members_fields,
    "meta": {"note": "Ligação entre usuários e equipes"},
    "schema": {}
}
post("/collections", team_members_collection)

# Coleção "scholarships" – registro de bolsas (associadas a um bolseiro)
scholarships_fields = [
    {"field": "title", "type": "string", "meta": {"interface": "input", "display_name": "Título"}},
    {"field": "holder", "type": "uuid", "meta": {
        "interface": "select-dropdown",
        "display_name": "Bolseiro/Técnico",
        "options": {"template": "{{first_name}} {{last_name}}"}
    }},
    {"field": "orientador", "type": "uuid", "meta": {
        "interface": "select-dropdown",
        "display_name": "Orientador Responsável",
        "options": {"template": "{{first_name}} {{last_name}}"}
    }},
    {"field": "level", "type": "string", "meta": {
    "interface": "select-dropdown",
    "display_name": "Nível",
    "options": {"choices": [
        {"text": "Pós-Doutoramento", "value": "postdoc"},
        {"text": "Doutoramento",      "value": "phd"},
        {"text": "Mestrado",          "value": "master"},
        {"text": "Licenciatura",      "value": "bachelor"}
    ]}
    }},
    {"field": "status", "type": "string", "meta": {
        "interface": "select-dropdown",
        "display_name": "Status",
        "options": {
            "choices": [
                {"text": "Pendente",  "value": "pending"},
                {"text": "Aprovado",  "value": "approved"},
                {"text": "Rejeitado", "value": "rejected"}
            ]
        }
    }},
    {"field": "approved_by", "type": "uuid", "meta": {
        "interface": "select-dropdown",
        "display_name": "Aprovado Por",
        "options": {"template": "{{first_name}} {{last_name}}"}
    }},
    {"field": "links", "type": "json", "meta": {
        "interface": "list",
        "display_name": "Links Relacionados"
    }},
    # Campo virtual para anexos (muitos-para-muitos com directus_files)
    {"field": "attachments", "type": "alias", "meta": {
        "interface": "files",          # interface “Files” múltiplos
        "display_name": "Anexos"
    }},
    {
        "field": "state",
        "type": "string",
        "meta": {
            "interface": "select-dropdown",
            "display_name": "Estado",
            "options": {
                "choices": [
                    {"text": "Activo",     "value": "active"},
                    {"text": "Concluído",  "value": "finished"}
                ]
            }
        }
    },
    {"field": "date_start",  "type": "date",
     "meta": {"interface": "date", "display_name": "Início"}},
    {"field": "date_end",    "type": "date",
     "meta": {"interface": "date", "display_name": "Término"}},
    {"field": "renew_start", "type": "date",
     "meta": {"interface": "date", "display_name": "Início Renova"}},
    {"field": "renew_end",   "type": "date",
     "meta": {"interface": "date", "display_name": "Término Renova"}},

]

scholarships_collection = {
    "collection": "scholarships",
    "fields": scholarships_fields,
    "meta": {
        "icon": "graduation-cap",
        "note": "Registos de bolsas de estudo",
        "display_template": "{{title}}"
    }
}
post("/collections", scholarships_collection)

# Coleção "publications" – publicações científicas produzidas
pub_fields = [
    {"field": "title", "type": "string", "meta": {
        "interface": "input",
        "display_name": "Título"
    }},
    {"field": "content", "type": "text", "meta": {
        "interface": "textarea",
        "display_name": "Conteúdo"
    }},
    {"field": "status", "type": "string", "meta": {
        "interface": "select-dropdown",
        "display_name": "Status",
        "options": {
            "choices": [
                {"text": "Pendente",  "value": "pending"},
                {"text": "Aprovado",  "value": "approved"},
                {"text": "Rejeitado", "value": "rejected"}
            ]
        }
    }},
    {"field": "approved_by", "type": "uuid", "meta": {
        "interface": "select-dropdown",
        "display_name": "Aprovado Por",
        "options": {"template": "{{first_name}} {{last_name}}"}
    }},
    {"field": "links", "type": "json", "meta": {
        "interface": "list",
        "display_name": "Links Relacionados"
    }},
    # Campo virtual para anexos (muitos-para-muitos com directus_files)
    {"field": "attachments", "type": "alias", "meta": {
        "interface": "files",          # interface “Files” múltiplos
        "display_name": "Anexos"
    }},
    {
        "field": "state",
        "type": "string",
        "meta": {
            "interface": "select-dropdown",
            "display_name": "Estado",
            "options": {
                "choices": [
                    {"text": "Rascunho",  "value": "draft"},
                    {"text": "Ativa",     "value": "active"},
                    {"text": "Arquivada", "value": "archived"}
                ]
            }
        }
    },
    {
        "field": "team_at_creation",
        "type": "uuid",
        "meta": {
            "interface": "select-dropdown",
            "display_name": "Equipa (snapshot)",
            "options": {"template": "{{name}}"},
            "readonly": True
        }
    }
    # ──────────────────────────────────────────────

]

publications_collection = {
    "collection": "publications",
    "fields": pub_fields,
    "meta": {
        "icon": "book",
        "note": "Publicações científicas",
        "display_template": "{{title}}"
    },
    "schema": {}
}

post("/collections", publications_collection)



# Coleção opcional "resources" – armazenamento de recursos (arquivos, materiais)
res_fields = [
    {"field": "title", "type": "string", "meta": {"interface": "input", "display_name": "Título do Recurso"}},
    {"field": "file", "type": "uuid", "meta": {"interface": "file", "display_name": "Arquivo"}}
]
resources_collection = {
    "collection": "resources",
    "fields": res_fields,
    "meta": {"icon": "archive", "note": "Recursos e materiais diversos"},
    "schema": {}
}
post("/collections", resources_collection)

user_coor_fields = [
    {"field": "supervised_user", "type": "uuid", "meta": {
        "interface": "select-dropdown",
        "display_name": "Bolseiro/Técnico",
        "options": {"template": "{{first_name}} {{last_name}}"}
    }},
    {"field": "coorientador", "type": "uuid", "meta": {
        "interface": "select-dropdown",
        "display_name": "Coorientador",
        "options": {"template": "{{first_name}} {{last_name}}"}
    }}
]
post("/collections", {
    "collection": "user_coorientadores",
    "fields": user_coor_fields,
    "meta": {"note": "Coorientadores dos utilizadores"}
})


# 2. Criação das roles de usuário e definição das permissões (policies)
logging.info("Criando roles e definindo permissões...")

def create_role(name, description="", icon=""):
    """Cria uma role no Directus. Se já existir, retorna a existente."""
    data = {"name": name, "description": description}
    if icon:
        data["icon"] = icon
    role = post("/roles", data)
    if not role:
        # Se não foi criada, tentar recuperar role existente com mesmo nome
        res = requests.get(f"{API_URL}/roles?filter[name][_eq]={name}", headers=headers)
        existing = res.json().get("data", []) if res.status_code == 200 else []
        if existing:
            role = existing[0]
            logging.info(f"Role '{name}' já existe (id={role.get('id')})")
    return role.get("id") if role else None

# Criar os perfis de usuário conforme especificado
admin_role_id   = create_role("Admin",       "Administrador do sistema",         icon="supervisor_account")
pres_role_id    = create_role("Presidente",  "Presidente / Diretor do projeto",  icon="shield")
orient_role_id  = create_role("Orientador",  "Orientador de Bolseiros",          icon="school")
bols_role_id    = create_role("Bolseiro",    "Pesquisador Bolseiro",             icon="person")
tec_role_id     = create_role("Técnico",     "Funcionário Técnico de apoio",     icon="engineering")
leader_role_id  = create_role("Líder de Equipa", "Responsável da equipa", icon="groups")
coorient_role_id = create_role("Coorientador",  "Coorientador de projectos", icon="person_add")
auth_role_id    = create_role("Authenticated", "Usuário autenticado padrão",     icon="person_outline")
public_role_id  = create_role("Public",       "Acesso público (não autenticado)", icon="public")

# Definir que roles autenticadas herdam permissões da role "Authenticated"
for role_id in (pres_role_id, orient_role_id, bols_role_id, tec_role_id, leader_role_id, coorient_role_id):
    if role_id and auth_role_id:
        patch(f"/roles/{role_id}", {"parent_role": auth_role_id})

logging.info("Definindo políticas de permissões para cada role...")

# ---------- helper para criar policies ----------

def add_policy(role_id, collection, action, *,
               fields=None, condition=None, name=None, effect="allow"):
    """
    Cria uma policy Directus 11 garantindo campos obrigatórios.
    """
    if name is None:
        name = f"{action.capitalize()} {collection} – {role_id[:4]}"
    data = {
        "name": name,
        "effect": effect,
        "role": role_id,
        "collection": collection,
        "action": action
    }
    if fields is not None:
        data["fields"] = fields
    if condition is not None:
        data["condition"] = condition
    return post("/policies", data)
# -----------------------------------------------

# ───── Role PUBLIC ───────────────────────────────────────────
if public_role_id:
    add_policy(
        public_role_id, "teams", "read",
        fields=["id", "name", "area"],
        name="Public lê teams"
    )

# ───── Role AUTHENTICATED ───────────────────────────────────
if auth_role_id:
    add_policy(
        auth_role_id, "teams", "read",
        fields=["id", "name", "area", "photo", "email"],
        name="Auth lê teams"
    )
    add_policy(
        auth_role_id, "publications", "read",
        fields=["id", "title", "content", "status", "approved_by"],
        condition={"status": {"_eq": "approved"}},
        name="Auth lê publicações aprovadas"
    )
    add_policy(
        auth_role_id, "resources", "read",
        name="Auth lê resources"
    )

# ───── Role BOLSEIRO ────────────────────────────────────────
if bols_role_id:
    add_policy(bols_role_id, "publications", "create",
               fields=["title", "content"],
               name="Bolseiro cria publicação")

    add_policy(
        bols_role_id, "publications", "read",
        fields=["id", "title", "content", "status", "approved_by"],
        condition={
            "_or": [
                {"status": {"_eq": "approved"}},
                {"user_created": {"_eq": "$CURRENT_USER"}}
            ]
        },
        name="Bolseiro lê próprias + aprovadas"
    )

    add_policy(
        bols_role_id, "publications", "update",
        fields=["title", "content"],
        condition={"user_created": {"_eq": "$CURRENT_USER"}},
        name="Bolseiro edita próprias"
    )

    add_policy(
        bols_role_id, "publications", "delete",
        condition={"user_created": {"_eq": "$CURRENT_USER"}},
        name="Bolseiro apaga próprias"
    )

    add_policy(
        bols_role_id, "scholarships", "read",
        condition={"holder": {"_eq": "$CURRENT_USER"}},
        name="Bolseiro lê sua bolsa"
    )

# ───── Role TÉCNICO (mesmo padrão do Bolseiro) ──────────────
if tec_role_id:
    add_policy(tec_role_id, "publications", "create",
               fields=["title", "content"],
               name="Técnico cria publicação")

    add_policy(
        tec_role_id, "publications", "read",
        fields=["id", "title", "content", "status", "approved_by"],
        condition={
            "_or": [
                {"status": {"_eq": "approved"}},
                {"user_created": {"_eq": "$CURRENT_USER"}}
            ]
        },
        name="Técnico lê próprias + aprovadas"
    )

    add_policy(
        tec_role_id, "publications", "update",
        fields=["title", "content"],
        condition={"user_created": {"_eq": "$CURRENT_USER"}},
        name="Técnico edita próprias"
    )

    add_policy(
        tec_role_id, "publications", "delete",
        condition={"user_created": {"_eq": "$CURRENT_USER"}},
        name="Técnico apaga próprias"
    )

# ───── Role ORIENTADOR ──────────────────────────────────────
if orient_role_id:
    # Pode criar publicações
    add_policy(orient_role_id, "publications", "create",
               fields=["title", "content"],
               name="Orientador cria publicação")

    # Pode ler todas as publicações (para rever), mas não muda estado
    add_policy(orient_role_id, "publications", "read",
               name="Orientador lê publicações")

    # Pode editar apenas título e conteúdo das publicações que ele próprio criou
    add_policy(orient_role_id, "publications", "update",
               fields=["title", "content"],
               condition={"user_created": {"_eq": "$CURRENT_USER"}},
               name="Orientador edita próprias publicações")

    # Ler bolsas que orienta
    add_policy(
        orient_role_id, "scholarships", "read",
        condition={"orientador": {"_eq": "$CURRENT_USER"}},
        name="Orientador lê bolsas próprias"
    )

# ───── Role PRESIDENTE ──────────────────────────────────────
if pres_role_id:
    # Equipes (full CRUD)
    for act in ("create", "read", "update", "delete"):
        add_policy(pres_role_id, "teams", act,
                   name=f"Presidente {act} teams")

    # Membros (full CRUD)
    for act in ("create", "read", "delete"):
        add_policy(pres_role_id, "team_members", act,
                   name=f"Presidente {act} membros")

    # Publicações (gestão total)
    for act in ("read", "update", "delete"):
        add_policy(pres_role_id, "publications", act,
                   name=f"Presidente {act} publications")

    # Bolsas (full CRUD)
    for act in ("create", "read", "update", "delete"):
        add_policy(pres_role_id, "scholarships", act,
                   name=f"Presidente {act} scholarships")

    # Resources (full CRUD)
    for act in ("create", "read", "update", "delete"):
        add_policy(pres_role_id, "resources", act,
                   name=f"Presidente {act} resources")


if leader_role_id:
    # Lider pode ler a própria equipa
    add_policy(leader_role_id, "teams", "read",
        condition={"leader": {"_eq": "$CURRENT_USER"}},
        name="Leader lê sua team")

    # Atualizar dados da equipa (agora sem campo orientador)
    add_policy(leader_role_id, "teams", "update",
        fields=["name", "area", "photo", "email"],
        condition={"leader": {"_eq": "$CURRENT_USER"}},
        name="Leader edita sua team")

    # Gerir membros da equipa
    for act in ("create", "read", "delete"):
        add_policy(leader_role_id, "team_members", act,
            fields=["team", "user"] if act=="create" else None,
            condition={"team": {"leader": {"_eq": "$CURRENT_USER"}}},
            name=f"Leader {act} membros")
        
    add_policy(
        leader_role_id, "publications", "update",
        fields=["status", "approved_by"],
        condition={
            "team_at_creation": {
                "_in": {"$SELECT": {
                    "collection": "teams",
                    "fields": "id",
                    "filter": {"leader": {"_eq": "$CURRENT_USER"}}
                }}
            }
        },
        name="Leader aprova publicação da sua equipa"
    )

if coorient_role_id:
    # Ver bolsas/publicações dos supervisionados
    add_policy(coorient_role_id, "scholarships", "read",
        condition={"holder": {
            "_in": {"$SELECT": {
                "collection": "user_coorientadores",
                "fields": "supervised_user",
                "filter": {"coorientador": {"_eq": "$CURRENT_USER"}}
            }}
        }},
        name="Coorient lê bolsas supervisionadas")

    add_policy(coorient_role_id, "publications", "read",
        condition={"user_created": {
            "_in": {"$SELECT": {
                "collection": "user_coorientadores",
                "fields": "supervised_user",
                "filter": {"coorientador": {"_eq": "$CURRENT_USER"}}
            }}
        }},
        name="Coorient lê pubs supervisionadas")

# Nota: Não criamos políticas específicas para o role Admin, pois considera-se 
# que o role Admin possui acesso irrestrito (admin_access) por definição no Directus.
# Se necessário, poderíamos atribuir uma policy ampla permitindo todas as ações 
# em todas as coleções para o Admin.

# 3. Configuração dos Flows de automação
logging.info("Criando flows de automação (gatilhos e ações)...")

# Flow 1: "Após criar publicação" – define status como pending se autor for Bolseiro/Técnico
flow1 = post("/flows", {
    "name": "Publicação nova pendente",
    "status": "active",
    "trigger": "event",
    "accountability": "$full",  # executa com permissões completas para poder atualizar item
    "options": {
        "event": "items.create",
        "collection": "publications"
    }
})
flow1_id = flow1.get("id") if flow1 else None
if flow1_id:
    # Operação: Atualizar o item de publicação recém-criado para status "pending"
    op_update = post("/operations", {
        "flow": flow1_id,
        "name": "Mark Pending",
        "key": "mark_pending",
        "type": "update",
        "position_x": 0, "position_y": 0,
        "options": {
            "collection": "publications",
            "ids": ["{{ $trigger.payload.id }}"],  # ID do item criado
            "payload": {
                "status": "pending",
                "state": "draft",
                "team_at_creation": "{{ $accountability.user.team }}"  # se guardas equipa no user
            },
            "emitEvents": False  # não emitir eventos para não disparar outros flows recursivamente
        }
    })
    # Associa a operação ao fluxo (como primeira operação após o gatilho)
    if op_update:
        patch(f"/flows/{flow1_id}", {"operation": op_update.get("id")})

# Flow 2: "Após atualizar publicação" – retorna status para pending se editada por Bolseiro/Técnico
flow2 = post("/flows", {
    "name": "Publicação editada pendente",
    "status": "active",
    "trigger": "event",
    "accountability": "$full",
    "options": {
        "event": "items.update",
        "collection": "publications"
    }
})
flow2_id = flow2.get("id") if flow2 else None
if flow2_id:
    # Operação 1: Condição verificando se o usuário que editou (user_updated) pertence às roles Bolseiro/Técnico
    condition = post("/operations", {
        "flow": flow2_id,
        "name": "Check Editor Role",
        "key": "check_role",
        "type": "condition",
        "position_x": 0, "position_y": 0,
        "options": {
            "condition": {
                "user_updated": {
                    "role": {
                        "id": {"_in": [bols_role_id, tec_role_id]}
                    }
                }
            }
        }
    })
    # Operação 2: Atualizar status para pending caso a condição acima seja verdadeira
    update2 = None
    if condition:
        update2 = post("/operations", {
            "flow": flow2_id,
            "name": "Revert to Pending",
            "key": "revert_pending",
            "type": "update",
            "position_x": 200, "position_y": 0,
            "options": {
                "collection": "publications",
                "ids": ["{{ $trigger.payload.id }}"],
                "payload": {"status": "pending"},
                "emitEvents": False
            }
        })
    # Conectar as operações: em caso de sucesso na condição, executa a atualização
    if condition and update2:
        patch(f"/operations/{condition.get('id')}", {"resolve": update2.get("id")})
        patch(f"/flows/{flow2_id}", {"operation": condition.get("id")})

# Flow 3: Botão manual "Aprovar" – altera status para approved e registra quem aprovou
flow3 = post("/flows", {
    "name": "Aprovar Publicação",
    "status": "active",
    "trigger": "manual",
    "accountability": "$trigger",
    "options": {
        "collections": ["publications"],
        "scope": "item"
    }
})

flow3_id = flow3.get("id") if flow3 else None
if flow3_id:
    # 0. Condition: é líder da equipa?
    cond = post("/operations", {
        "flow": flow3_id,
        "name": "Is Team Leader?",
        "key": "check_is_leader",
        "type": "condition",
        "position_x": -200, "position_y": 0,
        "options": {
            "condition": {
                "team_at_creation": {
                    "leader": { "_eq": "{{ $accountability.user.id }}" }
                }
            }
        }
    })

    # 1. Update → approved
    op1 = post("/operations", {
        "flow": flow3_id,
        "name": "Set Approved",
        "key": "set_approved",
        "type": "update",
        "position_x": 0, "position_y": 0,
        "options": {
            "collection": "publications",
            "ids": ["{{ $trigger.content.id }}"],
            "payload": {
                "status": "approved",
                "approved_by": "{{ $accountability.user.id }}"
            },
            "emitEvents": False
        }
    })

    # 2. Notificação
    op2 = post("/operations", {
        "flow": flow3_id,
        "name": "Notify Author",
        "key": "notify_author",
        "type": "notification",
        "position_x": 200, "position_y": 0,
        "options": {
            "users": ["{{ $trigger.payload.user_created }}"],
            "title": "Publicação Aprovada",
            "message": "Sua publicação foi aprovada."
        }
    })

    if cond and op1:
        patch(f"/operations/{cond.get('id')}", {"resolve": op1.get("id")})
    if op1 and op2:
        patch(f"/operations/{op1.get('id')}", {"resolve": op2.get("id")})

    patch(f"/flows/{flow3_id}", {"operation": cond.get("id")})

logging.info("Configuração concluída.")
