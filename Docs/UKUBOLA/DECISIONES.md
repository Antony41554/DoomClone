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
| D011 | P18 usa pipelines y renderers propios por perfil, sin cambiar PC_RPAsset ni sus Renderer Features. El pipeline anterior se restaura al salir de la escena. |
| D012 | El laboratorio artístico conserva RetroDisplay desactivado como adaptador de referencias para QA; ModernDisplay controla la textura de píxeles cuadrados. No se arrastra el HUD antiguo. |
| D013 | Escena temporal mediante Unity API, hashes de salidas y manifiestos; los cambios manuales se protegen antes de regenerar. Los bakes se ejecutan separadamente y se registran. |
| D014 | Los informes QA distinguen el tamaño de salida renderizada y el de la ventana física. Un layout 1440p offscreen no certifica una ventana 1440p en este monitor ni rendimiento final. |
| D015 | Contrato del build ArtPreview: conservar ShadowCaster base y todas las variantes receptoras que URP activa por perfil, aunque no haya luces Directional. El filtro solo excluye funciones ausentes (GI dinámica, SSR, irradiancia de pantalla y decals), además de combinaciones locales de materiales no presentes. No se aplica a builds de campaña. La matriz Windows decide su aceptación. |
| D016 | El 26/11/2026 el usuario prioriza entrega antes de las 13:00 de Lima y pide subida correcta a GitHub. Se cierra una preview jugable de Outpost junto a la campaña original conservada; se aplazan las reconstrucciones restantes y el acabado/validación final. Publicar código y ZIP verificados está autorizado por esta nueva instrucción. No marcar el plan completo ni P19 visual como aceptados. |
