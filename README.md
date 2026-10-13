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
