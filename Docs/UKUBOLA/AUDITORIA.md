# Auditoría de la base

Fecha: 2026-11-26. Proyecto `C:/Users/Anthony/Unity/DoomClone`, commit base baee6d2.

- Unity 6000.6.10f1, URP 17.6.0, Input System 1.20.1, UGUI 2.6.0 y Test Framework 1.8.0 instalado transitivamente.
- 128 scripts, 727 PNG, 48 WAV, 29 prefabs, 25 escenas y cuatro shaders antes de la evolución.
- Campaña `_Save`: 6/8/10 enemigos y 25/24/24 recogibles; cuatro puertas, dos interruptores y dos barriles por misión. Un elevador, peligro, secreto y teletransportador en cada una.
- Fase 15 contiene 582 PNG y 25 WAV, pero no ReleaseArt generado ni componentes ReleasePresentation conectados en las escenas `_Save`.
- La auditoría anterior ejecutó título y las tres misiones sin guardar escenas o partidas. Presentación de mampostería homogénea, sprites sencillos y poca iluminación material.
- i3-8100, GTX 1650 ~4 GB, RAM 8 GB. Python 3.13/Pillow/NumPy y Windows Build Support localizados. Blender/FFmpeg no localizados; no son dependencias.
- Limitaciones: mapa de una superficie con alturas 0/.25/.5, techo 3.2 m; giro horizontal; autoaim vertical ±30°; guardado v1 ligado a nombres, orden, cantidades y firma del mapa.
- El perfil PC ya tiene Forward+ y SSAO. Shaders sector/release mayormente unlit; activar opciones no resuelve su falta de respuesta física.
- Código Runtime depende de clases Diagnostics; IA requiere EnemySpriteView; WeaponDefinition y PickupDefinition mezclan reglas y presentación.

## Pendientes en la base

Recorrido completo, ocho armas en combate, todos los mecanismos, prueba de carga del ejecutable, escucha auditiva y rendimiento standalone. Los errores históricos del log no se clasifican como errores actuales sin reproducirlos.

## Protección

`Backups/Evolution/baseline/manifest.json` registra SHA256 y tamaño de 2203 archivos y dos guardados. `Tools/Verify-UkubolaBaseline.py` comprueba copia y guardados originales sin escribirlos. No restaurar toda la copia indiscriminadamente: seleccionar archivos y conservar cambios posteriores.
