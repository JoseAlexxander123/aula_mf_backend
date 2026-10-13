# Provisioning de Staging

Esta herramienta manual crea una sola vez las dos cuentas sintéticas de prueba en Staging. La API no la referencia ni la ejecuta al iniciar. No aplica migraciones ni asigna roles.

Exige `DOTNET_ENVIRONMENT=Staging`, el único argumento `--confirm-staging-provisioning` y `ConnectionStrings__DefaultConnection` suministrada fuera del repositorio. La conexión debe utilizar `SSL Mode=Require` o superior y apuntar exactamente a `pg8001.site4now.net:6432`, base `db_acb18a_vclass`, usuario `acb18a_vclass`. La contraseña puede provenir del passfile local de PostgreSQL.

Antes de escribir, verifica la identidad efectiva de la base, los historiales exactos de Users y Auth, la ausencia de datos de cuentas y sesiones, y que no existan los identificadores propuestos. Cualquier dato previo o archivo cifrado existente impide la ejecución. Utiliza los handlers, el generador de contraseñas y el hasher Argon2id existentes. No corrige ni elimina un resultado parcial.

Las credenciales generadas se guardan en `%APPDATA%\AulaVirtual\staging-provisioning\test-accounts.dpapi`, cifradas con Windows DPAPI para el usuario actual. Nunca se imprimen. Conserva este archivo de forma segura e inspecciona Staging manualmente si la ejecución se detiene después de iniciar una escritura.
