from diagrams import Diagram, Cluster
from diagrams.onprem.client import User
from diagrams.programming.framework import React, FastAPI
from diagrams.onprem.database import PostgreSQL
from diagrams.onprem.container import Docker

with Diagram("Arquitetura da Aplicação WAVY", show=True):
    user = User("Utilizador")

    with Cluster("Docker Container"):
        with Cluster("Frontend"):
            frontend = React("Frontend React")

        with Cluster("Backend"):
            api      = FastAPI("Directus API")
            database = PostgreSQL("Base de Dados SQLite3")

    # Fluxo de chamadas
    user >> frontend >> api >> database
