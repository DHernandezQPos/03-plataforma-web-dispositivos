# DEFINICION - Proyecto 3 Plataforma Web de Dispositivos

## 1. Proposito
Construir una plataforma web para registrar, asignar, configurar y gobernar dispositivos POS en ambientes demo, qa y prod, con controles de seguridad, auditoria y trazabilidad operativa.

## 2. Objetivos
- Controlar el ciclo de vida completo de dispositivos desde una UI segura.
- Aplicar configuraciones por ambiente y por dispositivo con gobernanza.
- Habilitar monitoreo operativo para soporte y operaciones.
- Evitar errores de configuracion mediante validaciones y permisos fuertes.

## 3. Alcance funcional obligatorio
- ABM de dispositivos con importacion masiva.
- Asignacion a comercio/sucursal/caja.
- Configuracion por templates y overrides.
- Vista estado online/offline y ultima actividad.
- Historial de cambios y auditoria inmutable.
- Gestion de usuarios y roles por ambiente.
- Exportaciones operativas y reportes.

## 4. Fuera de alcance V1
- Portal publico para terceros.
- Facturacion y cobranzas de la plataforma.
- BI avanzado embebido.

## 5. Actores
- PlatformAdmin.
- OpsAdmin.
- Support.
- MerchantViewer.
- Equipo de seguridad y compliance.

## 6. Reglas de negocio
- `deviceId` es global y unico por ambiente.
- Ninguna asignacion puede quedar activa si la caja/sucursal no existe.
- Un usuario no puede operar ambientes no autorizados.
- Toda accion sensible requiere auditoria obligatoria.
- Cambios de configuracion generan versionado y posibilidad de rollback.

## 7. Modelo de ejecucion IA/agentes
- Implementacion por hitos de valor verificable.
- Agente frontend: construye UI/UX Blazor y accesibilidad.
- Agente backend: implementa APIs y reglas de negocio.
- Agente seguridad: valida OIDC, RBAC, MFA, CSRF/XSS.
- Agente QA: cobertura funcional, permisos y pruebas de carga.
- Gate por hito con evidencia tecnica y operativa.

## 8. Gate de calidad por hito
- Reglas RBAC comprobadas por pruebas automatizadas.
- Operaciones sensibles auditadas 100%.
- UX de errores clara para usuario no tecnico.
- Performance de listados y filtros dentro de objetivo.

## 9. Validaciones de seguridad obligatorias
| Area | Validacion | Resultado esperado |
|---|---|---|
| AuthN | login sin MFA para rol admin | acceso denegado |
| AuthZ | usuario sin permiso ambiente | HTTP 403 + auditoria |
| Sesion | token vencido | logout controlado |
| Formularios | intento CSRF | rechazo del request |
| Entrada usuario | payload XSS | sanitizado y bloqueado |
| Datos sensibles | intento de ver secreto | valor enmascarado |
| Auditoria | cambio sin traza | operacion bloqueada |

## 10. Validaciones de errores de usuario
| Caso | Validacion | Respuesta UX |
|---|---|---|
| `deviceId` duplicado | unique check | mensaje `DEVICE_ALREADY_EXISTS` |
| Comercio inexistente | FK/lookup | error guiado para correccion |
| Cambio no permitido | policy role/ambiente | mensaje de permisos |
| Formato invalido | validadores de input | errores por campo |
| Exportacion sin filtros | politica de limite | solicitar filtros minimos |

## 11. Control de errores tecnicos
| Falla | Deteccion | Contencion | Recuperacion |
|---|---|---|---|
| API C2C no responde | timeout | fallback de datos cacheados | reintento asincrono |
| Deadlock DB | error SQL | retry transaccional | ajuste indice/consulta |
| Cache inconsistente | checksum/ttl | invalidacion selectiva | recarga de origen |
| pico de carga | metrica p95 | rate limiting parcial | escalado horizontal |
| fallo de exportacion | job error | reintento en background | notificacion al usuario |

## 12. Funcionalidades necesarias completas
- Dashboard operativo por ambiente.
- Buscador avanzado de dispositivos.
- Detalle de dispositivo con sesiones y transacciones recientes.
- Motor de configuracion con versionado.
- Flujo de aprobacion para cambios criticos.
- Centro de auditoria con filtros y exportacion.
- Panel de salud de integraciones.

## 13. Requisitos no funcionales iniciales
- Disponibilidad objetivo 99.9%.
- p95 de consultas de inventario menor a 2 segundos en QA.
- Exportacion de 100k filas menor a 30 segundos.
- Registro de auditoria inmutable y consultable.

## 14. Estrategia de pruebas
- Unit tests de validadores y reglas RBAC.
- Integration tests API + DB + cache.
- UI tests de flujos criticos administrativos.
- Pruebas de seguridad web (CSRF/XSS/IDOR).
- Pruebas de carga en listados, filtros y exportacion.

## 15. Criterios de aceptacion
- Registro/asignacion/configuracion operan sin inconsistencias.
- Ningun usuario cruza permisos de rol o ambiente.
- Auditoria permite reconstruir cada cambio sensible.
- Mensajes de error guian al usuario en correccion inmediata.

## 16. Artefactos obligatorios
- Matriz de permisos y segregacion por ambiente.
- Manual operativo de alta y soporte.
- Plan de pruebas con evidencia.
- Guia de respuesta a incidentes de seguridad.
