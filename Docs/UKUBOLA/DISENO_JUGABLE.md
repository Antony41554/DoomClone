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

## Contratos de aceptación del catálogo

Esta tabla conserva requisitos pendientes, no declara sistemas implementados.

| Familia | Estados y persistencia exigidos | Comprobación mínima |
|---|---|---|
| Puertas | Cerrada/abriendo/abierta/cerrando, fracción y destino; llave/energía independientes | Panel rígido, ocupante protegido; llave correcta/incorrecta; carga intermedia |
| Puertas secretas | Camuflada/descubierta/abierta | Pista visible; descubrimiento y recompensa una vez |
| Interruptor/terminal | Disponible/activado/sin energía; decisión funcional persistida | Acción explícita, condiciones y usos repetidos sin duplicación |
| Generador | Apagado/arrancando/activo | Alimenta sector; ningún apagado bloquea la única ruta obligatoria |
| Elevador/plataforma | Parada/movimiento/bloqueo; fracción, destino y tiempo | Llamada en ambos extremos, transporte y techo; retorno seguro; carga intermedia |
| Puente | Retraído/desplegando/desplegado | Ruta y navegación cambian solo al completar; movimiento determinista |
| Teletransporte | Sin energía/disponible/activación/cooldown | Destino libre y validado; sin bucle; cooldown restaurable |
| Barril | Vida y destrucción | Daño radial ocluido, cadenas, una explosión; restos limitados |
| Caja/rejilla/cristal | Intacto/destruido y recompensa entregada | Colisión coherente, recurso opcional fijo; nunca llave obligatoria |
| Peligros | Nivel/temperatura/ciclo y temporizadores por actor | Apariencia y daño comparten estado; guardado sin descarga inmediata |
| Enemigos | Vida, posición, alerta, rival, cooldown, estado de encuentro | Dolor/muerte silenciosos al restaurar; preparación visible; cadáver reconstruido |
| Armas | Selección, inventario, munición y cadencia | Ocho roles; presentación no decide daño; sin compensar animación con balance |
| Recursos | Disponible/recogido | Si no concede beneficio permanece; límites válidos; sin doble entrega |
| Llaves | Adquirida; color, forma y símbolo | Ninguna llave tras su propia puerta; accesos legibles desde ambos lados |
| Potenciadores | Disponible/activo/aviso/agotado, duración restante | Invulnerabilidad/berserk restaurables; ruta principal independiente |
| Proyectiles | Vuelo/impacto/terminación, transitorios sin guardar | Colisión/oclusión; un impacto; pools limitados |
| Secretos | IDs descubiertos y recompensas | Contador derivado de IDs; acceso y recompensa verificables |
| Objetivos/salida | Pendiente/habilitado/completado | Condiciones de cada misión; resumen/transición exactamente una vez |
| Acústica | Grafo de sectores/portales y amortiguación de puertas | Sectores sellados no reciben alertas a través de muros |
| Automapa | Exploración por sector/planta | Alturas, conexiones, llaves y objetivos distinguibles |

## Recorridos y recompensas que deben conservarse

Outpost: recepción segura con escopeta y Guard; almacén enseña cobertura/barriles; taller ofrece elevador a armadura y atajo; depósito permite arriesgarse por munición o drenar por ruta segura; control restaura energía y autorización; combate final corto con Guard/Imp/Brute. Secretos: rejilla de mantenimiento y nicho de pasarela.

Foundry: bifurcación de recursos; refrigeración transforma metal caliente; elevador obligatorio con llamadas; escaleras conectan galería para enemigos. Lanzacohetes visible antes de un encuentro opcional; puente libera enemigos desde un acceso anunciado. Secretos entre maquinaria, galería y retorno al almacén.

Core: anillo reconocible y sectores A/B en ambos órdenes; A contiene electricidad y corte permanente; B elevador, retorno por teletransporte y Watcher. Ambos circuitos abren el núcleo. Cañón opcional antes de dos grupos finales combinados, tres secretos y estabilización audiovisual del núcleo.
