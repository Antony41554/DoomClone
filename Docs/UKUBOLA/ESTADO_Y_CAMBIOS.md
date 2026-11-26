# Estado de implementación

Rama: codex/ukubola-evolution. Base: baee6d2.

| Fase | Estado |
|---|---|
| P16 | Implementada y validada: copia/hashes, aislamiento, cola, fallos, reload, capturas y build de conservación QA2 con carga v1, también desde ZIP extraído. |
| P17 | Base de contratos aceptada: 10/10 EditMode, validación de campaña antigua y regresión Play correctas. |
| P18 | Base gráfica técnicamente aceptada: cuatro perfiles/aspectos, 24 capturas Windows, shaders, escalera/NavMesh, 16 EditMode y hashes. El acabado integrado se evalúa en P19. |
| P19 | Preview funcional comprobada en Editor y Windows: 31 EditMode, cuatro perfiles/aspectos, nueve casos runtime por perfil y ruta completa con armas/daño normales. Generación repetible y bake correctos. Acabado 4/5, duración de exploración y revisión auditiva pendientes. |
| P20–P27 | Aplazadas para priorizar la preview solicitada antes de las 13:00; no se consideran implementadas. Distribución de preview no equivale a P27 final. |

## Cambios iniciales

- Copia con manifiesto SHA256 de Assets/Packages/ProjectSettings y guardados v1.
- Cola Editor con resultados/journal, comandos de menú y entrada batch.
- Aislamiento explícito de preferencias y guardados; comportamiento normal conservado.
- Escenario runtime para capturas y muestras, activado únicamente con QA.
- Scripts de envío de trabajos y comprobación de la copia.

Registrar aquí cada resultado y decisión material, con ruta de evidencia. No afirmar aceptación de una fase hasta completar sus pruebas.

## P16 aceptada

Conservación: Builds/UKUBOLA_Legacy_baee6d2_QA2.zip, SHA256 047e30d7d1da16b5a83f5f9740c5db8c139343bdf3dfdb03e35ef2ee5f1cb961. Primera compilación conservada aparte. Datos productivos y configuración de precarga restaurados/verificados. Capturas negras iniciales rechazadas y corregidas mediante render requests URP. Siguiente: P17, contratos del mundo y persistencia v2; no hay actualización visual aceptada todavía.

## Cambios P17

Ensamblados Runtime/Editor/EditMode; MissionDefinition con sectores/portales/colocaciones; MissionContext con registro por ID y rechazo de ciclos; PersistentBehaviour/IPersistentEntity; WorldRelay/ActionBinding con restauración silenciosa; SaveRepository y MissionSnapshot v2 con backup y migración de entrada v1; presentaciones de enemigos/armas desacopladas y PickupPresentation 3D. Se conservan GUID y namespaces anteriores. La integración completa con jugador/campaña nueva pertenece a P19/P24.

Siguiente fase: P18, sala industrial y sistema de presentación; no extender a campaña hasta superar P19.

## Cierre técnico P18

Build 68cc764922b44986849332fdd893b0eb y matriz presentation-069f565c3b4a4b48a21beb4a44a007d4 correctos. Evidencia versionable en Evidencia/P18/Windows. Se preservan los intentos rechazados. La captura 1440p es una salida real a textura; la ventana física fue 1080p. Rendimiento final, calidad auditiva y umbral visual P19 siguen pendientes. Originales y 377 salidas verificadas; TagManager añade únicamente ViewModel.

Siguiente operación: crear el checkpoint P18, integrar los fuentes de Tools/EvolutionSources/P19 con sus hooks indicados en README, ejecutar pruebas, generar/hornear OutpostSlice mediante API y comprobar mecanismos y guardado en Play/Windows antes de evaluar acabado. No activar P20 hasta aceptar P19.

Checkpoint P18 creado: 439ef80. Se incorporaron 25 fuentes de P19 conservando metas existentes; Assets es la implementación activa, y Tools/EvolutionSources/P19 queda como snapshot de origen, no se debe copiar otra vez sin comparar. Trabajo inicial RunEditTests: 2a6c7d403d85400584e87d477e19ffea. Inspeccionar resultado/compilación antes de generar. P19 no está aceptada.

## Integración y fallos P19

Entrada: checkpoint 439ef80 y contratos P17. Herramientas: Unity 6000.6.10f1/URP 17.6, cola Editor y generadores C#. Se añadieron mecanismos persistentes, guardado de sesión, acústica por sectores, presentación/audio y escenario OutpostSlice sin sustituir escenas antiguas.

- EditMode 2a6c7d403d85400584e87d477e19ffea: 30/30 correctas.
- Generación aa263a9b1cd04411ad12a3bcf5a1e441: correcta. Un intento previo rechazado por nombre de textura incorrecto no alteró las 377 salidas P18; se corrigió la fuente y se añadió prevalidación de mapas.
- Horneado 52c472d1c230458fafd06eeea2100aa1: cuatro lightmaps y cinco sondas de reflexión.
- Play 698c304bd8024079a729f36491e1c3df, 677aaedb2d2140d2a4d9667fb54cd654 y eaeb1959b0db446b8c93afc746a79179: rechazados. La puerta bloqueada y su guardado intermedio pasan, pero el elevador no detectaba al CharacterController tras restaurar y dejaba al jugador abajo. No son pruebas aprobadas del conjunto.
- Corrección activa: candidatos de pasajeros de la escena mediante límites del controlador, transporte ascendente antes de mover la plataforma y descendente después. Trabajo 3399a677d83242cb8f142b296324d208 pendiente de inspección.

La fuente añade además una plataforma de retorno del depósito; requiere regenerar y hornear antes de evaluar el recorrido. Siguiente operación: comprobar transporte real y carga intermedia, corregir cualquier fallo restante y ejecutar exposición tóxica/restauración de actores. No iniciar P20 ni declarar calidad visual, duración o rendimiento sin evidencia.

Regresión posterior: 3399a677d83242cb8f142b296324d208 pasa las primeras cuatro comprobaciones tras corregir el elevador. Horneado bbabc7766eaa4e0bb4e03c0e9de9a8fa correcto; Play 799d5ac9ef0d4c44bf512e423bedadf1 pasa nueve comprobaciones, incluidas obstrucción, techo, barriles, secreto y salida. Capturas/informe parciales en Evidencia/P19/EditorCandidato; no son aceptación visual ni recorrido completo.

Se añadió tipografía original de geometría 5×7 con profundidad, panel de objetivos, ventana hacia almacén y consola superior lateral. La inspección del recorrido detectó una caja ante el secreto y navegación generada antes de terminar la geometría; corregidos en fuente. Generación 8d5c655b47844413a41b5a1651e207cd correcta. Antes hubo un error C# del nuevo controlador QA por nombre `Run` duplicado; corregido a `Execute`. El cliente PowerShell ahora devuelve explícitamente código 0 en éxito. El recorrido activo exige armas/munición normales, motor real, interacción por rayo, enemigos activos, elevador/escalera/secreto, guardado, final y muerte/reintento; no se considera aprobado hasta inspeccionar su informe. Trabajo PlaySliceRoute 6df3d8b4d0a64c0e9a1e39d83e8549a2.

## Cierre reducido por plazo del usuario

La instrucción posterior exige una entrega correcta en GitHub antes de las 13:00 de Lima; ver ENTREGA_PREVIEW.md y D016. Se priorizan Outpost jugable y campaña anterior conservada. P19 no obtiene aceptación visual por este cambio de alcance.

- 31 EditMode correctas: 9194551568024e218074b1f8d2ae82c3, incluida prohibición de controlador QA en producción.
- Recorrido 6df3d8b4d0a64c0e9a1e39d83e8549a2 rechazado por trayectoria contra un barril. Corrección del recorrido, sin retirar el obstáculo diseñado.
- Recorridos 82cf6e26c701495ab332ac667d7b02c4 y b7b3497febb643208276e3cd8b37295c correctos: ambas armas, siete enemigos, ruta elevada y secreto, llamadas/elevador, recursos, bomba, tarjeta, guardado/carga, amboscada, salida y muerte/reintento. Aproximadamente 62 segundos de ruta conocida; no prueba 5–8 minutos de exploración. Final con 90 HP; 11 disparos y 27 rayos dañinos, no 27 impactos de proyectil diferentes.
- Dos generaciones semánticamente idénticas: slice-generation-868e480382de4d43ba6ac0d387d7a81c. Siete actores, ocho recursos, seis movers, un peligro y un secreto. No se exige igualdad binaria de YAML para identidad.
- Horneado c57185a829084ee5963f54c8f4a34e01 y nueve casos 51fb9a34c2db4e8e8c1fb8b563f2a183 correctos después de acabado de armas, tipografía y flag de autorización.
- Validación antigua f71fbb0dd1db4483b240f54ad387817b correcta.
- 17 WAV reutilizados pasan formato/señal/clipping: audio-technical-709f3330b943495fb223868a9b31ab35. Escucha pendiente.
- Checkpoints hasta 439ef80 subidos a origin/codex/ukubola-evolution. Acceso de escritura al repositorio comprobado; sin secretos guardados en archivos.
- Build Windows e72482cf8fb94fb0a25eca46861a1008 en curso. No editar fuentes C# ni generar mientras termina.

Próxima operación concreta: inspeccionar resultado de build, ejecutar matriz Windows y ruta real, verificar hashes originales/generados, crear checkpoint, empaquetar y ejecutar ambos players extraídos. Publicar solo tras esos resultados. Mantener explícitas las fases aplazadas y las limitaciones de acabado/rendimiento.

La primera compilación e72482cf8fb94fb0a25eca46861a1008 pasó cuatro perfiles y ruta completa en Windows: preview-validation-01aa6fce7ac84c058dfa0571981d638a. Se añadió una entrada que congela el mundo hasta comenzar y opciones básicas de calidad/aspecto/FOV/sensibilidad, con QA manual aislado. 31 EditMode posteriores correctas: 96bd75e45be149e6b24e2c59b2ea2dcf. Build de entrega ce6183dd567b46e2a9edb5be4550192e en curso; repetir verificación Windows y comprobar UI antes de empaquetar. No volver a modificar C# durante esta compilación.

Build final de preview ce6183dd567b46e2a9edb5be4550192e correcto (273 s). Matriz y ruta Windows preview-final-f147333044ef49d8bbc2b0c00addcb89 correctas. 634 salidas/fuentes verificadas; copia de 2203 archivos y dos guardados v1 intactos. Evidencia versionable en Evidencia/P19/Windows. El menú mantuvo el mundo en pausa hasta comenzar; Low/Medium/High, aspecto, sensibilidad y FOV cambiaron y se guardaron en preferencias QA. Escape físico del usuario detuvo Computer Use; no seguir controlando ventanas. La revisión manual de teclado/cursor/guardar/cargar quedó incompleta; se conserva el mensaje ClipCursor denegado del entorno de baja integridad. No atribuirlo a una ejecución normal ni declarar esa prueba aprobada. Continuar únicamente comprobaciones automáticas, empaquetado y publicación autorizada.

## Entrega publicada — 26/11/2026, 12:36 Lima

- Código y recursos: commit 01116871f116c4c837726154b864b5c14a6f0fd6, subido a codex/ukubola-evolution. El usuario rechazó modificar main; se conserva su base baee6d2.
- ZIP Windows de 161127806 bytes, 441 archivos comprobados por CRC/extracción/hashes. SHA256 f4c337f8c220ebf18c179739af347d7dd303278f24d1a0e8e5ff377e27d43509.
- extraídos: extracted-checks-4a03ba1c213b465daa3a2dce4bbe494e, ambos correctos. Preview player-5f2c784aa58f4a39887abdae5cc40507 completa ruta; Legacy player-10283b0b4d1f46a09d068b973fe5a4fd carga copia v1. No se alteran partidas originales.
- Release 459726964, etiqueta evolution-preview-2026-11-26, publicada antes del plazo: https://github.com/Antony41554/DoomClone/releases/tag/evolution-preview-2026-11-26. ZIP, checksum e informe adjuntos, digests de GitHub verificados.
- Descarga posterior github-download-f712a930521a4453bd682c36444b20d8: 161127806 bytes y SHA256 idéntico al ZIP local probado.

La entrega reducida queda disponible; no es la finalización del plan completo. Última fase íntegramente aceptada: P18 técnica; P19 tiene preview funcional, con acabado/duración/escucha/revisión manual pendientes. Próximo trabajo, si se retoma el plan: validar entrada/teclado/cursor en un entorno normal, revisar visualmente las seis dimensiones y ampliar la sección antes de P20. Leer D016/D017: mantener main intacta y no reanudar Computer Use sin nueva autorización del usuario.

### Comprobación final de partidas activas

La última comprobación encuentra los slots v1 activos distintos de la instantánea inicial. Investigación: el Editor registra entrada nueva E1M1 con equipo inicial y `MenuFlow.LateUpdate -> WriteSave` a las 17:33:31 UTC. La prueba del ejecutable extraído cargó la copia E1M3 y no registra escritura de producción. No había un trabajo Play de la cola en ese momento. La atribución inicial a la prueba automática era incorrecta.

No se restauró ni sobrescribió el progreso activo. La primaria actual tiene SHA256 8b2d7cb62e84d746905e1282c575b9c52001bdbe1566af08beb889e19f2c3be8; su .bak conserva exactamente la primaria original cc1c7af1c796256fdb97dd31285c0087517b25ba3b3a0cb31b143b56966a2630. La instantánea inicial conserva además el respaldo original 9be0dfa261c1844c76c06b7b8e47b0e7290c2ca6e36dec96dcd6f3757fd9cb55. Copia de las dos versiones activas: Evidence/Evolution/save-change-audit-18c49b61569c43e49b5c91174f0b9c84. El verificador estricto seguirá indicando diferencia de slots activos; no ocultar ese resultado ni restaurar sobre una partida posterior sin revisar su origen. Los 2203 archivos y dos guardados de la copia inicial siguen intactos.
