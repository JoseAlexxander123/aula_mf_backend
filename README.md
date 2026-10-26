# AulaVirtual Backend

API modular de AulaVirtual en .NET 10. La solución contiene los módulos de autenticación, autorización, usuarios, cursos y ediciones, almacenamiento y las demás bases del dominio.

## Requisitos

- .NET SDK 10.0.400 (`global.json`)
- PostgreSQL para la persistencia local y las pruebas de integración que lo requieren
- Configuración de R2 únicamente en los entornos que utilicen almacenamiento de archivos

## Desarrollo local

Desde la raíz de este repositorio:

```bash
dotnet restore AulaVirtual.sln
dotnet build AulaVirtual.sln
dotnet test AulaVirtual.sln
dotnet run --project src/AulaVirtual.Api/AulaVirtual.Api.csproj
```

Configura conexiones, JWT y Storage mediante variables de entorno, User Secrets o un mecanismo seguro del entorno. No coloques credenciales en archivos versionados. Los `appsettings*.json` incluidos son configuración de base, no plantillas para guardar secretos reales.

## Estructura

- `src/AulaVirtual.Api/`: host HTTP.
- `src/Modules/`: módulos de dominio, aplicación, infraestructura y API.
- `tests/`: pruebas por módulo.
- `tools/`: utilidades de provisioning explícito.
- `Directory.Packages.props`: versiones centralizadas de paquetes.

Las migraciones EF existentes están dentro de cada módulo de infraestructura. No se aplican automáticamente al preparar este repositorio; cualquier upgrade, rollback o provisioning debe validarse por entorno.

## CI/CD

Los flujos viven en `.github/workflows/`.

### CI (`ci.yml`)

Se ejecuta en cada pull request hacia `main`, y también de forma manual o desde `cd.yml`.

| Job | Objetivo | Bloquea |
|---|---|---|
| `build-and-test` | Restore, build Release y pruebas de toda la solución; publica los `.trx`. | Sí |
| `dependency-audit` | Falla ante paquetes NuGet vulnerables (directos y transitivos); informa los obsoletos. | Sí |
| `database` | PostgreSQL efímero: detecta cambios de modelo sin migración, aplica las migraciones sobre una base vacía, provisiona el catálogo de autorización y ejecuta la prueba de concurrencia contra Postgres. | Sí |
| `container` | Construye la imagen, levanta el stack con Docker Compose y valida `/health` y un endpoint público respaldado por la base. | Sí |
| `code-style` | `dotnet format --verify-no-changes`. Informativo hasta que el código base cumpla `.editorconfig`. | No |
| `ci-passed` | Agrega los resultados anteriores. | Es el check a exigir |

Configuración recomendada en GitHub (Settings, Branches, regla para `main`): exigir pull request, exigir el check `CI passed` y que la rama esté actualizada antes de integrar.

### CD (`cd.yml`)

En cada push a `main` o etiqueta `vX.Y.Z` ejecuta el CI completo y, solo si pasa, publica la imagen en `ghcr.io/<propietario>/<repositorio>` con etiquetas por SHA, rama y versión semántica, junto con SBOM y atestación de procedencia. El despliegue a un entorno (staging o producción) debe añadirse como un job posterior que consuma esa imagen y use un `environment` de GitHub con aprobación y secretos propios.

### Mantenimiento

`dependabot.yml` propone semanalmente actualizaciones de NuGet, GitHub Actions y la imagen base de Docker. Las acciones están fijadas por versión mayor; para mayor rigor de cadena de suministro conviene fijarlas por SHA de commit.
