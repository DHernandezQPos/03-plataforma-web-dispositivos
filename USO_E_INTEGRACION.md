# USO E INTEGRACION - Proyecto 3 Plataforma Web de Dispositivos

## 1. Proposito
Este documento describe como usar e integrar la plataforma web de dispositivos para administracion operativa y gobierno por ambiente.

## 2. Perfiles de uso
- Administrador de plataforma.
- Operaciones.
- Soporte.
- Visualizador de comercio.

## 3. Flujo operativo principal
1. Registrar dispositivo en ambiente correcto.
2. Asignar dispositivo a comercio/sucursal/caja.
3. Aplicar configuracion base y ajustes por dispositivo.
4. Monitorear estado operativo desde dashboard.
5. Auditar cambios y exportar reportes cuando se requiera.

## 3.1 Importacion masiva CSV
- Endpoint API: `POST /api/devices/import`.
- Columnas esperadas: `DeviceId, MerchantId, BranchId, RegisterId, Environment, Status`.
- Reglas: cada fila se valida de forma independiente; una fila invalida no detiene el resto.
- Resultado: resumen con total procesado/importado/fallido y detalle de errores por fila.
- UI: disponible en la pagina `/devices` para roles operativos.

## 3.2 Asignacion validada por entidades activas
- Endpoint API: `PUT /api/devices/{deviceId}/assign`.
- Validaciones: el objetivo debe existir y estar activo en organizaciones/sucursales/cajas para el ambiente del dispositivo.
- Error guiado: si no existe o esta inactivo, retorna mensaje funcional para correccion de datos.
- Trazabilidad: cada reasignacion crea un registro en `DeviceAssignments` y desactiva la asignacion previa.

## 3.3 Templates de configuracion por ambiente
- Endpoints API:
- `GET /api/environment-configs/{environment}/{configKey}`
- `POST /api/environment-configs`
- `POST /api/environment-configs/{environment}/{configKey}/rollback`
- UI: pagina `/config-templates` para publicar nuevas versiones, consultar historico y ejecutar rollback.
- Regla de versionado: cada publicacion incrementa version por `environment + configKey`.
- Regla de rollback: no sobrescribe versiones; crea una nueva version copiando una version origen.

## 4. Integracion con ecosistema
- Sincronizacion de inventario con backend C2C.
- Integracion con proveedor de identidad para login y roles.
- Integracion con sistemas de monitoreo y alertas.

## 5. Reglas de gobierno
- No mezclar operaciones entre ambientes.
- Aplicar control de permisos por rol.
- Registrar auditoria en cada accion sensible.
- Controlar cambios criticos con aprobacion cuando aplique.

## 6. Manejo de errores de usuario
- Dispositivo duplicado: informar conflicto y sugerir busqueda previa.
- Asignacion invalida: mostrar entidad faltante y accion correctiva.
- Permiso insuficiente: bloquear accion y guiar al rol requerido.
- CSV con filas invalidas: informar fila y motivo, permitiendo continuar con el resto del lote.

## 7. Manejo de errores tecnicos
- Timeout de API: reintento controlado.
- Integracion C2C no disponible: modo degradado y alerta.
- Error de exportacion: reproceso en segundo plano.
- JSON invalido en templates: rechazo controlado con mensaje de validacion en API/UI.

## 8. Seguridad minima
- Autenticacion robusta con MFA para perfiles criticos.
- Autorizacion por rol y ambiente.
- Protecciones CSRF/XSS/IDOR.
- Enmascaramiento de datos sensibles.

## 9. Operacion y soporte
- Dashboard por ambiente para estado de dispositivos.
- Historial y bitacora de cambios para auditoria.
- Procedimiento de escalamiento para incidentes de permisos o configuracion.

## 10. Checklist de salida a produccion
- Matriz de roles aprobada.
- Pruebas RBAC y ambiente cruzado en verde.
- Auditoria de cambios validada.
- Rendimiento de consultas y exportaciones dentro de objetivo.

## 11. Persistencia y migraciones
- Base de datos objetivo: Supabase PostgreSQL.
- Scripts de migracion: `database/supabase/migrations` en formato up/down versionado.
- Parametro obligatorio de API: `ConnectionStrings:Supabase`.
- Ejecutar migraciones por ambiente en orden definido antes de habilitar operaciones.
