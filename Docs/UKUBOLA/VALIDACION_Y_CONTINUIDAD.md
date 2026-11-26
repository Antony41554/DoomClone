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

### P17

Primera ejecución NUnit falló en SetUp porque Test Runner crea una escena sin guardar y Unity impide otra escena aditiva. Corrección: escenas de preview aisladas, cerradas en TearDown, sin guardar ni sustituir escenas del usuario.

- EditMode `1d5b9588244a4546b714b41b6c44d253`: 10/10. Renombrar/reordenar, restaurar sin repetir acciones, conjuntos inválidos, IDs duplicados, ciclos, sectores desconectados, corrupción/backup, números/versiones inválidos, revisión e importación v1.
- ValidateLegacy `bfbf30db03174a35bbad862a5883cbd7`: referencias, rutas y firmas correctas después de separar ensamblados.
- PlayQa `fbe9100a31bd4af6b60d63d05934977b`: E1M1 se ejecuta y captura con los adaptadores y ensamblados nuevos; presentación anterior conservada. P17 base aceptada; integración de jugador/mundo v2 se comprobará en P19.

Leer PLAN_MAESTRO, ESTADO_Y_CAMBIOS, DECISIONES y este archivo. Comprobar Git y logs actuales; verificar copia/guardados con Tools/Verify-UkubolaBaseline.py. Inspeccionar journal y resultados antes de reenviar un trabajo. Reanudar la primera fase no aceptada, completar sus pruebas y registrar evidencia. No repetir una mutación interrumpida sin inspeccionar sus salidas. Conservar cambios del usuario. En cada cierre registrar commit, fase aceptada, cambios, pruebas, defectos y siguiente operación concreta.
