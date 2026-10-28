## Qué cambia

<!-- Una o dos frases. Enlaza el ticket de Jira: SCRUM-XX -->

## Cómo probarlo

<!-- Pasos concretos para que quien revise lo compruebe. Ejemplo:
1. `docker compose up -d --build` (y `docker compose --profile tools run --rm provision` si hace falta datos)
2. Llamar a `POST /api/v1/...` con ... (Scalar: http://localhost:5087/scalar/v1)
3. Esperado: ...
-->

## Checklist

- [ ] El CI pasa (`CI passed`)
- [ ] Si cambia el modelo de datos: incluye la migración
- [ ] Si cambia un endpoint: el contrato con el frontend sigue cuadrando (rutas y forma del JSON)
- [ ] Sin secretos ni datos reales en el código, `appsettings*.json` o el `.env.example`
- [ ] README o `.env.example` actualizados si hay variables o pasos nuevos
