# Entorno local con Docker

Requiere Docker y un `.env` (copiar `.env.example`).

```bash
docker compose up -d --build                      # postgres + migraciones + API (http://localhost:5087)
docker compose --profile tools run --rm provision  # catálogo de permisos + 2 cuentas de prueba (las claves se muestran una vez)
docker compose down -v                             # apagar y borrar la base
```

- Las migraciones las aplica el servicio `migrate` (one-shot); la API no migra al arrancar.
- Scalar: `http://localhost:5087/scalar/v1`. Health: `/health`.
- PostgreSQL queda en `127.0.0.1:5433`.
- `provision` usa la red de postgres porque `DevProvisioning` solo acepta `localhost` y la base `aula_virtual_dev`.
- Solo para desarrollo: el `.env` no se versiona y la clave JWT de ejemplo no debe usarse fuera de local.
