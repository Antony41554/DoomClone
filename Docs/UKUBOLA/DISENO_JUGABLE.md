# Diseño jugable aprobado

## Campaña

- E1M1: recepción -> almacén -> taller/elevador opcional -> depósito tóxico/bomba -> control de energía/autorización -> esclusa. 18 enemigos, dos secretos. Escopeta principal; motosierra y ametralladora opcionales.
- E1M2: carga con bifurcación -> fundición -> refrigeración -> galería/elevador de carga -> puente con encuentro anunciado -> salida. 24 enemigos, tres secretos. Lanzacohetes y plasma.
- E1M3: vestíbulo -> anillo -> sectores A/B en cualquier orden -> control -> núcleo -> final. Circuito eléctrico, elevador y teletransporte de retorno; 32 enemigos, tres secretos; cañón opcional; final en dos grupos.

Todas las misiones completables sin armas secretas. Riesgo comprensible y recompensa proporcionada. Alternar preparación, tensión, combate, recuperación y descubrimiento. Salas con función y hito. Narrativa ambiental sin diálogos/cinemáticas extensos.

## Reglas espaciales

Retícula .25 m; pasillos >=2.4 m; escalones compatibles con el jugador. Mantener giro horizontal y autoaim ±30°. Validar alturas, visibilidad y alcance de todo enemigo obligatorio. Sin salto/agachado. Los enemigos usan escaleras; no viajan en elevadores. Elevadores con llamada en ambos extremos y protección contra atrapamiento.

## Catálogo funcional

Puertas normales, por llave, remotas y secretas; interruptores; terminales; generadores; elevadores/plataformas; puentes; teletransportadores; barriles; cajas y rejillas/cristales destructibles; peligros; armas; enemigos; munición/salud/armadura/llaves/mochila/potenciadores; secretos; objetivos/salidas; sectores acústicos y automapa por plantas. Persistir estados funcionales y reconstruir apariencias; partículas/impactos menores transitorios.

Eventos: activar, recoger, morir, entrar, llegar a parada. Acciones: abrir/cerrar, mover, cambiar peligro/energía, iniciar encuentro, avanzar objetivo. Validar referencias/ciclos/duplicaciones. Recompensas de cajas fijas y opcionales; nunca llave obligatoria.

## Peligros Normal

Tóxico 10/s, drenaje 4 s y ruta segura. Metal caliente 20/s, enfriamiento 6 s. Electricidad: aviso .75 s, descarga .75 s, reposo 1.5 s; máximo 15 por descarga/actor. Daño/colisión/apariencia comparten estado, temporizadores por actor y restauración sin descarga injusta. Invulnerabilidad protege. Enemigos evitan peligros y reciben daño si entran.

## Roles

Guard: hitscan anunciado y cobertura; Imp: proyectil esquivable; Brute: presión cuerpo a cuerpo en espacios rodeables; Watcher: control con proyectiles y flotación visual sobre NavMesh.

Puños emergencia; motosierra contacto; pistola precisión económica; escopeta impacto cercano; ametralladora presión sostenida; cohetes daño radial y riesgo; plasma proyectiles rápidos; cañón descarga costosa. Valores anteriores como base; registrar cambios con tiempo/consumo/riesgo.

Dificultades Asistida/Normal/Difícil: modificar recursos/daño recibido, sin alterar llaves, objetivos o rutas. Persistir elección.
