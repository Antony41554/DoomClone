# Decisiones técnicas

| ID | Decisión y motivo |
|---|---|
| D001 | Conservar Unity 6000.6.10f1 y paquetes instalados; evitar migración simultánea del motor. |
| D002 | Geometría 3D segmentada original mediante Unity; Python para texturas/audio. Blender no localizado y no requerido para la referencia. |
| D003 | Escenas nuevas para reconstrucción; conservar campaña _Save y build anterior. |
| D004 | Guardado v2 separado. No trasladar posiciones v1 a mapas diferentes; importar entrada de misión y totales previos explícitamente. |
| D005 | No extender contenido antes de aprobar P19 visual y funcionalmente. |
| D006 | Giro horizontal/autoaim existentes; sin salto/agachado; enemigos no usan elevadores. |
| D007 | QA opt-in, fuera de almacenamiento productivo; no usar PlayerPrefs reales durante pruebas. |
| D008 | Cola local Editor para no cerrar ni competir con el proyecto abierto; batch alternativo con códigos de salida reales. |
| D009 | SSR/volumetría global y dependencias externas no son requisitos. Sondas, materiales y efectos locales priorizan el hardware existente. |
| D010 | Ninguna fase se aprueba por compilación, inventario o capturas conceptuales. Evidencia real obligatoria. |
