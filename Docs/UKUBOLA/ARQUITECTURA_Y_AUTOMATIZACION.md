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

## Operaciones P18

`BuildArtRoom`, `BakeArtRoom`, `BuildArtPreview` y `PlayQa -Scene Assets/_UKUBOLA/Scenes/Evolution/ArtRoom.unity`. También aparecen en Tools > UKUBOLA > Evolution. Primero generar texturas con Python. Generar la escena invalida su horneado: volver a hornear antes de compilar. El primer build URP puede tardar más de diez minutos por variantes de shaders; usar `-TimeoutSeconds 1800`. Agotar el tiempo del cliente no cancela el trabajo del Editor: inspeccionar su resultado/journal, sin reenviarlo.

Los destinos son `Art/Evolution/Textures`, `Generated/Evolution` y la escena/carpeta `Scenes/Evolution/ArtRoom`. `SOURCE_MANIFEST.json` y `GENERATION_MANIFEST.json` contienen procedencia, parámetros y hashes. El generador protege modificaciones de salidas registradas; copia una escena temporal mediante EditorSceneManager y sustituye el destino solo tras validar. Materiales, meshes y clips conservan sus assets/GUID; no se editan sus YAML a mano.

Tres pipelines/renderers propios se seleccionan en ModernDisplay y se restaura el pipeline previo al destruirlo. SSAO es una Renderer Feature: desactivada en Bajo, reducida en Medio y completa en Alto. No se ha aprobado todavía su rendimiento. El perfil también controla resolución, sombras y partículas; las opciones del producto pertenecen a P24.

`Tools/QA/ArtRoom.json` define seis posiciones, arma y perfil, congelación de actores para comparación y comprobación física de escalera/navegación. El probe registra dimensiones reales y errores runtime. `Run-UkubolaPlayerQa.ps1` acepta `-Scenario`, `-Quality`, `-ClassicAspect`, `-ScreenWidth` y `-ScreenHeight`, siempre con almacenamiento aislado. Un build de ArtPreview es evidencia técnica local, no una distribución final.

`Tools/Test-UkubolaPresentation.ps1 -Executable <EXE>` ejecuta cuatro casos secuenciales: Bajo 720p, Medio 1080p, Alto 1440p y Medio 4:3. Renderiza mundo y UI a la salida solicitada mediante cámaras reales; el tamaño de ventana física se registra aparte. Comprueba dimensiones, escalera, navegación, ausencia de avisos de shaders y capturas con un mundo no uniforme. Ninguna muestra estática sustituye el recorrido de rendimiento P26.

`python Tools/Verify-UkubolaGenerated.py` comprueba bytes y pertenencia de las salidas declaradas, sin modificarlas; espera a que no haya un trabajo Editor activo. `Tools/Write-UkubolaBuildEvidence.py <carpeta Build> <JSON nuevo>` registra todos los archivos del build y hashes de los fuentes presentes durante la validación. No sobrescribe el ejecutable ni un informe existente. Un hash del bootstrap EXE por sí solo no identifica los datos/shaders de una compilación.
