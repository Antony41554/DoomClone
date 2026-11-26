# Arquitectura y automatización

## Contratos objetivo

MissionDefinition: sectores/plantas/conexiones/objetivos/colocaciones. MissionContext: registro y servicios. IEnemyPresentation e IWeaponPresentation: vista separada de reglas. PickupPresentation: aspecto separado de efecto. ActionBinding: eventos, condiciones y acciones. IPersistentEntity: captura/restauración por ID estable. PresentationProfile: resolución y presupuestos. QaScenario: escenario reproducible. SaveRepository: versión, integridad, ubicación y escritura.

Escenas nuevas con raíces Geometry/Collision/Navigation/Gameplay/Presentation/Services. Manifiestos declaran propiedad de archivos y objetos. Generación determinista e idempotente; no tocar fuentes ajenas ni YAML complejo. Ensamblados nuevos deben conservar GUID/namespaces y validar escenas antiguas.

## Persistencia

v1 intacto; v2 en archivo separado. IDs por clave semántica, no por orden/nombre visible. Guardar jugador, entrada, inventario, enemigos, recogibles, mecanismos, objetivos, encuentros, peligros, plataformas y exploración. Fracción/destino y tiempos restantes. Restaurar mundo -> navegación/actores -> presentación/input sin emitir eventos consumidos. Importación v1 reinicia misión actual con equipo de entrada y totales previos, conservando archivo original.

## Automatización P16

`Ukubola.EditorTools.UkubolaAutomation`: cola serial en Temp/UkubolaAutomation; jobs con GUID, operación y escena opcional. Journal active.json; resultados atómicos en Evidence/Evolution/<id>/result.json. Operaciones cerradas, sin reflexión ni ejecución arbitraria. Un trabajo interrumpido durante mutación no se repite automáticamente. ReloadCheckpoint comprueba recuperación después de recarga de dominio.

PowerShell: `Tools/Invoke-UkubolaJob.ps1 -Operation Audit`. Requiere Editor abierto e importación de scripts terminada. Batch: Unity -batchmode -projectPath <proyecto> -executeMethod Ukubola.EditorTools.UkubolaAutomation.RunBatch -ukubola-operation Audit -logFile <log>. Nunca segunda instancia sobre el mismo proyecto abierto. Fallos devuelven código no cero.

QA exige raíz explícita por argumento -ukubola-qa-root, variable UKUBOLA_QA_ROOT o marcador Editor temporal. SaveStore y preferencias usan almacenamiento aislado. La ejecución normal conserva comportamiento y rutas. QaScenario/run-result.json registran muestras y capturas reales; no certifican calidad ni rendimiento completo de campaña.
