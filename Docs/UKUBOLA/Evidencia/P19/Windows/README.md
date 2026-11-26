# Outpost Preview: Windows verificado

Compilación de entrega `ce6183dd567b46e2a9edb5be4550192e`, Unity 6000.6.10f1, Windows x64/D3D11, Development Preview. El proyecto mantiene sus ajustes originales después de compilar. Esto no es la aceptación final P27.

Matriz `preview-final-f147333044ef49d8bbc2b0c00addcb89`: Bajo 720p (480×270 interno), Medio 1080p (640×360), Alto 1440p (960×540) y Medio 4:3 (480×360). Cuatro casos, 24 capturas, nueve comprobaciones de componentes por caso y ausencia de errores de variantes comprobada por el ejecutor. Alto usa salida real a textura de 2560×1440 con ventana física 1920×1080.

Ruta Windows `player-8bb4d6794c0d49bbb703e5f100ae6469`: nueva partida con equipamiento inicial, daño normal, siete enemigos activos, pistola/escopeta, tarjeta, escalera, secreto, elevador y llamadas, armadura, guardado/carga, bomba, emboscada, objetivos, salida, muerte y reintento. Termina con 90 HP, 11 disparos y 28 rayos dañinos. No hay teletransporte ni daño directo a enemigos en este recorrido; se usan los sistemas normales del jugador.

Las 3621 muestras de la ruta dan media 17,15 ms, p95 16,71 ms y p99 17,01 ms. Incluyen capturas y una ruta conocida de aproximadamente un minuto, con límite de 60 FPS. Son diagnóstico de preview, no el benchmark P26 de tres minutos por misión. `allocatedBytes` es memoria asignada por Unity, no memoria total de proceso ni VRAM.

Se observaron el menú de entrada y cambios de Low/Medium/High, aspecto 4:3/16:9, sensibilidad y FOV, guardados en preferencias QA aisladas. El control gráfico se interrumpió por Escape físico del usuario: pausa por teclado, captura del cursor y guardado/carga mediante botones no se declaran revisados manualmente. La ventana de prueba de baja integridad registró ClipCursor con acceso denegado; no se atribuye a una ejecución normal fuera de ese entorno. Los estados de pausa/restauración y la ruta automatizada sí tienen sus pruebas separadas.

Pendientes de aceptación del plan: acabado 4/5 en seis dimensiones, 5–8 minutos de primera exploración, revisión auditiva, reconstrucción restante y validación completa de campaña/rendimiento. Se entrega explícitamente una preview reducida por el plazo del usuario.
