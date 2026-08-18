# Proyecto 3 - Plataforma Web de registro y configuracion de dispositivos (Demo/QA/Prod)

## 1) Objetivo
Construir una plataforma web para administrar el ciclo de vida de dispositivos POS: registro, asignacion, configuracion remota, gobierno por ambiente y auditoria completa.

## 2) Alcance funcional V1
- Registro manual y carga masiva de dispositivos.
- Asignacion a comercio, sucursal y caja.
- Configuracion por ambiente con templates y overrides.
- Monitoreo operativo online/offline y diagnostico.
- Acciones remotas controladas y auditadas.
- RBAC estricto para demo, qa y prod.

## 3) Modelo de ejecucion con IA y agentes (sin sprints)
- Entrega por hitos de capacidad comprobables.
- Agente web: implementa frontend Blazor y UX operativa.
- Agente backend: implementa APIs, servicios y persistencia.
- Agente seguridad: valida RBAC, MFA, CSRF/XSS y segregacion de ambientes.
- Agente QA: prueba reglas de negocio, permisos y error handling.
- Gate por hito: aprobacion funcional, seguridad, auditoria y performance.

## 4) Hitos de entrega
1. Fundacion: auth, RBAC, estructura base de datos.
2. Inventario: ABM de dispositivos y asignaciones.
3. Configuracion: templates por ambiente y override por dispositivo.
4. Operacion: dashboard, diagnostico y acciones remotas.
5. Gobierno: auditoria inmutable, reportes y exportaciones.
6. Hardening: seguridad final, escalabilidad y readiness productivo.

## 5) Calidad obligatoria por hito
- Sin bypass de permisos por rol o ambiente.
- Registro de auditoria completo para operaciones sensibles.
- Manejo claro de errores de usuario en UI.
- Evidencia de pruebas no funcionales en consultas masivas.

## 6) Validaciones de seguridad y errores
- Seguridad: OIDC, MFA, RBAC, CSRF, XSS, rate limit, secrets management.
- Errores de usuario: datos obligatorios faltantes, `deviceId` duplicado, entidad inexistente, accion no permitida.
- Errores tecnicos: timeout API, bloqueo de DB, integracion C2C no disponible, conflicto de concurrencia.
- Control de errores: mensajes de UX accionables, retries controlados, locks optimistas y logging de causa raiz.

## 7) Documentos tecnicos del proyecto
- Definicion funcional, permisos y controles: `DEFINICION.md`.
- Arquitectura detallada, integraciones y resiliencia: `ARQUITECTURA.md`.

## 8) Entregables
- Plataforma Blazor desplegable por ambiente.
- API de gestion con contratos versionados.
- Manual operativo, matriz RBAC y guias de soporte.
- Evidencia de validaciones funcionales, seguridad y rendimiento.
