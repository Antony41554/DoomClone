# P19: candidato Editor, no aceptación de fase

Fuente: trabajo `799d5ac9ef0d4c44bf512e423bedadf1`, Unity 6000.6.10f1, GTX 1650, perfil Medio, mundo 640×360 y salida 984×554 del Game View. Nueve invariantes reales de componentes pasan: tarjeta/puerta en movimiento, transporte del CharacterController, techo y ocupante bajo elevador, puerta obstruida, exposición tóxica/drenaje, actores vivos y muertos, cadena de barriles, secreto/recompensa, objetivos/emboscada/salida exactamente una vez. Guardados aislados; v1 original intacto.

Estas capturas pertenecen a un escenario con actores suspendidos y poses declaradas. Los daños y cambios de estado de la prueba son explícitos; no demuestran combate con armas, recorrido completo ni duración de juego. Los tiempos del Editor no certifican rendimiento Windows.

Revisión: se corrigieron rótulos GUI que atravesaban superficies, consola dentro de una pose y falta de orientación de objetivos. Pendientes: recorrido físico completo, acceso al secreto tras mover una caja, presentación más detallada, respuestas de armas, mezcla auditiva, evaluación de las seis dimensiones y ejecutable. No asignar 4/5 ni aceptar P19 con esta evidencia parcial.

El siguiente generador modifica la caja del secreto y retrasa la navegación hasta que toda la geometría esté creada; requiere un nuevo horneado. El recorrido `OutpostRoute.json` emplea intentos de movimiento, giro, selección, uso y disparo por los sistemas normales; no teletransporta al jugador ni aplica daño directamente durante el combate.
