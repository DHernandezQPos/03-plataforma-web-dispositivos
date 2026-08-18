# MACRO CLIENTE - Proyecto 3 Plataforma Web de Dispositivos

## 1. Resumen ejecutivo
Este proyecto entrega la plataforma web de gobierno de dispositivos POS para registrar, asignar, configurar y auditar equipos en ambientes demo, qa y productivo de forma segura y controlada.

## 2. Problema que resuelve
- Alta manual desordenada de terminales.
- Errores de configuracion por falta de controles.
- Baja trazabilidad de cambios operativos.
- Riesgo de permisos indebidos entre ambientes.

## 3. Solucion propuesta
- Plataforma Blazor con API dedicada.
- ABM de dispositivos y asignaciones a caja/sucursal.
- Motor de configuracion por ambiente con overrides.
- RBAC estricto, auditoria inmutable y monitoreo operativo.

## 4. Alcance macro
- Registro individual y carga masiva de dispositivos.
- Asignacion a estructura comercial.
- Configuracion centralizada por ambiente.
- Dashboard de estado y diagnostico.
- Historial de cambios y reportes.

## 5. Beneficios para cliente
- Menor error humano en gestion de terminales.
- Mejor control de ambientes y permisos.
- Mayor velocidad operativa para soporte y operaciones.
- Trazabilidad completa para gobierno y compliance.

## 6. Entregables
- Plataforma web desplegable por ambiente.
- API de gestion de dispositivos.
- Matriz de roles y permisos.
- Reportes y exportaciones operativas.

## 7. Validaciones clave para aceptacion
- Unicidad de `deviceId` por ambiente.
- Asignacion valida a entidades existentes.
- Bloqueo de acciones no autorizadas.
- Auditoria obligatoria en acciones sensibles.
- Rendimiento objetivo de listados y exportaciones.

## 8. Riesgos y mitigacion
- Riesgo: cambios no controlados en productivo.
- Mitigacion: aprobaciones y auditoria obligatoria.

- Riesgo: degradacion por volumen de inventario.
- Mitigacion: indices, paginacion y export asincrono.

- Riesgo: errores de permisos.
- Mitigacion: RBAC por rol y ambiente con pruebas automatizadas.

## 9. Modelo de ejecucion
Se implementa por hitos funcionales con agentes de IA (frontend, backend, seguridad, QA y data), sin esquema por sprint y con gates de calidad por etapa.

## 10. Indicadores de exito
- Tiempo de alta y asignacion de terminal.
- Incidentes por permisos o configuracion.
- Tiempo de respuesta p95 en consultas operativas.
- Cobertura de auditoria en operaciones sensibles.
