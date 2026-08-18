# ARQUITECTURA - Proyecto 3 Plataforma Web de Dispositivos

## 1. Principios
- Seguridad y auditoria por defecto.
- Separacion frontend/backend en capas limpias.
- Segregacion estricta por ambiente.
- UX orientada a operacion y soporte.
- Escalabilidad para inventario masivo.

## 2. Componentes principales
- Blazor Web Frontend.
- API Management Service (ASP.NET Core).
- Device Registry Service.
- Assignment Service.
- Configuration Service.
- Audit Service.
- Auth Service (OIDC integration).
- Supabase PostgreSQL.
- Redis cache para consultas operativas.

## 3. Flujo de alta de dispositivo
1. Usuario con rol `OpsAdmin` abre formulario de alta.
2. UI valida campos obligatorios y formato.
3. API valida RBAC y ambiente autorizado.
4. Device Registry valida unicidad de `deviceId`.
5. Persistencia en Supabase PostgreSQL con auditoria.
6. Resultado a UI con mensaje de exito y `traceId`.

## 4. Flujo de asignacion dispositivo
1. Seleccionar `deviceId`, comercio, sucursal y caja.
2. Validar existencia y estado activo de entidades.
3. Validar politica de ambiente y permisos.
4. Guardar asignacion con version y fecha efectiva.
5. Registrar auditoria de antes/despues.

## 5. Motor de configuracion
- Config base por ambiente (`EnvironmentTemplate`).
- Override por dispositivo (`DeviceOverride`).
- Resolucion final en lectura: base + override.
- Versionado de configuracion con rollback.
- Accion critica opcional con aprobacion de segundo operador.

## 6. Modelo de datos
- `Organizations`
- `Branches`
- `Registers`
- `Devices`
- `DeviceAssignments`
- `EnvironmentConfigs`
- `DeviceConfigOverrides`
- `UserRoles`
- `AuditEntries`

Indices clave:
- `UX_Devices_Environment_DeviceId`.
- `IX_DeviceAssignments_Device_Active`.
- `IX_AuditEntries_Entity_EntityId_Utc`.
- `IX_Devices_Merchant_Status`.

## 7. Seguridad aplicada
- OIDC con MFA para roles admin.
- RBAC por rol y ambiente.
- Proteccion CSRF en formularios.
- Sanitizacion y encoding anti XSS.
- Validacion anti IDOR por resource scoping.
- Enmascaramiento de datos sensibles.
- Auditoria inmutable para operaciones sensibles.

## 8. Manejo de errores de usuario
- Error por campo con mensaje claro y accion sugerida.
- Mensajes consistentes para duplicados y conflictos.
- Prevencion de acciones irreversibles con confirmacion.
- Mensaje de permisos insuficientes sin fuga de informacion.

## 9. Manejo de errores tecnicos
- Timeout API: retry corto del cliente + correlacion.
- Conflicto de concurrencia: optimistic lock y refresh UI.
- Caida de integracion C2C: modo degradado con cache.
- Falla de exportacion: job en background + notificacion.
- Error DB transitorio: retry controlado en servicio.

## 10. Integraciones
- Backend C2C para estado de sesiones/transacciones.
- Identity Provider para autenticacion/autorizacion.
- Servicio de alertas (mail/teams/slack) para eventos criticos.

## 11. Observabilidad
- Logs estructurados con `traceId`.
- Metricas de uso y salud:
- `devices_online_ratio`
- `device_register_latency_ms`
- `config_publish_total`
- `audit_write_failures_total`
- Dashboard por ambiente.
- Alertas por error rate y p95.

## 12. Topologia de despliegue
- Frontend y API en instancias separadas.
- Supabase PostgreSQL administrado en alta disponibilidad.
- Redis replicado.
- Separacion fisica o logica fuerte por ambiente.
- CI/CD con aprobaciones para prod.

## 13. Estrategia de performance
- Paginacion server-side obligatoria.
- Filtros indexados por ambiente/comercio/estado.
- Cache de lecturas frecuentes.
- Exportaciones asincronas para cargas grandes.

## 14. Validacion arquitectonica
- Prueba de carga: 5000 dispositivos con filtros p95 < 2s.
- Prueba de exportacion: 100k filas < 30s.
- Prueba de seguridad RBAC y ambiente cruzado.
- Prueba de auditoria completa en acciones criticas.
- Prueba de resiliencia ante caida temporal de C2C.

## 15. Riesgos y mitigaciones
- Riesgo: error humano en configuracion productiva.
- Mitigacion: flujo de aprobacion y rollback.
- Riesgo: fuga de permisos entre ambientes.
- Mitigacion: policies por ambiente y tests automatizados.
- Riesgo: degradacion de performance por crecimiento de inventario.
- Mitigacion: indices, cache y archivado controlado.
