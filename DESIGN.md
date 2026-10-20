---
name: Entrenate
description: Interfaz mobile-first para entrenar fuerza e hipertrofia con precisión, continuidad y mínima fricción.
colors:
  fondo-carbon: "#0c0c12"
  superficie-grafito: "#171923"
  superficie-grafito-elevada: "#20222e"
  violeta-impulso: "#a78bfa"
  violeta-impulso-fuerte: "#8b5cf6"
  azul-rendimiento: "#3b82f6"
  blanco-tecnico: "#f5f5f7"
  gris-informacion: "#a1a1aa"
  borde-sutil: "rgba(255, 255, 255, 0.10)"
  borde-fuerte: "rgba(255, 255, 255, 0.16)"
  exito: "#22c55e"
  advertencia: "#f59e0b"
  error: "#ef4444"
typography:
  display:
    fontFamily: "Raleway, system-ui, sans-serif"
    fontSize: "2.25rem"
    fontWeight: 800
    lineHeight: 1.05
    letterSpacing: "normal"
  headline:
    fontFamily: "Raleway, system-ui, sans-serif"
    fontSize: "2rem"
    fontWeight: 700
    lineHeight: 1.08
    letterSpacing: "normal"
  title:
    fontFamily: "Raleway, system-ui, sans-serif"
    fontSize: "1.25rem"
    fontWeight: 700
    lineHeight: 1.4
    letterSpacing: "normal"
  body:
    fontFamily: "Manrope, system-ui, sans-serif"
    fontSize: "0.9375rem"
    fontWeight: 400
    lineHeight: 1.6
    letterSpacing: "normal"
  label:
    fontFamily: "Manrope, system-ui, sans-serif"
    fontSize: "0.875rem"
    fontWeight: 600
    lineHeight: 1.25
    letterSpacing: "normal"
  metadata:
    fontFamily: "Manrope, system-ui, sans-serif"
    fontSize: "0.8125rem"
    fontWeight: 600
    lineHeight: 1.333
    letterSpacing: "0.1em"
rounded:
  control-sm: "12px"
  control: "16px"
  card: "20px"
  container: "24px"
  full: "9999px"
spacing:
  1: "4px"
  2: "8px"
  3: "12px"
  4: "16px"
  5: "20px"
  6: "24px"
  8: "32px"
  10: "40px"
  12: "48px"
  16: "64px"
components:
  button-primary:
    backgroundColor: "{colors.violeta-impulso-fuerte}"
    textColor: "{colors.blanco-tecnico}"
    typography: "{typography.label}"
    rounded: "{rounded.control}"
    padding: "12px 20px"
    height: "48px"
  button-secondary:
    backgroundColor: "{colors.superficie-grafito-elevada}"
    textColor: "{colors.blanco-tecnico}"
    typography: "{typography.label}"
    rounded: "{rounded.control}"
    padding: "12px 20px"
    height: "48px"
  input:
    backgroundColor: "{colors.superficie-grafito-elevada}"
    textColor: "{colors.blanco-tecnico}"
    typography: "{typography.body}"
    rounded: "{rounded.control}"
    padding: "0 16px"
    height: "56px"
  chip-active:
    backgroundColor: "{colors.superficie-grafito}"
    textColor: "{colors.violeta-impulso}"
    typography: "{typography.label}"
    rounded: "{rounded.full}"
    padding: "0 16px"
    height: "44px"
  card-functional:
    backgroundColor: "{colors.superficie-grafito}"
    textColor: "{colors.blanco-tecnico}"
    rounded: "{rounded.card}"
    padding: "16px"
  nav-floating:
    backgroundColor: "{colors.superficie-grafito-elevada}"
    textColor: "{colors.gris-informacion}"
    rounded: "{rounded.full}"
    padding: "4px"
---

# Design System: Entrenate

## Overview

**Creative North Star: "Precisión en movimiento"**

Entrenate expresa rendimiento mediante una interfaz premium, atlética, tecnológica, precisa y sobria. Está diseñada para responder rápido y mantener la lectura clara durante el entrenamiento: la jerarquía señala la próxima acción, el feedback confirma el avance y el movimiento acompaña los cambios de estado sin distraer.

La tecnología se comunica mediante tipografía, ritmo, motion funcional, datos legibles y respuestas inmediatas. La personalidad nace del sistema visual y de la continuidad entre rutina, sesión, historial, actividad y progreso; no depende de decoración futurista ni de recursos narrativos recurrentes.

La interfaz debe resultar simple para quien empieza, sólida para quien entrena hace años y cómoda para adultos mayores. La legibilidad, los targets grandes y la baja carga visual tienen prioridad sobre la densidad ornamental.

**Key Characteristics:**

- Dark-first, mobile-first y orientada al uso real en gimnasio.
- Jerarquía tipográfica fuerte con una acción primaria evidente por estado.
- Violeta para acción y foco; azul para información y evolución.
- Superficies contenidas, pocas capas y poco ruido.
- Motion breve y funcional que guía sin interrumpir.
- Targets táctiles de 44–48 px como mínimo.

**The Precision in Motion Rule.** Cada transición, cambio de color o variación de escala debe explicar una acción, confirmar un resultado o facilitar la orientación.

## Colors

La paleta combina un lienzo carbón con superficies grafito y acentos fríos de uso restringido. Los tokens del frontmatter son la fuente normativa.

### Primary

- **Violeta Impulso:** acción, foco, selección y progreso activo.
- **Violeta Impulso Fuerte:** CTA principal y estados activos que necesitan mayor contraste.

### Secondary

- **Azul Rendimiento:** información, evolución, datos y estados secundarios. No reemplaza al violeta como color de acción principal.

### Neutral

- **Fondo Carbón:** lienzo principal continuo.
- **Superficie Grafito:** agrupación funcional cuando el contenido necesita separación semántica.
- **Superficie Grafito Elevada:** elementos superpuestos y controles que necesitan mayor separación del fondo.
- **Blanco Técnico:** títulos, contenido primario y métricas principales.
- **Gris Información:** metadata, supporting copy y contenido secundario.
- **Bordes sutil y fuerte:** definición de límites sin construir cajas pesadas.

### Semantic

- **Éxito:** confirmaciones y estados completos.
- **Advertencia:** atención sin error bloqueante.
- **Error:** fallos, validación y acciones destructivas.

**The Controlled Accent Rule.** El violeta y el azul guían la atención; no rellenan la interfaz ni compiten simultáneamente por protagonismo.

**The Semantic Surface Rule.** Una superficie grafito debe agrupar contenido relacionado o comunicar una capa real. No se usa para convertir cada sección en una card.

## Typography

Raleway aporta identidad deportiva y precisión a títulos, métricas y números relevantes. Manrope sostiene la lectura operativa en cuerpos, labels, navegación, formularios y metadata.

- **Display:** Raleway ExtraBold para cifras protagonistas y momentos de alto énfasis.
- **Headline:** Raleway Bold para títulos de pantalla y estados principales.
- **Title:** Raleway Bold para secciones, ejercicios y entidades.
- **Body:** Manrope Regular para explicaciones breves y contenido continuo.
- **Label:** Manrope SemiBold para controles y acciones.
- **Metadata:** Manrope SemiBold, frecuentemente en mayúsculas y con tracking amplio, para contexto breve.

Usar números tabulares en timers, pesos, repeticiones, posiciones y métricas comparables. El texto funcional y la metadata parten de 13 px; solo elementos estrictamente decorativos o excepcionales, innecesarios para comprender la interfaz, pueden quedar por debajo de ese tamaño. Conservar un line-height cómodo cuando la interfaz se usa en movimiento.

**The Operational Legibility Rule.** Cuanto más importante sea un dato durante una serie, mayor debe ser su contraste y menor la distancia visual respecto del control que modifica.

## Layout

El sistema es mobile-first. Las pantallas autenticadas usan un contenedor centrado con 16 px de padding horizontal en mobile y un ancho máximo compacto; el App Shell actual limita el contenido general a 672 px. Las superficies de trabajo más enfocadas suelen limitarse a 576 px. En `sm` (640 px), el padding lateral aumenta y el contenido conserva una columna única.

El ritmo espacial se construye con pasos de 4 px y combinaciones frecuentes de 8, 12, 16, 20, 24 y 32 px. Las secciones se separan con espacio antes que con contenedores. El contenido principal reserva espacio inferior para navegación flotante y safe area; Training elimina esa navegación para priorizar la operación.

No debe existir scroll horizontal en el viewport. Los únicos desbordes horizontales intencionales son regiones acotadas como filtros o calendarios anuales, con gesto y foco propios.

**The One Operational Column Rule.** En flujos de entrenamiento, formularios y detalle, conservar una columna principal clara; desktop amplía el aire, no transforma la experiencia en un dashboard ancho.

**The Thumb-Reach Rule.** Los controles frecuentes deben mantener targets de 44–48 px y permanecer separados de acciones destructivas.

## Elevation & Depth

La interfaz es plana por defecto. Espacio, tipografía, divisores, bordes sutiles y cambios mínimos de superficie crean la mayor parte de la profundidad. La elevación se reserva para Bottom Nav, menús, diálogos, bottom sheets y tooltips.

### Shadow Vocabulary

- **Elevated:** sombra ambiental contenida para comunicar una capa superpuesta. No se aplica a cards comunes ni a filas.

El blur puede acompañar una navegación flotante o un overlay cuando mejora la separación, siempre con baja opacidad. No usar glow.

**The Flat-by-Default Rule.** Si un elemento no se superpone físicamente a otro, debe resolver su jerarquía sin sombra.

**The Real Layers Rule.** La sombra comunica una capa interactiva real; nunca sustituye una jerarquía de contenido débil.

## Shapes

La forma es redondeada, moderna y táctil. Los controles pequeños usan 12 px, inputs y botones 16 px, cards funcionales 20 px y contenedores o sheets 24 px. La forma pill se reserva para chips, segmented controls, navegación flotante y estados compactos.

Los bordes son finos y de bajo contraste. Los cambios de borde, superficie y color indican hover, focus, selección o error. Los medios visuales deben recortarse con el radio del control que los contiene, sin deformar su proporción.

**The Purposeful Radius Rule.** El radio expresa escala y función; no convierte todas las superficies en pills ni suaviza divisores que funcionan mejor como líneas.

## Components

Los componentes se sienten directos, firmes y contenidos. Cuanto más frecuente y operacional sea una acción durante el entrenamiento, menos decorativa debe ser su interfaz.

### Buttons

- **Primary:** altura mínima de 48 px, fondo Violeta Impulso Fuerte, texto Blanco Técnico y peso semibold. Se reserva para la acción principal del estado.
- **Secondary:** superficie elevada, borde sutil y texto primario. Mantiene jerarquía claramente inferior al CTA.
- **Text:** acción sin contenedor, usada para navegación secundaria o comandos discretos.
- **States:** hover ajusta superficie o brillo; active puede reducir la escala a 0.99; focus-visible usa un outline violeta claro. Loading conserva el label y bloquea duplicados.

El gradiente violeta a azul es un tratamiento excepcional, no un primitive ni una alternativa equivalente a Primary. Solo puede aparecer en un momento puntual de alto énfasis cuando la composición lo justifica; nunca se agrega para hacer que una pantalla se sienta más premium.

**The Solid Primary Rule.** La acción primaria habitual usa Violeta Impulso Fuerte sólido. El gradiente no es una solución visual por defecto.

### Inputs

- Altura base de 56 px, radio de 16 px, superficie grafito elevada y borde sutil.
- El label siempre existe fuera o dentro de una composición semántica clara; el placeholder no reemplaza al label.
- Focus combina borde y ring violeta. Error cambia a rojo y mantiene mensaje textual.
- Los inputs numéricos operativos usan Raleway, cifras grandes y números tabulares cuando mejora la carga durante Training.

### Chips and Segmented Controls

- Forma pill, target mínimo de 44 px y padding horizontal compacto.
- El estado activo combina tint violeta con texto violeta y `aria-pressed` cuando corresponde.
- El estado no seleccionado conserva contraste legible y no parece deshabilitado.

### Rows and Lists

- Las filas son el patrón preferido para rutinas, ejercicios, historial y opciones frecuentes.
- Usan separación vertical, divisores sutiles y una superficie solo cuando necesitan comunicar expansión o selección.
- Toda el área útil navega o abre detalle; acciones internas independientes detienen la propagación.

### Cards and Surfaces

- Una card agrupa información con relación funcional clara; no es el envoltorio predeterminado de cada bloque.
- Usa radio de 20 px, padding habitual de 16–24 px y bordes de bajo contraste.
- Evitar card dentro de card. Para subestructura, preferir filas, espacio y divisores.

### Navigation

- La navegación principal es una pill flotante compacta con cuatro destinos, iconos lineales y labels visibles.
- El destino activo usa un tint violeta suave; los inactivos mantienen legibilidad.
- Respeta safe area y se eleva sobre el contenido con blur y sombra contenidos.

### Dialogs and Bottom Sheets

- En mobile aparecen como bottom sheets casi full-width; en desktop se centran con ancho limitado.
- Incluyen overlay oscuro, superficie elevada, borde sutil, título accesible y cierre por control explícito, overlay y Escape cuando corresponda.
- La elevación y el radio de 24 px comunican la capa; el contenido interno permanece plano.

### Feedback and Motion

- Las transiciones habituales duran 150–250 ms. Cambios de opacidad, color y escala deben ser breves y funcionales.
- Respetar `prefers-reduced-motion`; ninguna información depende de animación continua.
- Los estados idle, loading, success y error mantienen la composición estable y usan texto comprensible además del color.

**The Direct Action Rule.** Una acción frecuente durante el entrenamiento debe requerir el mínimo de lectura, desplazamiento y decoración compatible con seguridad y accesibilidad.

## Do's and Don'ts

### Do

- Do usar una acción primaria evidente por estado.
- Do priorizar lectura rápida, targets amplios y operación con una mano.
- Do separar secciones con espacio, tipografía y divisores antes de agregar superficies.
- Do usar Violeta Impulso para acción, foco, selección y progreso activo.
- Do usar Azul Rendimiento para información, evolución y estados secundarios.
- Do acompañar el color con texto, iconografía o forma para comunicar estados.
- Do conservar español natural y rioplatense en el producto.
- Do usar figuras humanas neutrales y sin identidad reconocible en demostraciones técnicas.

### Don't

- Don't usar cyberpunk, gaming, neon fuerte o decoración futurista.
- Don't convertir Entrenate en un dashboard de analytics.
- Don't usar glassmorphism excesivo, glow o sombras en superficies planas.
- Don't anidar cards ni encerrar cada sección en una caja.
- Don't basar la identidad en una figura narrativa recurrente.
- Don't llenar la interfaz de violeta o azul; los acentos deben conservar significado.
- Don't usar el gradiente violeta a azul como CTA habitual ni como decoración para elevar artificialmente una pantalla.
- Don't reducir labels, metadata o targets hasta comprometer su uso durante el entrenamiento.
- Don't inventar diferencias de desktop que rompan la continuidad mobile-first.
