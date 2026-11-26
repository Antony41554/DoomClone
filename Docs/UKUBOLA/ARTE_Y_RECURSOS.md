# Dirección artística y producción

Referencia principal: opción C proporcionada por el usuario. Paneles con profundidad, biseles y uniones; metal desgastado; acentos cian/naranja; suelo húmedo selectivo; personajes segmentados con volumen; arma central con manos y movimiento mecánico; humo/vapor localizado; HUD metálico legible.

Paleta común: carbón, acero verdoso, hueso, amarillo industrial. Outpost: gris/verde y cian; Foundry: óxido/ámbar; Core: grafito, rojo y violeta. Llaves: color más símbolo. Evitar realismo militar, superficies uniformemente brillantes y efectos que tapen objetivos.

## Fuentes y generación

- Unity Mesh API: geometría diseñada por partes, UV, normales y biseles; no cajas aleatorias como acabado.
- Python/Pillow/NumPy: albedo, normal, máscara metálica/oclusión/smoothness y emisión; patrones estructurales con variación secundaria.
- Editor API: importación y materiales URP/Lit; clips mediante curvas y jerarquías articuladas. Movimiento lógico independiente de animación.
- Particle System con pools y presupuestos. Sondas y horneado para geometría estática; luz dinámica limitada.
- Sonidos originales sintetizados por capas; variantes, normalización y asignación automática. Música en streaming.
- No utilizar assets extraídos de juegos comerciales. Registrar procedencia/licencia por familia.

## Inventario objetivo

24 módulos de arquitectura; 18 props compartidos y seis distintivos; 18 familias de materiales de misión y seis funcionales; cuatro enemigos con seis familias de animación; ocho armas, manos compartidas y cinco estados; 23 recogibles en siete familias; diez mecanismos base; doce familias VFX; paneles/atlas/fuente y 16 rostros; ocho conjuntos de sonido de armas, cuatro de enemigos, mecanismos y ambientes; tres temas de misión y uno de menú.

## Presentación

16:9 predeterminado; 4:3 opcional, sin deformación. Alturas internas 270/360/540, ancho derivado del aspecto. HUD independiente, cámara de arma con profundidad propia. Bloom contenido, Neutral, sin motion blur/TAA. SSAO por perfil; reflejos estáticos con sondas y humedad selectiva. Haces/niebla localizada en vez de volumetría global. Calidad se valida en juego, no por inventario.
