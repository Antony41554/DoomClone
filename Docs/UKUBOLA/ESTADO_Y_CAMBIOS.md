# Estado de implementación

Rama: codex/ukubola-evolution. Base: baee6d2.

| Fase | Estado |
|---|---|
| P16 | Implementada y validada: copia/hashes, aislamiento, cola, fallos, reload, capturas y build de conservación QA2 con carga v1, también desde ZIP extraído. |
| P17–P27 | Pendientes; no se consideran implementadas. |

## Cambios iniciales

- Copia con manifiesto SHA256 de Assets/Packages/ProjectSettings y guardados v1.
- Cola Editor con resultados/journal, comandos de menú y entrada batch.
- Aislamiento explícito de preferencias y guardados; comportamiento normal conservado.
- Escenario runtime para capturas y muestras, activado únicamente con QA.
- Scripts de envío de trabajos y comprobación de la copia.

Registrar aquí cada resultado y decisión material, con ruta de evidencia. No afirmar aceptación de una fase hasta completar sus pruebas.

## P16 aceptada

Conservación: Builds/UKUBOLA_Legacy_baee6d2_QA2.zip, SHA256 047e30d7d1da16b5a83f5f9740c5db8c139343bdf3dfdb03e35ef2ee5f1cb961. Primera compilación conservada aparte. Datos productivos y configuración de precarga restaurados/verificados. Capturas negras iniciales rechazadas y corregidas mediante render requests URP. Siguiente: P17, contratos del mundo y persistencia v2; no hay actualización visual aceptada todavía.
