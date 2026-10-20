# Neutral Exercise Model V1 + Exercise Demo Visual Standard V2

## Propósito

Este documento define la base visual y de producción para las demostraciones técnicas propias de Entrenate.

La figura funciona exclusivamente como un **modelo técnico de movimiento**. Carece de nombre, personalidad e identidad narrativa, y solo aparece dentro de una demostración de técnica.

Orden de prioridad:

1. biomecánica;
2. claridad;
3. consistencia;
4. legibilidad;
5. estética.

## Neutral Exercise Model V1

### Identidad maestra

- adulto joven masculino neutral para V1;
- físico atlético con musculatura visible y realista;
- proporciones humanas naturales;
- articulaciones legibles: hombros, codos, muñecas, caderas, rodillas y tobillos;
- rostro sin identidad, con geometría simplificada y sin expresión protagonista;
- sin barba distintiva, tatuajes, accesorios, reloj, texto ni logos;
- pelo corto y neutro, sin rasgos que lo conviertan en un personaje reconocible.

### Vestimenta fija

- musculosa técnica ajustada negra/grafito;
- short deportivo negro/grafito;
- zapatillas deportivas oscuras y neutras;
- sin marcas, patrones ni colores saturados;
- la ropa debe permitir leer la postura y las articulaciones.

### Materiales y paleta

- piel natural cálida/neutra;
- ropa en negro y grafito mate;
- equipo en negro, grafito y metal;
- sin violeta, azul, neon ni colores de marca sobre el modelo o el equipo.

### Estilo

- ilustración 3D semi-realista limpia;
- premium, moderna y técnica;
- ligeramente estilizada para mejorar lectura;
- anatomía clara y materiales simples;
- iluminación suave, uniforme y ligeramente cálida en piel;
- sin estética anime, cartoon, médica, fotográfica, cinematográfica o de superhéroe;
- sin cel shading, glow ni sombras dramáticas.

### Master visual

El master aprobado para esta primera validación se guarda en:

`frontend/public/exercises/v2/_master/neutral-exercise-model-v1-master.png`

Es la referencia obligatoria de identidad, anatomía, ropa, materiales e iluminación. Los ejercicios futuros deben derivarse de este archivo; no se debe generar un “modelo similar” desde cero.

## Exercise Demo Visual Standard V2

### Canvas y formato

- PNG de 1024×1024 px;
- transparencia alfa real;
- fondo completamente transparente;
- sin escenario, piso ni sombra ambiental horneada;
- sin texto, flechas, números, labels ni marcas de agua;
- modelo y equipamiento ocupan aproximadamente 75–85% del área útil;
- ninguna articulación o pieza necesaria para comprender el movimiento puede quedar recortada.

### Cámara

La cámara se elige por claridad biomecánica:

- lateral;
- 3/4 lateral leve;
- frontal;
- 3/4 frontal leve;
- posterior;
- 3/4 posterior leve;
- superior únicamente cuando sea imprescindible.

La perspectiva preferida es una desviación suave de 10–20°. Se evitan ángulos heroicos, dramáticos o elegidos solo por estética.

### Consistencia dentro de un ejercicio

Entre frames deben permanecer idénticos:

- modelo e identidad faceless;
- ropa, piel y proporciones;
- cámara, altura, focal, perspectiva, zoom y framing;
- iluminación y materiales;
- equipo, carga, posición base y escala;
- agarre, apoyo y posición de pies, salvo que el movimiento exija lo contrario.

Solo cambia la pose necesaria para representar el recorrido.

### Equipamiento

- moderno, simple y sin marca;
- bancos y máquinas negros/grafito;
- barras metálicas y discos negros;
- misma geometría, carga y posición relativa en toda la secuencia;
- no se rediseña ni se deforma entre frames.

### Frames y reproducción

Un ejercicio simple usa cinco frames principales:

1. posición inicial;
2. inicio del recorrido;
3. mitad del recorrido;
4. aproximación final;
5. posición final.

La reproducción futura puede usar el recorrido `1 → 2 → 3 → 4 → 5 → 4 → 3 → 2 → 1`. Las vistas complementarias se agregan solo cuando una única cámara no explica correctamente la técnica.

### Biomecánica

Cada secuencia debe revisar explícitamente:

- posición y estabilidad de hombros;
- trayectoria y alineación de codos;
- posición neutral o requerida de muñecas;
- columna y pelvis;
- rodillas y tobillos;
- trayectoria de la carga;
- agarre y apoyos;
- relación cuerpo/equipamiento;
- rango de movimiento realista, sin exageraciones.

La exactitud técnica prevalece sobre la estética. Una imagen visualmente atractiva se rechaza si comunica una ejecución dudosa.

## Flujo de producción

1. Seleccionar la cámara por claridad biomecánica.
2. Cargar el master visual como referencia de identidad.
3. Generar el frame inicial con modelo, equipo, cámara y framing definitivos.
4. Generar cada frame siguiente usando el master y al menos el frame anterior como referencias.
5. Pedir que cambie únicamente la pose requerida.
6. Normalizar cada salida a PNG 1024×1024 con alfa.
7. Revisar cada frame de forma aislada.
8. Revisar los cinco frames juntos sobre fondo oscuro.
9. Corregir solamente el frame defectuoso cuando la inconsistencia sea local.
10. Integrar al registry únicamente después de la aprobación visual y biomecánica.

### Contrato de prompt

Cada generación debe declarar:

- uso: `scientific-educational`;
- master de identidad y estilo;
- frame anterior como referencia de cámara, equipo y continuidad;
- pose exacta y único cambio permitido;
- invariantes de modelo, ropa, equipo, cámara, escala e iluminación;
- articulaciones y trayectoria que se deben validar;
- transparencia real y prohibición de texto, logos, glow y colores de marca.

## Estructura de archivos V2

```text
frontend/public/exercises/v2/
├── _master/
│   └── neutral-exercise-model-v1-master.png
└── curl-barra-ez/
    ├── frame-01.png
    ├── frame-02.png
    ├── frame-03.png
    ├── frame-04.png
    └── frame-05.png
```

Las demos aprobadas se registran únicamente en `frontend/src/features/exercises/components/exercise-demo-v2-registry.ts` y usan assets bajo `frontend/public/exercises/v2/`.

## Piloto: Curl con barra EZ

### Cámara y framing

- vista 3/4 lateral suave, aproximadamente 15°;
- cámara a la altura de los ojos;
- focal neutra, sin perspectiva heroica;
- cuerpo completo, barra y discos visibles;
- modelo y equipamiento centrados y con margen de seguridad;
- misma escala y encuadre en los cinco frames.

### Equipo y agarre

- barra EZ metálica;
- discos negros pequeños/moderados e idénticos en toda la secuencia;
- agarre semi-supinado coherente con la barra;
- ancho de agarre, manos, muñecas y carga constantes.

### Secuencia

1. `frame-01.png`: brazos casi extendidos; barra delante de los muslos.
2. `frame-02.png`: inicio de flexión; barra se separa ligeramente de los muslos.
3. `frame-03.png`: mitad del recorrido; antebrazos en diagonal.
4. `frame-04.png`: aproximación final; antebrazos cercanos a vertical.
5. `frame-05.png`: contracción superior controlada; barra cerca del torso superior.

### Validación del piloto

- mismo modelo, rostro faceless, ropa y proporciones: aprobado;
- misma cámara, zoom, escala e iluminación: aprobado;
- misma barra, carga y agarre: aprobado;
- pies, rodillas y torso estables: aprobado;
- hombros sin elevación protagonista: aprobado;
- codos próximos al torso y recorrido progresivo: aprobado;
- muñecas neutrales y movimiento simétrico: aprobado;
- cuerpo, articulaciones, barra y discos sin recortes: aprobado;
- transparencia alfa real: aprobado;
- continuidad visual 1→5: aprobada para revisión de producto.

### Incidencias y correcciones del piloto

- El generador produjo los originales a 1254×1254 px aunque el contrato pedía 1024×1024 px. Se normalizaron el master y los cinco frames a 1024×1024 con remuestreo bicúbico de alta calidad y se conservó el canal alfa.
- En la primera exportación, el extremo izquierdo de la barra de `frame-04.png` llegaba al límite del canvas. Se aplicó el mismo reencuadre uniforme del 88% al master y a toda la secuencia para mantener escala relativa y recuperar margen de seguridad.
- Después del reencuadre, todos los assets conservan las cuatro esquinas transparentes y ningún modelo, articulación, disco o extremo de barra toca el borde.
- No se detectaron cambios que requirieran regenerar una pose completa: identidad, cámara, postura base, carga y recorrido se mantuvieron suficientemente consistentes para la revisión visual del piloto.

### Estado del piloto

El piloto V2 de Curl con barra EZ está integrado únicamente en `Exercise Catalog > Preview > Técnica` para revisión visual. Su registry V2 permanece separado del registry legacy y conserva fallback seguro hacia la demo anterior. Esta validación puntual no autoriza la migración de otros ejercicios.

## Checklist de aceptación para futuros ejercicios

### Frame individual

- [ ] Es el mismo modelo master.
- [ ] Rostro, pelo, piel y ropa permanecen constantes.
- [ ] Equipo, carga y agarre no cambiaron.
- [ ] Cámara, framing, escala e iluminación son idénticos.
- [ ] No hay articulaciones ni equipo recortados.
- [ ] La pose es biomecánicamente correcta.
- [ ] El PNG es 1024×1024 con transparencia real.
- [ ] No hay texto, marcas, fondos, glow ni colores de marca.

### Secuencia

- [ ] El recorrido progresa sin saltos.
- [ ] La trayectoria de la carga es coherente.
- [ ] Los apoyos permanecen estables.
- [ ] El rango de movimiento no está exagerado.
- [ ] Los frames se perciben como una misma grabación.
- [ ] Reduced motion podrá mostrar un frame estático representativo cuando se integre.
