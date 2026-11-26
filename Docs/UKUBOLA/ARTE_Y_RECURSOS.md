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

## Base P18 implementada

`Tools/Generate-UkubolaArt.py` produce 24 familias originales de 128 px: 96 mapas PBR, dos atlas de interfaz y una textura de vapor. Semillas fijas; el manifiesto de fuentes registra hashes y rechaza recursos modificados antes de sobrescribirlos. La importación distingue albedo/emisión sRGB de normales/máscaras lineales; filtrado Point y mipmaps en materiales.

`UkubolaEvolutionArt` genera meshes biselados y tubos con UV secundarias, materiales URP/Lit, Guard/Imp articulados y seis clips por modelo. La sala `Assets/_UKUBOLA/Scenes/Evolution/ArtRoom.unity` contiene un almacén industrial, pasarela/escalera, contenedores, tuberías, cableado, luminarias, señales, vapor y muestras 3D de recursos. Modelos de armas preliminares: todavía requieren el trabajo de P20.

Iluminación mixta, lightmap de 512 px a 8 texels/m, 28 posiciones de Light Probes y Reflection Probe de 128 px con proyección de caja. El horneado CPU es compatible con esta instalación, aunque Unity emite una advertencia de API obsoleta. Los reflejos son estáticos. El HUD está en un Canvas separado del mundo; el retrato reutiliza el recurso original Phase15 del proyecto.

Se corrigió emisión apagada por `EmissiveIsBlack` al reimportar: ahora cada material declara su participación emisiva y existe una prueba contra regresiones. Se rechazaron las primeras vistas demasiado oscuras y el patrón excesivamente ruidoso de la armadura. La comparación con opción C sigue siendo un criterio de revisión, no una equivalencia alcanzada.
