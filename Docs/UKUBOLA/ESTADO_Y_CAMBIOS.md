# Estado de implementación

Rama: codex/ukubola-evolution. Base: baee6d2.

| Fase | Estado |
|---|---|
| P16 | Implementada y validada: copia/hashes, aislamiento, cola, fallos, reload, capturas y build de conservación QA2 con carga v1, también desde ZIP extraído. |
| P17 | Base de contratos aceptada: 10/10 EditMode, validación de campaña antigua y regresión Play correctas. |
| P18 | Base gráfica técnicamente aceptada: cuatro perfiles/aspectos, 24 capturas Windows, shaders, escalera/NavMesh, 16 EditMode y hashes. El acabado integrado se evalúa en P19. |
| P19 | Fuentes candidatos preparados fuera de Assets; integración, compilación, recorrido, audio y evaluación visual pendientes. |
| P20–P27 | Pendientes; no se consideran implementadas. |

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
