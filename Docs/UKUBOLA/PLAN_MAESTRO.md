# UKUBOLA — Evolución aprobada

Base: baee6d2, Unity 6000.6.10f1, URP 17.6.0. Plan aprobado el 26 de noviembre de 2026.

## Producto y restricciones

Campaña individual Windows x64: The Outpost, The Foundry y The Core; ocho armas y cuatro arquetipos. Ciencia ficción industrial con geometría 3D estilizada, texturas pixeladas, iluminación cian/naranja, metal desgastado, humedad localizada y HUD independiente. Referencia: `References/opcion-C.png` cuando esté disponible. Una captura conceptual no constituye evidencia de implementación.

Conservar combate rápido, exploración, secretos, autoapuntado vertical y guardado. Sin salto, agachado, multijugador o nuevas familias de enemigos. Sin compras, instalaciones ni publicación externa automática. Conservar escenas antiguas, partidas v1 y preferencias de producción. Usar Unity API para escenas y prefabs.

## Fases y puertas de aceptación

| Fase | Entrega | Dependencia y aceptación |
|---|---|---|
| P16 | Rama, copias y hashes; documentación; cola Editor con journal; QA aislado; capturas; compilación de conservación | Ninguna. Probar éxito, fallo, recarga de dominio y carga de copia v1. |
| P17 | MissionDefinition/Context, IDs estables, acciones configurables, presentaciones desacopladas y SaveRepository v2 | P16. Referencias antiguas válidas; restauración de múltiples mecanismos sin repetir eventos. |
| P18 | Kit industrial, materiales URP/Lit, cámaras y perfiles; luz, sondas y efectos | P16/P17. Píxeles cuadrados, sombras, materiales correctos y buena exposición desde varias vistas. |
| P19 | Sección representativa de 5–8 minutos | P17/P18. Dos enemigos y armas, recursos, puertas, drenaje, elevador, barriles, secreto, emboscada, HUD, audio y guardado. >=4/5 en las seis dimensiones; medición real antes de extender. |
| P20 | Ocho armas, cuatro enemigos, 23 recogibles, biblioteca funcional y pools | P19 aceptada. Consumo/cadencia/daño independientes de animación. |
| P21 | The Outpost reconstruido | P20. 18 enemigos, dos secretos, energía y refrigerante; equipamiento inicial suficiente. |
| P22 | The Foundry reconstruido | P21. 24 enemigos, tres secretos, refrigeración, elevador, puente y rutas entre plantas. |
| P23 | The Core reconstruido | P22. 32 enemigos, tres secretos, dos circuitos en orden libre y final en dos grupos. |
| P24 | Menús, HUD, automapa por plantas, controles, opciones, sonido e importación v1 | Tres misiones aceptadas. Inglés inicial y textos centralizados; efectos reducibles. |
| P25 | Regresión completa y persistencia | P24. Tres dificultades; ningún bloqueo, duplicación de recompensas ni pérdida de partidas. |
| P26 | Optimización y perfiles definitivos | P25. Mediciones standalone, no estadísticas del Editor. |
| P27 | Candidato Windows, ZIP verificado, créditos e instrucciones | P25/P26. Cumplir definición de terminado y conservar versión anterior. |

## Ciclo obligatorio por fase

Comprobar cambios y escenas abiertas; declarar fuentes/destinos; generar determinísticamente; validar antes de sustituir; ejecutar pruebas y juego real; comparar capturas y comportamiento; corregir; registrar evidencia y punto recuperable. No marcar aprobado por compilación o cantidad de recursos. No extender la sección representativa si no alcanza la calidad.

## Aceptación global

Ver `VALIDACION_Y_CONTINUIDAD.md`. No terminar hasta completar tres misiones, arsenal, enemigos y mecanismos; guardado/carga y recuperación; conservación v1; calidad visual >=4/5 en cada dimensión; revisión auditiva; rendimiento medido; ejecutable extraído sin Editor; créditos, licencias y hashes; cero defectos críticos/importantes.

## Primera ejecución

P16 comienza con copia de 2203 archivos de Assets/Packages/ProjectSettings y dos archivos de guardado v1. Evidencia y copias se excluyen de Git; scripts y documentación permanecen versionados. La autorización de implementar el plan no equivale a aprobar resultados que aún no se han medido.
