# Estado del backend del aula virtual

Informe de SCRUM-24 · 27 de octubre de 2026 · Revisión del código de este repositorio frente al prototipo B
(frontend `aula_mf_frontend`).

## En resumen

- **La base está bien hecha.** El código es ordenado (un módulo por área), tiene pruebas automáticas y un CI
  que revisa cada cambio antes de integrarlo.
- **Está hecho:** el ingreso con usuario y contraseña, los roles y permisos del administrador, y la gestión de
  cursos, ediciones, horarios con enlace de clase y profesores.
- **Falta la mayor parte de lo que ve el alumno y la familia.** Matrícula, evaluaciones, asistencia y
  certificados existen solo como carpetas vacías. No hay lecciones, práctica, tutor IA, avisos, panel de la
  familia ni recuperación de contraseña.
- **El frontend todavía no puede conectarse.** Funciona con datos de ejemplo. Incluso el ingreso, que sí
  existe, responde con un formato distinto al que espera el frontend.
- **Hay que decidir pronto** cómo se modela la familia (rol y vínculo padre–hijo). De eso dependen el
  ingreso, el panel de la familia y el consentimiento.

## Versiones confirmadas

| Componente | Versión |
|---|---|
| .NET | 10 (SDK 10.0.400, fijado en `global.json`) |
| Base de datos | PostgreSQL 17 |
| Acceso a datos | Entity Framework Core 10.0.12 con Npgsql |
| Archivos | Cloudflare R2 (compatible con S3), opcional |

## Qué está hecho

Todas las rutas empiezan por `/api/v1`.

| Módulo | Qué hace | Pruebas |
|---|---|---|
| Auth | Ingreso de alumno (usuario y contraseña) y de administrador (correo); renovación de sesión con cookie segura; cerrar sesión en uno o todos los dispositivos; bloqueo tras 5 intentos fallidos; contraseñas con Argon2id | 36 |
| Authorization | Roles `administrator`, `student` e `instructor`; permisos por rol (`GET authorization/me`) | 9 |
| Courses | Categorías, cursos (con portada), ediciones con sesiones y enlace de clase virtual, profesores, precios y promociones; listado público de cursos | 42 |
| Users | Datos de la persona (nombres, documento, correo). Sin endpoints | 3 |
| Infraestructura | Docker Compose (PostgreSQL + migraciones + API), CI completo, imagen publicada en GitHub Packages | — |

## Qué falta frente al prototipo B

| Pantalla del prototipo B | ¿El backend la soporta? | Ticket |
|---|---|---|
| Ingreso del alumno | Parcial: existe, pero con otro formato de respuesta y sin datos del usuario | SCRUM-36 |
| Ingreso de la familia | No | SCRUM-34, SCRUM-35 |
| Inicio, ruta de aprendizaje, logros | No | — |
| Lecciones y práctica con corrección | No | SCRUM-60 |
| Tutor IA | No | SCRUM-80 |
| Evaluaciones | No (módulo vacío) | — |
| Clases en vivo, asistencia, materiales | Parcial: hay horarios con enlace, pero no matrícula ni asistencia | SCRUM-51, SCRUM-54 |
| Entregas de tareas (subir archivos) | Parcial: el almacenamiento existe, sin endpoint | SCRUM-58 |
| Panel de la familia (hijos, mensajes, membresía) | No | — |
| Avisos | No | SCRUM-72 |
| Certificados | No (módulo vacío) | SCRUM-81 |
| Matrícula y activación | No (módulo vacío) | SCRUM-44, SCRUM-45, SCRUM-46 |
| Recuperar contraseña por correo | No (no hay envío de correos) | SCRUM-37 |
| Crear alumnos y usuarios desde la administración | No: solo con las herramientas de consola de `tools/` | — |

### Diferencias de contrato con el frontend

El frontend define lo que espera en `src/services/http/index.ts` y `src/types/domain.ts`.

| Punto | Frontend espera | Backend hace hoy |
|---|---|---|
| Respuesta del ingreso | `{ token, user, expiresIn }` | `{ accessToken, tokenType, expiresIn }`, sin `user` |
| `GET /auth/me` | Usuario con rol, nombre, zona horaria e hijos | Id de cuenta, tipos de ingreso y estado; sin rol ni nombre |
| Ingreso de familia | `POST /auth/family/login` | No existe; solo `auth/admin/login` |
| Cerrar sesión | Con el token en la cabecera | Con la cookie de renovación, que el servidor de Next no reenvía |
| Rol en el token | — | El JWT no lleva el rol; se consulta aparte en `authorization/me` |

Hay que acordar un solo contrato antes de construir más pantallas. Lo más simple es que el backend devuelva
`user` en el ingreso y amplíe `auth/me`.

## Riesgos

1. **Los repositorios son públicos** y siguen en la cuenta personal `JoseAlexxander123`, no en la organización
   de Mate Fácil (SCRUM-21). Además, `tools/AulaVirtual.StagingProvisioning/README.md` publica el servidor, la
   base de datos y el usuario de staging, sin la contraseña. Conviene hacer los repos privados o sacar esos
   datos.
2. **El modo `hybrid` del frontend no es seguro para producción.** Está sin commitear. El rol (alumno o
   familia) lo decide el formulario y viaja en la cookie sin que el backend lo valide, así que alguien podría
   cambiárselo. Sirve para demos; desaparece cuando el backend devuelva el rol (SCRUM-34).
3. **No hay despliegue.** El CD construye la imagen pero no la publica en ningún servidor. Staging (SCRUM-23)
   está por hacer.
4. **Las cuentas de prueba no tienen rol.** `DevProvisioning` crea 2 cuentas sin asignarles `student`.

## Deuda técnica

| # | Qué | Impacto |
|---|---|---|
| 1 | 4 módulos vacíos (Exams, Enrollments, Attendance, Certificates) | Es trabajo pendiente, no un error |
| 2 | No hay API de usuarios ni de cuentas | No se pueden dar de alta alumnos desde el aula |
| 3 | El código no cumple `.editorconfig` y el control de estilo del CI no bloquea | Bajo; arreglarlo antes de que crezca el equipo |
| 4 | GitHub Actions fijadas por versión mayor, no por SHA | Bajo (cadena de suministro) |
| 5 | Solo una prueba de integración real contra PostgreSQL | Medio: el resto son unitarias |
| 6 | 4 copias de `SnakeCaseNpgsqlHistoryRepository` | Bajo: duplicación |
| 7 | Los permisos se consultan en la base en cada petición porque el JWT no lleva roles | Bajo hoy; revisar con carga |
| 8 | `StagingProvisioning` guarda credenciales con DPAPI | Solo funciona en Windows |

## Próximos pasos sugeridos

1. Acordar el contrato de ingreso y el modelo de familia (rol + vínculo padre–hijo): SCRUM-34 y SCRUM-35.
2. Exponer el alta de alumnos y familias para que el administrador pueda matricular: SCRUM-44.
3. Montar staging con despliegue automático desde `main`: SCRUM-23.
4. Construir por orden de pantallas del prototipo B: inicio, lecciones y práctica, clases y evaluaciones.
