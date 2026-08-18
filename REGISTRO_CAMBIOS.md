# REGISTRO DE CAMBIOS - Proyecto 3 Plataforma Web de Dispositivos

## Estado
- Proyecto: activo en fase de preparacion y orquestacion.
- Responsable de actualizacion: Agent-Orchestrator.

## Historial
### 2026-08-14
- Se consolido el set documental base del proyecto.
- Se creo backlog ejecutable por agentes con items atomicos WEB-001 a WEB-018.
- Se agrego documento macro para presentacion de cliente.
- Se agrego one-pager comercial.
- Se inicia trazabilidad formal en este archivo.

### 2026-08-14 (actualizacion operativa)
- Se activo la orquestacion maestra de agentes para los 3 proyectos.
- Se creo documentacion de uso e integracion del proyecto.
- Se dejo este registro como bitacora oficial para seguimiento de cambios por item.
- Archivos impactados en esta actualizacion: REGISTRO_CAMBIOS.md, USO_E_INTEGRACION.md.
- Validacion ejecutada: verificacion de existencia de archivos en carpeta del proyecto.
- Riesgos detectados: ninguno en etapa documental.
- Estado final: COMPLETADO.

### 2026-08-14 (ejecucion item WEB-001)
- Fecha: 2026-08-14
- Item backlog: WEB-001
- Tipo de cambio: tecnico
- Resumen: se scaffold la solucion web con `Blazor`, `API`, capas `Application/Domain/Infrastructure` y proyecto de `Tests`.
- Archivos impactados: `src/C2C.DevicePlatform.slnx`, `src/C2C.DevicePlatform.Web/*`, `src/C2C.DevicePlatform.Api/*`, `src/C2C.DevicePlatform.Application/*`, `src/C2C.DevicePlatform.Domain/*`, `src/C2C.DevicePlatform.Infrastructure/*`, `tests/C2C.DevicePlatform.Tests/*`.
- Validaciones ejecutadas: `dotnet build src/C2C.DevicePlatform.slnx` exitoso y `dotnet test src/C2C.DevicePlatform.slnx` exitoso (1/1 pruebas).
- Riesgos detectados: advertencia NU1903 en `Microsoft.OpenApi` del template Web API.
- Estado final: COMPLETADO

### 2026-08-14 (ejecucion item WEB-002)
- Fecha: 2026-08-14
- Item backlog: WEB-002
- Tipo de cambio: seguridad
- Resumen: se implemento autenticacion OIDC en Blazor Web y JWT en API; se agrego politica `AdminMfa` para validar segundo factor por claims `amr/acr`.
- Archivos impactados: `src/C2C.DevicePlatform.Web/Program.cs`, `src/C2C.DevicePlatform.Web/appsettings.json`, `src/C2C.DevicePlatform.Api/Program.cs`, `src/C2C.DevicePlatform.Api/Security/*`.
- Validaciones ejecutadas: `dotnet test src/C2C.DevicePlatform.slnx` exitoso.
- Riesgos detectados: pendiente parametrizar secretos reales por ambiente (secret store).
- Estado final: COMPLETADO

### 2026-08-14 (ejecucion item WEB-003)
- Fecha: 2026-08-14
- Item backlog: WEB-003
- Tipo de cambio: seguridad
- Resumen: se aplicaron politicas RBAC por rol/ambiente y se protegieron endpoints de gestion de dispositivos y chequeo MFA administrativo.
- Archivos impactados: `src/C2C.DevicePlatform.Api/Controllers/*`, `src/C2C.DevicePlatform.Api/Contracts/*`, `src/C2C.DevicePlatform.Api/Services/*`, `src/C2C.DevicePlatform.Api/Security/*`.
- Validaciones ejecutadas: `dotnet test src/C2C.DevicePlatform.slnx` exitoso.
- Riesgos detectados: persistencia actual en memoria temporal hasta implementar capa de datos definitiva.
- Estado final: COMPLETADO

### 2026-08-14 (avance WEB-004 + migraciones Supabase + hardening)
- Fecha: 2026-08-14
- Item backlog: WEB-004
- Tipo de cambio: tecnico
- Resumen: se implemento persistencia de catalogo de dispositivos en Supabase PostgreSQL (repositorio + servicio asincrono), se reemplazo store en memoria, se agregaron migraciones SQL versionadas up/down y se completo UI basica de ABM en Blazor (listar, registrar/editar, asignar y desactivar).
- Archivos impactados: `src/C2C.DevicePlatform.Api/Controllers/*`, `src/C2C.DevicePlatform.Api/Program.cs`, `src/C2C.DevicePlatform.Api/appsettings.json`, `src/C2C.DevicePlatform.Application/*`, `src/C2C.DevicePlatform.Infrastructure/*`, `database/supabase/migrations/*`, `src/C2C.DevicePlatform.Api/C2C.DevicePlatform.Api.csproj`.
- Validaciones ejecutadas: `dotnet build src/C2C.DevicePlatform.slnx` exitoso; `dotnet test src/C2C.DevicePlatform.slnx` exitoso.
- Riesgos detectados: advertencia NU1903 vigente en `Microsoft.OpenApi 2.4.0`; mantener seguimiento hasta version compatible no vulnerable con `Microsoft.AspNetCore.OpenApi`.
- Estado final: EN_VALIDACION

### 2026-08-14 (continuidad sin automatizacion - WEB-004)
- Fecha: 2026-08-14
- Item backlog: WEB-004
- Tipo de cambio: funcional
- Resumen: se continuo ejecucion sin automatizacion CI/CD, incorporando cliente API autenticado en Blazor y pagina `/devices` para ABM operativo minimo.
- Archivos impactados: `src/C2C.DevicePlatform.Web/Program.cs`, `src/C2C.DevicePlatform.Web/appsettings*.json`, `src/C2C.DevicePlatform.Web/Api/DeviceAdminApiClient.cs`, `src/C2C.DevicePlatform.Web/Components/Pages/Devices.razor`, `src/C2C.DevicePlatform.Web/Components/Pages/Devices.razor.css`, `src/C2C.DevicePlatform.Web/Components/Layout/NavMenu.razor*`, `src/C2C.DevicePlatform.Api/Controllers/DevicesAdminController.cs`, `src/C2C.DevicePlatform.Api/Services/DeviceCatalogService.cs`, `src/C2C.DevicePlatform.Application/Repositories/IDeviceCatalogRepository.cs`, `src/C2C.DevicePlatform.Infrastructure/Repositories/SupabaseDeviceCatalogRepository.cs`.
- Validaciones ejecutadas: `dotnet build src/C2C.DevicePlatform.slnx` exitoso; `dotnet test src/C2C.DevicePlatform.slnx` exitoso.
- Riesgos detectados: pendiente smoke test manual con IdP real para verificar token `access_token` y permisos por rol en `/devices`.
- Estado final: EN_VALIDACION

### 2026-08-14 (ejecucion item WEB-005)
- Fecha: 2026-08-14
- Item backlog: WEB-005
- Tipo de cambio: funcional
- Resumen: se implemento importacion masiva CSV con validacion por fila y reporte de errores; filas invalidas no interrumpen el lote. Se agrego endpoint API y UI en Blazor para carga y visualizacion de resultados.
- Archivos impactados: `src/C2C.DevicePlatform.Api/Controllers/DevicesAdminController.cs`, `src/C2C.DevicePlatform.Api/Services/DeviceCatalogService.cs`, `src/C2C.DevicePlatform.Api/Contracts/DeviceAdminContracts.cs`, `src/C2C.DevicePlatform.Web/Api/DeviceAdminApiClient.cs`, `src/C2C.DevicePlatform.Web/Components/Pages/Devices.razor`, `tests/C2C.DevicePlatform.Tests/*`.
- Validaciones ejecutadas: `dotnet test src/C2C.DevicePlatform.slnx` exitoso (2/2 pruebas).
- Riesgos detectados: smoke test funcional pendiente con IdP real y archivo CSV de negocio; warning NU1903 permanece vigente en `Microsoft.OpenApi 2.4.0`.
- Estado final: EN_VALIDACION

### 2026-08-14 (ejecucion item WEB-006)
- Fecha: 2026-08-14
- Item backlog: WEB-006
- Tipo de cambio: funcional
- Resumen: se reforzo el flujo de asignacion dispositivo-comercio-sucursal-caja con validacion de entidades activas por ambiente y manejo de error guiado para objetivos inexistentes o inactivos.
- Archivos impactados: `src/C2C.DevicePlatform.Api/Controllers/DevicesAdminController.cs`, `src/C2C.DevicePlatform.Api/Services/DeviceCatalogService.cs`, `src/C2C.DevicePlatform.Application/Repositories/IAssignmentTargetRepository.cs`, `src/C2C.DevicePlatform.Infrastructure/Repositories/SupabaseAssignmentTargetRepository.cs`, `src/C2C.DevicePlatform.Infrastructure/Repositories/SupabaseDeviceCatalogRepository.cs`, `database/supabase/migrations/20260814_002_active_assignment_entities.*`.
- Validaciones ejecutadas: `dotnet test src/C2C.DevicePlatform.slnx` exitoso.
- Riesgos detectados: requiere smoke test con catalogo real activo/inactivo por ambiente para validar mensajes funcionales finales.
- Estado final: EN_VALIDACION

### 2026-08-14 (ejecucion item WEB-007)
- Fecha: 2026-08-14
- Item backlog: WEB-007
- Tipo de cambio: funcional
- Resumen: se implementaron templates de configuracion por ambiente con versionado incremental y rollback (creacion de nueva version desde una version fuente), incluyendo API y UI operativa.
- Archivos impactados: `src/C2C.DevicePlatform.Api/Controllers/EnvironmentConfigsController.cs`, `src/C2C.DevicePlatform.Api/Services/EnvironmentConfigTemplateService.cs`, `src/C2C.DevicePlatform.Api/Contracts/EnvironmentConfigContracts.cs`, `src/C2C.DevicePlatform.Application/Repositories/IEnvironmentConfigRepository.cs`, `src/C2C.DevicePlatform.Domain/Configuration/EnvironmentConfigTemplate.cs`, `src/C2C.DevicePlatform.Infrastructure/Repositories/SupabaseEnvironmentConfigRepository.cs`, `src/C2C.DevicePlatform.Web/Api/DeviceAdminApiClient.cs`, `src/C2C.DevicePlatform.Web/Components/Pages/ConfigTemplates.razor*`, `src/C2C.DevicePlatform.Web/Components/Layout/NavMenu.razor*`.
- Validaciones ejecutadas: `dotnet test src/C2C.DevicePlatform.slnx` exitoso (5/5 pruebas).
- Riesgos detectados: pendiente validacion funcional con payloads JSON de negocio y aprobacion de formato por operaciones.
- Estado final: EN_VALIDACION

### 2026-08-15 (ejecucion WEB-008 a WEB-018)
- Fecha: 2026-08-15
- Item backlog: WEB-008, WEB-009, WEB-010, WEB-011, WEB-012, WEB-013, WEB-014, WEB-015, WEB-016, WEB-017, WEB-018
- Tipo de cambio: funcional | seguridad | QA | documentacion
- Resumen:
	- WEB-008: se implementaron overrides por dispositivo y resolucion de configuracion efectiva (template + override con precedencia).
	- WEB-009: se activo workflow de doble aprobacion para cambios criticos (publish, rollback, override), bloqueando auto-aprobacion del solicitante.
	- WEB-010: se incorporo dashboard operativo por ambiente en API y Home Blazor (online/offline/maintenance/alerts/last activity).
	- WEB-011: se agrego detalle avanzado de dispositivo con historial de asignaciones paginado, configuracion efectiva y eventos recientes filtrables.
	- WEB-012: se agrego servicio de exportacion asincrona con endpoints start/status/download y UI de seguimiento en Home.
	- WEB-013: se reforzo hardening con scoping por ambiente (IDOR), validaciones de entrada y bloqueo de payloads scriptados.
	- WEB-014: se implemento auditoria sensible transversal y migracion de inmutabilidad para `audit_entries`.
	- WEB-015: se aplico mascarado de datos sensibles en respuestas y metadatos de auditoria.
	- WEB-016: se amplio la suite unitaria con pruebas de governance, masking y scope de seguridad.
	- WEB-017: se agregaron artefactos de performance con k6 y umbrales p95 para listados/dashboard.
	- WEB-018: se creo checklist Go/No-Go operativo por ambiente con gates de seguridad, QA, performance y operacion.
- Archivos impactados:
	- API: `src/C2C.DevicePlatform.Api/Controllers/*`, `src/C2C.DevicePlatform.Api/Services/*`, `src/C2C.DevicePlatform.Api/Security/*`, `src/C2C.DevicePlatform.Api/Contracts/*`, `src/C2C.DevicePlatform.Api/Program.cs`.
	- Application/Domain/Infrastructure: nuevos contratos de repositorio y entidades de governance/auditoria/config efectiva, repositorios Supabase extendidos.
	- Web: `src/C2C.DevicePlatform.Web/Components/Pages/Home.razor*`, `src/C2C.DevicePlatform.Web/Components/Pages/Devices.razor*`, `src/C2C.DevicePlatform.Web/Components/Pages/ConfigTemplates.razor*`, `src/C2C.DevicePlatform.Web/Api/DeviceAdminApiClient.cs`, `src/C2C.DevicePlatform.Web/Components/Layout/NavMenu.razor`.
	- Data: `database/supabase/migrations/20260815_003_governance_and_audit_immutability.*`, `database/supabase/migrations/MIGRATION_ORDER.md`, `database/supabase/README.md`.
	- QA/Perf/Readiness: `tests/C2C.DevicePlatform.Tests/*`, `tests/performance/*`, `GO_NO_GO_CHECKLIST.md`.
- Validaciones ejecutadas: `dotnet test src/C2C.DevicePlatform.slnx` exitoso (12/12 pruebas).
- Riesgos detectados: warning NU1903 permanece vigente en `Microsoft.OpenApi 2.4.0`; requiere remediacion de dependencia para cierre productivo.
- Estado final: EN_VALIDACION

### 2026-08-18 (remediacion de dependencias + prevalidacion automatica)
- Fecha: 2026-08-18
- Item backlog: WEB-016, WEB-017, WEB-018 (soporte de cierre)
- Tipo de cambio: tecnico | seguridad | QA
- Resumen: se actualizaron dependencias de plataforma (`Microsoft.AspNetCore.* 10.0.11` y `Microsoft.OpenApi 2.7.5`), se agrego script de prevalidacion automatica y se ejecuto build/test completo del stack web.
- Archivos impactados: `src/C2C.DevicePlatform.Api/C2C.DevicePlatform.Api.csproj`, `scripts/prevalidacion-automatica.ps1`, `GO_NO_GO_CHECKLIST.md`.
- Validaciones ejecutadas: `dotnet test src/C2C.DevicePlatform.slnx` exitoso (12/12), `dotnet build src/C2C.DevicePlatform.Web/C2C.DevicePlatform.Web.csproj` exitoso, `dotnet list src/C2C.DevicePlatform.slnx package --vulnerable --include-transitive` sin vulnerabilidades reportadas.
- Riesgos detectados: quedan pendientes validaciones de ambiente (IdP real, performance k6 en entorno objetivo, migraciones y cierre GO/NO-GO por ambiente).
- Estado final: EN_VALIDACION

## Formato para proximos cambios
- Fecha:
- Item backlog:
- Tipo de cambio: funcional | tecnico | seguridad | QA | documentacion
- Resumen:
- Archivos impactados:
- Validaciones ejecutadas:
- Riesgos detectados:
- Estado final: PENDIENTE | EN_EJECUCION | EN_VALIDACION | COMPLETADO
