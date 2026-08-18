# BACKLOG EJECUTABLE POR AGENTES - Proyecto 3 Plataforma Web de Dispositivos

## 1. Objetivo operativo
Entregar una plataforma web Blazor para registrar, asignar y configurar dispositivos POS por ambiente con seguridad fuerte, auditoria inmutable y UX operativa.

## 2. Agentes y responsabilidades
- `Agent-Orchestrator`: orden de implementacion, riesgos, gates.
- `Agent-Web-Frontend`: UI Blazor, formularios, validaciones, accesibilidad.
- `Agent-Web-Backend`: APIs, dominio, persistencia, integraciones.
- `Agent-Web-Security`: OIDC, MFA, RBAC, CSRF/XSS/IDOR.
- `Agent-Web-QA`: pruebas funcionales, seguridad y performance.
- `Agent-Web-Data`: indices, tuning consultas y exportaciones.

## 3. Definition of Ready (DoR)
- Historia definida con rol usuario y resultado esperado.
- Dependencias y permisos identificados.
- Criterios de aceptacion y pruebas asociadas.
- Riesgos de seguridad y error de usuario declarados.

## 4. Definition of Done (DoD)
- Feature funcional y testeada.
- RBAC aplicado y probado.
- Auditoria sensible registrada.
- UX de errores validada con casos reales.

## 5. Backlog atomico por hitos

## Hito A - Fundacion plataforma

### WEB-001 Estructura solucion web
- Agente: `Agent-Web-Backend`
- Dependencias: ninguna
- Tarea: crear solucion con frontend Blazor + API + capas Application/Domain/Infrastructure/Tests.
- Salida: estructura lista para desarrollo.
- Validaciones: build y ejecucion local.
- Criterio: base estable.

### WEB-002 Auth OIDC + MFA
- Agente: `Agent-Web-Security`
- Dependencias: WEB-001
- Tarea: integrar login OIDC y exigir MFA para roles admin.
- Salida: flujo autenticacion productivo.
- Validaciones: login sin MFA denegado para admin.
- Criterio: acceso seguro aplicado.

### WEB-003 RBAC por rol y ambiente
- Agente: `Agent-Web-Security`
- Dependencias: WEB-002
- Tarea: policies por `PlatformAdmin`, `OpsAdmin`, `Support`, `MerchantViewer` y ambiente.
- Salida: autorizacion granular.
- Validaciones: pruebas de acceso horizontal/vertical.
- Criterio: no hay cruce de permisos.

## Hito B - Inventario y asignacion

### WEB-004 ABM de dispositivos
- Agente: `Agent-Web-Backend`
- Dependencias: WEB-001, WEB-003
- Tarea: CRUD de dispositivos con `deviceId` unico por ambiente.
- Salida: endpoints y UI basica.
- Validaciones: duplicado bloqueado.
- Criterio: gestion completa de inventario.

### WEB-005 Importacion masiva de dispositivos
- Agente: `Agent-Web-Data`
- Dependencias: WEB-004
- Tarea: carga CSV con validacion por fila y reporte de errores.
- Salida: proceso batch seguro.
- Validaciones: filas invalidas no detienen toda la carga.
- Criterio: import robusta y trazable.

### WEB-006 Asignacion a comercio/sucursal/caja
- Agente: `Agent-Web-Backend`
- Dependencias: WEB-004
- Tarea: flujo de asignacion con verificacion de entidades activas.
- Salida: `DeviceAssignments` funcional.
- Validaciones: entidad inexistente retorna error guiado.
- Criterio: asignacion consistente.

## Hito C - Configuracion y gobierno

### WEB-007 Templates de configuracion por ambiente
- Agente: `Agent-Web-Backend`
- Dependencias: WEB-003
- Tarea: definir `EnvironmentConfigs` versionados.
- Salida: API + UI de templates.
- Validaciones: versionado y rollback.
- Criterio: cambios trazables.

### WEB-008 Overrides por dispositivo
- Agente: `Agent-Web-Backend`
- Dependencias: WEB-007
- Tarea: `DeviceConfigOverrides` con precedencia sobre template.
- Salida: resolver configuracion efectiva.
- Validaciones: lectura final base + override correcta.
- Criterio: configuracion granular confiable.

### WEB-009 Aprobacion doble para cambios criticos
- Agente: `Agent-Web-Security`
- Dependencias: WEB-007
- Tarea: activar workflow de doble aprobacion para acciones sensibles.
- Salida: politica de cambio controlada.
- Validaciones: cambio critico sin 2da aprobacion se bloquea.
- Criterio: gobernanza reforzada.

## Hito D - Operacion y soporte

### WEB-010 Dashboard operativo por ambiente
- Agente: `Agent-Web-Frontend`
- Dependencias: WEB-004, WEB-006
- Tarea: mostrar online/offline, ultima actividad, alertas.
- Salida: vista operacional.
- Validaciones: consistencia con backend C2C.
- Criterio: soporte puede diagnosticar rapido.

### WEB-011 Detalle avanzado de dispositivo
- Agente: `Agent-Web-Frontend`
- Dependencias: WEB-010
- Tarea: sesiones recientes, transacciones y config efectiva.
- Salida: pantalla de detalle completa.
- Validaciones: datos con paginacion y filtros.
- Criterio: trazabilidad operativa.

### WEB-012 Exportaciones asincronas
- Agente: `Agent-Web-Data`
- Dependencias: WEB-004
- Tarea: export CSV para volumen alto por job en background.
- Salida: servicio de export y notificacion.
- Validaciones: 100k filas dentro de objetivo.
- Criterio: export estable.

## Hito E - Seguridad y auditoria

### WEB-013 Protecciones CSRF/XSS/IDOR
- Agente: `Agent-Web-Security`
- Dependencias: WEB-003
- Tarea: tokens antiforgery, sanitizacion y scoping de recursos.
- Salida: hardening web aplicado.
- Validaciones: pruebas de seguridad web.
- Criterio: ataques comunes mitigados.

### WEB-014 Auditoria inmutable
- Agente: `Agent-Web-Backend`
- Dependencias: WEB-004, WEB-007
- Tarea: registrar before/after de acciones sensibles.
- Salida: `AuditEntries` consultable y exportable.
- Validaciones: toda accion critica genera traza.
- Criterio: cumplimiento de gobierno.

### WEB-015 Mascarado de datos sensibles
- Agente: `Agent-Web-Security`
- Dependencias: WEB-011
- Tarea: ocultar secretos/tokens en UI y logs.
- Salida: politicas de data masking.
- Validaciones: inspeccion de logs y pantallas.
- Criterio: no exposicion de datos sensibles.

## Hito F - Calidad y readiness

### WEB-016 Pruebas funcionales automatizadas
- Agente: `Agent-Web-QA`
- Dependencias: WEB-004..WEB-015
- Tarea: suite para alta, asignacion, config y permisos.
- Salida: pipeline de regresion.
- Validaciones: cobertura de flujos criticos.
- Criterio: regresiones bloqueadas.

### WEB-017 Pruebas de performance
- Agente: `Agent-Web-QA`
- Dependencias: WEB-012
- Tarea: benchmark listados/filtros/export.
- Salida: reporte p95 y tuning recomendado.
- Validaciones: p95 < 2s en filtros de inventario.
- Criterio: objetivo de rendimiento cumplido.

### WEB-018 Go/No-Go operativo
- Agente: `Agent-Orchestrator`
- Dependencias: todas
- Tarea: consolidar evidencias de seguridad, QA y operacion.
- Salida: checklist final por ambiente.
- Validaciones: gates cerrados.
- Criterio: aprobacion de salida.

## 5.1 Estado operativo actual (2026-08-15)
- WEB-001: COMPLETADO
- WEB-002: COMPLETADO
- WEB-003: COMPLETADO
- WEB-004: EN_VALIDACION
- WEB-005: EN_VALIDACION
- WEB-006: EN_VALIDACION
- WEB-007: EN_VALIDACION
- WEB-008: EN_VALIDACION
- WEB-009: EN_VALIDACION
- WEB-010: EN_VALIDACION
- WEB-011: EN_VALIDACION
- WEB-012: EN_VALIDACION
- WEB-013: EN_VALIDACION
- WEB-014: EN_VALIDACION
- WEB-015: EN_VALIDACION
- WEB-016: EN_VALIDACION
- WEB-017: EN_VALIDACION
- WEB-018: EN_VALIDACION

## 6. Prompt operativo recomendado por item
"Implementa el item <ID> del backlog web. Lee PLAN.md, DEFINICION.md y ARQUITECTURA.md. Mantener separacion de capas, seguridad web estricta, UX clara de errores y auditoria completa. Entregar codigo, tests y evidencia del criterio de aceptacion."

## 7. Gate transversal de seguridad y errores
- Todo endpoint con policy de autorizacion.
- Mensajes de error de usuario claros, sin fuga de informacion.
- Errores tecnicos con retry/timeout y registro de causa raiz.
- Toda accion sensible deja auditoria inmutable.
