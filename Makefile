# ---------- Docker Dev Helpers ----------

SERVICE=educheck-api

dev:
	docker compose down -v
	docker compose up --build

dev-bg:
	docker compose down -v
	docker compose up -d --build

stop:
	docker compose down

logs:
	docker compose logs -f $(SERVICE)

shell:
	docker compose exec $(SERVICE) bash

env:
	docker compose exec $(SERVICE) printenv | sort
