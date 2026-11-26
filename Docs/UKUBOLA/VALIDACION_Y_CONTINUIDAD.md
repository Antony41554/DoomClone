# Validación y continuidad

## Matriz obligatoria

C# y shaders Editor/Windows; referencias/importación; movimiento/input/colisión; ocho armas y cuatro enemigos; facciones y navegación; puertas/acciones/elevadores; peligros por actor; rutas/secretos/objetivos; escritura/backup/corrupción/versiones/importación; campaña/muerte/reintento/final; UI teclado/ratón en 720p/1080p/1440p/4:3; ZIP extraído sin Editor.

Cada sección/misión: seis vistas y recorrido con movimiento, combate y mecanismos, usando mismo FOV/aspecto/perfil para comparar. Calificar 1–5 arquitectura/profundidad, materiales/pixel art, iluminación/atmósfera, enemigos/armas/animación, lectura/interacción y HUD/coherencia. Umbral >=4 en cada dimensión con evidencia; nunca reemplazar defectos funcionales con una puntuación.

## Objetivos de rendimiento, aún no medidos

Equipo auditado, perfil Medio, salida 1080p e interno 640x360. Standalone con calentamiento y >=3 minutos por misión: media >=60 FPS, p95 <=20 ms, p99 <=33 ms; sin pausas recurrentes >50 ms. Proceso objetivo <=2 GB y VRAM <=2.5 GB; orientación <=350 batches/700k triángulos visibles. Sin crecimiento sostenido al repetir/cargar. Carga caliente <=3 s, primer acceso <=10 s. Medir antes de afirmar cumplimiento.

## Definición de terminado

Tres misiones completables, arsenal y mecanismos integrados; ningún bloqueo/pérdida/duplicación; v2 y backup probados; v1 preservado/importación validada; todas las dimensiones visuales >=4; UI y opciones legibles; revisión técnica y auditiva documentada; rendimiento standalone; distribución extraída autónoma; créditos/licencias/hashes; cero defectos críticos/importantes.

## Evidencia inicial

Pendiente ejecutar: Audit, SelfTests, Fail esperado, ReloadCheckpoint, ValidateLegacy, BuildLegacy y prueba standalone de carga de una copia v1. Los resultados serán JSON en Evidence/Evolution. Capturas del probe son reales; medición breve/estática no equivale al benchmark final.

### P16, primera comprobación

- Audit `5d79a173ff934333a0143088a7b4a34c`: correcto. El cliente de espera agotó 90 s durante compilación inicial; el resultado posterior confirma ejecución correcta.
- SelfTests `8f647ba6a02244b982bc7f3fe8e93e64`: diez aserciones, sin cambios productivos.
- Fail `5f41d385a30f42d6a9da95c2530918b9`: fallo intencional y código 1, correcto.
- ReloadCheckpoint `7acb0131f351471081f187aad3b7102c`: recuperación correcta. La duración inicial no sobrevivía al reload; corregida para usar UTC persistido.
- BuildLegacy `530056fc219946888b6d7fed04ef692e`: build y ZIP de 82,902,769 bytes; cuatro escenas validadas.
- Standalone `player-5e6a93de1e154f92a48409af71d432a4`: carga de copia v1 en E1M3 y round trip correctos. Capturas negras por ventana oculta: **rechazadas como evidencia visual**. Sus tiempos no certifican rendimiento.
- PlayQa Editor `d7c5e57f32d146049b8ee7a2d353cad2`: seis capturas visibles de E1M1. Se detectó importación pendiente al trabajar con Editor sin foco; añadir comprobación de fuentes antes de reclamar trabajos.
- Corrección en curso: solicitudes URP de cámaras reales, proyección temporal del Canvas para captura offscreen y rechazo automático de imágenes vacías. Conservar primer build; producir revisión QA2, sin sobrescribirlo.

### Cierre P16

- PlayQa `b47a5a4aeff44d1280996880aaacc025`: solicitudes URP y HUD real verificados visualmente.
- BuildLegacy QA2 `0d503fe824004901a02af52caef1caa7`: correcto; ZIP verificado por SHA256/CRC y extraído (334 entradas).
- Standalone QA2 `player-c17ad55716a1404c9163c5e03e7bd9a2`: copia v1 restaurada y round trip; seis capturas no vacías, revisión visual correcta.
- Ejecutable extraído `player-d5272d18c289402abe9305a87c3fd3a7`: misma carga/round trip y seis capturas correctas sin Editor.
- SelfTests actuales `5d46bdbe09d649678aef2b116e9ef09b`: diez aserciones correctas.
- RestoreBuildSettings `cb9f11492a5e44109164a17586a7ea25`: eliminada mediante API la precarga añadida automáticamente por el build de Input System; configuración original conservada.
- Verificación de los 2203 archivos de copia y dos guardados originales: correcta después de QA.

Los tiempos de 300 fotogramas son muestras de bucle estático; **no** se ha aprobado el objetivo de rendimiento de P26 ni la campaña completa. P16 queda aceptada por infraestructura/recuperación, no por acabado visual.

## Continuidad

### P18, resultados intermedios

- `52cace30d82743ad911736dcfbddb90e`: primera generación correcta; `1c78b6ef2a94453a8bff187d9ce244b0` capturó la sala, pero las luces emisivas apagadas y el exceso de oscuridad exigieron correcciones.
- `7477452983b746899e724f734342f231`, `fe87656614a34d74bab44310edb4a699`, `04ae13cd6b7c49b4a7293e7af7aaf2d8`: horneados correctos. Emisión corregida mediante flags GI; armadura simplificada tras inspección real.
- `a7972434d62b4a61aed9bea76134f823` y `b92dbd43bb874c19b0eb9ab64d52b36d`: generaciones consecutivas correctas, sin duplicar vistas de personajes. Manifiestos protegen salidas.
- `7890fc603d774836a95c71b1e792874a`: seis posiciones, mundo 640x360; CharacterController sube 2.4 m sin salto y NavMesh conecta pasarela. Inspección del Editor correcta. El nuevo detector de fondo uniforme acepta estas capturas.
- `153d0e92daef4b7690701a641d37cd50`: 15/15 pruebas, incluida condición pendiente que no consume ActionBinding de un uso.
- Primer BuildArtPreview `65b740ddbefa45a68694c0bc8f2c7389`: cancelado desde UI tras 702 s y miles de variantes; resultado fallido registrado, no aceptado.
- BuildArtPreview `10e79d5d32574de2ba4ecb708897b9f1`: compilación correcta en 117 s, pero **rechazada visualmente**. El filtrado eliminaba variantes de Light Layers que el pipeline aún activaba. Los procesos `player-ab43cd4653ea424ca65bad763f2bef98` y `player-817ca856ca474e6a8d4ff8b5e3e36547` mostraban HUD/letreros sin superficies Lit. Sus `success=true` anteriores no representan aceptación: el nuevo validador los rechaza (97–99% del mundo uniforme).
- Corrección: desactivar explícitamente Light Layers, LOD crossfade y sombras de luz direccional no usados, antes del filtrado del laboratorio. El filtro está limitado a ArtPreview y no se aplica a la campaña de conservación. Verificar de nuevo en ejecutable antes de aprobar P18.
- El AssetPostprocessor se separó en `Ukubola.Importing` para evitar reimportar todas las texturas por cada cambio del generador Editor. Su primera incorporación sí causó una reimportación global; las reglas solo modifican los recursos de Evolution.

P18 no demuestra calidad >=4/5 de P19 ni rendimiento final. La revisión auditiva y el recorrido completo de campaña siguen pendientes.

### P18, segunda comprobación y recuperación

- `6eb0b234bbbc49179af1ac748a1e6460`: 16/16 EditMode, con independencia de Renderer Features frente al renderer antiguo.
- `art-source-81aa54287061415c9f55e47eb6192191`: prueba externa aislada del generador Python. Dos ejecuciones idénticas, 99 PNG; modificación manual provoca rechazo sin alterar ninguna salida.
- `11113802770140a8b8333d16971eef7b`: generación rechazada correctamente por cambio de hash en Pipeline_0. Se verificó por reconstrucción de hash, sin escribir YAML, que solo habían cambiado campos de prefiltrado de URP durante el build.
- `4d0346d294c84e709ff0488056920bb3`: recuperación mediante SerializedObject de esos campos, comprobando que el archivo completo vuelve al hash registrado. Los builds siguientes capturan/restauran JSON Editor de pipelines y configuración del proyecto; no adoptan cambios desconocidos.
- `8b971885e8004be593f843347058811a` y `bb81e5a79943447dbb4bee1763235958`: generación y horneado posteriores correctos.
- `5c45decac95047d39d0be0d796837d4d`: build correcto, 76 s, pero **rechazado** por el validador del mundo y revisión de `player-6647b92d7431440d83827747bf2946a4`. La configuración global también habilita REFLECTION_PROBE_ROTATION. Se retiró el filtrado de todas las funciones base; quedan solo funciones ausentes del laboratorio: SSR/irradiancia de pantalla, decals y GI dinámica.
- `08b5dde72df54b6592a9e432e42fd62d`: nuevo build con comprobación estricta de variantes. Al último control seguía compilando; la espera PowerShell de 1800 s agotó su plazo y NO canceló el Editor. No considerarlo aprobado, no reenviarlo sin inspeccionar active.json y result.json. Durante un build, QualitySettings/PlayerSettings y prefiltrado pueden estar temporalmente modificados: esperar su finally antes de versionar estos archivos.
- El escritorio devolvió capturas negras/GetCursorPos acceso denegado. Se pidió desbloquear mediante la skill computer-use; el usuario confirmó el desbloqueo. El acceso gráfico volvió a fallar al intentar activar el diálogo modal. Se conservan comprobaciones de archivos y capturas offscreen; no afirmar una inspección UI que no se haya realizado.

Próxima operación: comprobar resultado/progreso del build 08b5; después ejecutar `Run-UkubolaPlayerQa.ps1` contra ese nuevo ejecutable con `Tools/QA/ArtRoom.json`. Validar imágenes con el detector actualizado (excluye el HUD), inspeccionarlas y comprobar cero errores de variantes. Repetir Bajo/Medio/Alto y 4:3, validar campaña antigua y hashes de partidas, registrar evidencia y punto de recuperación. La última fase plenamente aceptada sigue siendo la base P17.

Actualización de esa operación: tras reiniciar el kernel de Computer Use y seleccionar el diálogo modal exacto se recuperó el control. 08b5 se canceló desde el botón de Unity a los 2035 s, con resultado fallido registrado y configuración restaurada. Se añadió filtrado de combinaciones locales de materiales obtenido de los Renderers de la escena, solo para ForwardLit fragment: máscaras 0,7,8 en este laboratorio; conserva las funciones globales.

`cab61bfff04b46a09758d2b1f2c7f554` compiló en 100 s pero reutilizó resultados de shaders (contadores de preprocesado cero). El proceso `player-3a5eac841d6b40168127010ca3b105c3` se detuvo antes de QA con `unity_builtin_extra is corrupted / Position out of bounds`. **Rechazado**. Se activa `BuildOptions.CleanBuildCache`, conservando importaciones y caché de shaders según [Unity](https://docs.unity3d.com/Manual/build-clean-build.html); no se borra Library ni Assets manualmente.

Los intentos `d9d9becdd852483990b5f5160a6724b9` y `32ca` se cancelaron y sus resultados quedaron fallidos. Las capturas correctas del Editor y 16 pruebas están versionables en `Evidencia/P18/Editor/`; no confundirlas con un Windows aceptado.

### P18, Windows con superficies visibles

- Build `a40672e29621480e8555ed40d8549309`: correcto tras 1678 s. Compilación limpia, ámbito del filtro conservado en SessionState y archivo, y espera explícita de importación/compilación antes de reclamar el trabajo. El filtro no eliminó variantes en esta ejecución; no se atribuye una mejora de rendimiento a ese filtro.
- `player-81751bdf16ab44fa8b02fcc669931c30`: seis capturas reales, mundo 640x360 y salida 1920x1080; escalera física y ruta NavMesh correctas. Revisión visual confirma arquitectura, materiales y modelos presentes. **Aún rechazado para aceptación técnica:** los avisos estrictos detectan variantes INSTANCING_ON de sombras Lit y PROCEDURAL_INSTANCING_ON de partículas ausentes. El validador PowerShell ya rechaza esos avisos, además de errores y fondos vacíos.
- Conservación extraída `player-3ebe1f9a217a44efbf2aebeae0d97272`: carga de copia v1 y round trip correctos, seis capturas válidas. No se escribió sobre los guardados originales.
- **Trabajo activo: `75c093ddacb24f149c0a9b5fab6bd5d7`, BuildArtPreviewV2.** Configura temporalmente KeepAll para instancing y restaura GraphicsSettings junto con PlayerSettings, QualitySettings y pipelines al finalizar. No editar ni versionar esa configuración mientras el build siga activo. Inspeccionar journal y resultado antes de reenviar.

Actualización: `75c093...` terminó correctamente en 656 s, pero los procesos Bajo `player-8c59562b86664c29909745a9089dd976` y Alto `player-c3afe90368d941db90cdf10323ae7a8e` siguen mostrando los mismos avisos. KeepAll no resolvió el problema y se retiró. La matriz rechazó el primer caso correctamente. Alto solicitó 2560x1440, pero la ventana real quedó en 1920x1080: no cuenta como prueba de 1440p.

- `aea8477fe08f450a80e6d485ffb71f01`: 16/16 EditMode antes del último ajuste.
- `f582f7903eb9482da0dd13ea16d0ab79` y `1e5887ae42574675a028149a40236426`: generación y horneado correctos. Se desactiva GPU instancing en materiales y partículas de este laboratorio, que usa SRP batching; comprobar el resultado en Windows antes de aceptarlo.
- **Trabajo activo más reciente: `fe185cc27628428894d87222cb8ce638`, BuildArtPreviewV2.** Incluye el ajuste anterior y salida QA a textura con tamaño explícito. Los informes diferencian outputWidth/outputHeight (mundo+HUD realmente renderizados) de windowWidth/windowHeight (ventana física). Esta prueba de layout offscreen no acredita una ventana física 1440p ni rendimiento P26.

Actualización posterior: `fe185...` compiló en 319 s. `player-93b98bfd04a24f0f8de4a0f5dd14f781` tiene superficies visibles y salida QA explícita correcta, pero sigue mostrando avisos de ShadowCaster (ahora sin keywords); se rechaza. Desactivar instancing no era la solución al pase base ausente.

- La lectura del `ShaderScriptableStripper.cs` instalado mostró que, sin sombras principales, URP elimina el caster que no lleva `_CASTING_PUNCTUAL_LIGHT_SHADOW`. Se conservó esa capacidad en los perfiles y se filtraron receptores direccionales no usados por la sala.
- `3396eb62212b41608a6d7ed82d85a887`, generación correcta; el cliente agotó 240 s durante recompilación, pero el trabajo se ejecutó una sola vez al terminar. `1bb8773f9fb246ea8b4c5345cfb5ed6a`, horneado correcto en 42 s.
- `781853b188644523a6ea1e64f84cd3da`, build correcto en 247 s; `player-46db8ee764af427e931160b2bf5fad7d` **rechazado** por capturas con 98–99% del mundo uniforme. Cero avisos de variantes no basta: el log de build confirma `ForwardLit Vertex 64 -> 0` y `Fragment 768 -> 0`. Los tres perfiles seleccionaban únicamente receptores principales que después se filtraban. Se corrige conservando esa capacidad solo en Alto, para que la unión de perfiles conserve también receptores sin sombra principal. Se añadió rechazo del build si Lit Forward queda vacío tras el filtrado.
- `4371a0a0f2f24070b0d4323d4f643428`: diez aserciones de aislamiento correctas. `35b170c478644b3ea5ecfb9e2f287ef2`: referencias, rutas y firmas antiguas correctas. `92fa4e7ea7924677a6ad75434e56fa7a`: 16 EditMode correctas antes de la última corrección de perfiles.
- `Tools/Verify-UkubolaGenerated.py`: 377 salidas con hashes correctos tras el build. Rechaza ejecutar una verificación durante un trabajo activo; no adopta cambios.

`41031e6ce75341c88a563d2b1c9acee7` generó correctamente y `62a1fe6f0df941f6b37f5d385ab70f9e` horneó en 43 s. `75b35a8ce3d645af863f027901f431e0` compiló en 233 s, con ForwardLit 32 vertex/288 fragment conservados. Su inventario completo es `Evidence/Evolution/75b35a8ce3d645af863f027901f431e0/build-files.json`: 326 archivos, 210723350 bytes, con hashes de datos y fuentes; el bootstrap EXE solo no identifica el build.

### P18, matriz parcial correcta y ajuste final de Alto

- Matriz `presentation-fd4c929577b54864947fa25aeb296fe7`: Bajo `player-93ad7ce42201441ebdec590fdc80ca0f` y Medio `player-a51e24f9163c4b8c9a25215c4f3d92d3` correctos, seis capturas válidas cada uno, sin avisos de shader; escalera física y ruta NavMesh correctas. Se inspeccionaron las capturas 01 y 04: superficies, modelos, HUD y pasarela presentes.
- Alto `player-1f885e7c839d40c08bbd2e78fa933660`: **rechazado**. URP activa `_MAIN_LIGHT_SHADOWS` en ese perfil aunque la sala no tenga Directional; el filtro eliminaba esas variantes. Salida real a textura 2560x1440 y ventana física 1920x1080 correctamente diferenciadas, pero no hay aceptación visual de Alto.
- Medio 4:3 `player-efd89a9ceec642899888101d6fefd203`: correcto, seis capturas válidas, 1280x960 de salida y 480x360 internos, escalera y navegación correctas.
- `1e5552bbfe854d8a8910580b10f81e31`: 16/16 EditMode. Verificación externa de 377 salidas, copia de 2203 archivos y dos guardados originales: correcta.
- **Trabajo activo más reciente: `68cc764922b44986849332fdd893b0eb`, BuildArtPreviewV2.** Retira la exclusión de TODAS las variantes receptoras principales; mantiene la capacidad base del caster mediante Alto y las variantes sin sombra mediante los otros perfiles. No requiere regenerar ni hornear otra vez: los datos de sala no cambian. Está compilando variantes adicionales, no se ha aceptado todavía. Timeout del cliente 2400 s no cancela el Editor.

### Cierre técnico P18

- `68cc764922b44986849332fdd893b0eb`: build correcto en 2119 s, 96 variantes vertex y 672 fragment de ForwardLit conservadas. Configuración temporal restaurada; solo ViewModel se añade a TagManager de forma intencional.
- Matriz `presentation-069f565c3b4a4b48a21beb4a44a007d4`: **cuatro casos correctos, 24 capturas válidas y cero avisos de variantes ausentes**. Bajo `player-2b6c2b43e0524f9b9ca01134146f7f97`, Medio `player-7bd462243c4c4bee9f67187d67bef681`, Alto `player-c477548b28fa45f99bd11c3081c9086d`, 4:3 `player-1d27acbe01994d63abce8078984025c8`. Todos pasan escalera física y navegación. Se inspeccionaron las imágenes de Alto y 4:3, además de las vistas anteriores de Medio.
- Hashes posteriores: 377 salidas declaradas correctas. Inventario completo del build: 326 archivos, 213115860 bytes. Evidencia seleccionada, matriz e inventario en `Evidencia/P18/Windows/`.
- Se conservan 16 EditMode correctas, diez aserciones de aislamiento, validación de referencias/rutas antiguas y comprobación de la copia y guardados. La escucha y el rendimiento sostenido no se han aprobado.

La base P18 queda técnicamente aceptada; no equivale al acabado integrado ni al umbral >=4/5 de P19. La ventana física de Alto fue 1080p, y la salida a textura/Canvas realmente renderizada fue 1440p. La legibilidad/pulido del HUD y todos los estados animados siguen bajo revisión en P19/P24.

Operación histórica posterior a P18: integrar los candidatos P19 preservando GUID. Ya se realizó; **Assets es la fuente activa**. No volver a copiar Tools/EvolutionSources/P19 sobre las correcciones actuales. El estado vigente figura en la sección de cierre reducido de ESTADO_Y_CAMBIOS y ENTREGA_PREVIEW.

### P17

Primera ejecución NUnit falló en SetUp porque Test Runner crea una escena sin guardar y Unity impide otra escena aditiva. Corrección: escenas de preview aisladas, cerradas en TearDown, sin guardar ni sustituir escenas del usuario.

- EditMode `1d5b9588244a4546b714b41b6c44d253`: 10/10. Renombrar/reordenar, restaurar sin repetir acciones, conjuntos inválidos, IDs duplicados, ciclos, sectores desconectados, corrupción/backup, números/versiones inválidos, revisión e importación v1.
- ValidateLegacy `bfbf30db03174a35bbad862a5883cbd7`: referencias, rutas y firmas correctas después de separar ensamblados.
- PlayQa `fbe9100a31bd4af6b60d63d05934977b`: E1M1 se ejecuta y captura con los adaptadores y ensamblados nuevos; presentación anterior conservada. P17 base aceptada; integración de jugador/mundo v2 se comprobará en P19.

Leer PLAN_MAESTRO, ESTADO_Y_CAMBIOS, DECISIONES y este archivo. Comprobar Git y logs actuales; verificar copia/guardados con Tools/Verify-UkubolaBaseline.py. Inspeccionar journal y resultados antes de reenviar un trabajo. Reanudar la primera fase no aceptada, completar sus pruebas y registrar evidencia. No repetir una mutación interrumpida sin inspeccionar sus salidas. Conservar cambios del usuario. En cada cierre registrar commit, fase aceptada, cambios, pruebas, defectos y siguiente operación concreta.

### P19 y entrega con plazo

31 EditMode, nueve comprobaciones reales de mecanismos y recorrido completo del Editor han pasado. El recorrido controla intención a través de PlayerMotor/WeaponController/PlayerInteractor; el combate no usa teletransporte ni daño directo. Las pruebas separadas de persistencia sí establecen estados/daños de forma explícita y no se presentan como combate.

El manifiesto registra fuentes generadoras con hash UTF8/LF y salidas con hash binario exacto. Test-UkubolaSliceGeneration compara semántica entre dos generaciones. El empaquetador excluye carpetas DontShip y símbolos, comprueba CRC, extrae a una carpeta nueva y compara cada hash. Finalize-UkubolaPreview exige ejecución correcta de Outpost y carga v1 en los dos ejecutables extraídos. Publish-UkubolaPreview exige esos comprobantes, verifica digest de GitHub y prepara un borrador antes de publicarlo.

El plazo solicitado no elimina las limitaciones: P19 visual/duración/audio y P20–P27 completos no se consideran aceptados. Las muestras estáticas o de una ruta conocida de ~1 minuto no son el benchmark P26 de tres minutos por misión. El cierre actual se documenta como preview.
