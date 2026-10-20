---
name: Entrenate — Acero Térmico
description: Instrumento deportivo digital mobile-first, preciso, sobrio y legible durante el entrenamiento.
colors:
  background: "#10110f"
  surface: "#181a17"
  surface-elevated: "#22251f"
  surface-interactive: "#1d1f1b"
  surface-active: "#292c25"
  technical-white: "#e8e7df"
  primary: "#e8e7df"
  primary-foreground: "#151612"
  primary-pressed: "#cbcac1"
  thermal-accent: "#e1734e"
  steel: "#7fa4ad"
  text-primary: "#f2f1ea"
  text-secondary: "#b2b2a9"
  text-muted: "#7e8178"
  border: "rgba(242, 241, 234, 0.12)"
  border-strong: "rgba(242, 241, 234, 0.20)"
  focus: "#e1734e"
  success: "#67b77a"
  warning: "#d8ae55"
  error: "#d85d56"
  info: "#7fa4ad"
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
    lineHeight: 1.35
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
    lineHeight: 1.5
    letterSpacing: "0.04em"
rounded:
  small: "8px"
  medium: "12px"
  large: "16px"
  overlay: "18px"
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
    backgroundColor: "{colors.primary}"
    textColor: "{colors.primary-foreground}"
    typography: "{typography.label}"
    rounded: "{rounded.medium}"
    padding: "12px 20px"
    height: "48px"
  button-secondary:
    backgroundColor: "{colors.surface-elevated}"
    textColor: "{colors.text-primary}"
    typography: "{typography.label}"
    rounded: "{rounded.medium}"
    padding: "12px 20px"
    height: "48px"
  input:
    backgroundColor: "{colors.surface-interactive}"
    textColor: "{colors.text-primary}"
    typography: "{typography.body}"
    rounded: "{rounded.medium}"
    padding: "0 16px"
    height: "56px"
  chip-active:
    backgroundColor: "{colors.surface-active}"
    textColor: "{colors.text-primary}"
    typography: "{typography.label}"
    rounded: "{rounded.full}"
    padding: "0 16px"
    height: "44px"
  card-functional:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.text-primary}"
    rounded: "{rounded.large}"
    padding: "16px"
  nav-floating:
    backgroundColor: "{colors.surface-elevated}"
    textColor: "{colors.text-secondary}"
    rounded: "{rounded.overlay}"
    padding: "4px"
---

# Design System: Entrenate — Acero Térmico

## Creative North Star

**Precisión en movimiento.** Entrenate se comporta como un instrumento deportivo digital: premium, atlético, tecnológico, preciso y sobrio. Debe ser directo, rápido, accesible y altamente legible durante el entrenamiento.

La tecnología se expresa mediante jerarquía, datos, motion funcional y feedback. La materialidad combina carbón, grafito, acero, goma y una señal térmica contenida. La identidad no depende de mascotas, decoración futurista ni superficies innecesarias.

## Color semantics

- **Background `#10110F`:** lienzo carbón continuo.
- **Surface `#181A17`:** agrupación funcional plana.
- **Surface Elevated `#22251F`:** overlays y capas reales.
- **Surface Interactive `#1D1F1B`:** controles editables o accionables.
- **Surface Active `#292C25`:** respuesta contenida a interacción o selección.
- **Technical White / Primary `#E8E7DF`:** acción cerámica y datos de máxima jerarquía.
- **Primary Foreground `#151612`:** contenido sobre la acción primaria.
- **Thermal Accent `#E1734E`:** señal breve de foco, avance o performance.
- **Steel / Info `#7FA4AD`:** información y contexto secundario.
- **Text Primary `#F2F1EA`, Secondary `#B2B2A9`, Muted `#7E8178`:** jerarquía de lectura.
- **Success `#67B77A`, Warning `#D8AE55`, Error `#D85D56`:** estados semánticos exclusivos.

`primary` is the solid ceramic action treatment, not a universal brand color. `thermalAccent` never replaces success, warning, error, or info and never fills large surfaces.

Do not use gradients, halos, glow, inherited violet/blue tints, or color to artificially elevate a composition.

## Typography and data

Raleway expresses metrics, timers, highlighted numbers, and entity titles when it adds identity. Manrope supports body, labels, controls, descriptions, navigation, and metadata.

Functional text starts at 13px. Metadata uses Manrope 13px/600 with moderate tracking. Timers, weights, repetitions, positions, and comparable metrics use tabular numerals. Units are secondary and aligned with their value.

## Surfaces and elevation

The interface is flat by default. Space, typographic hierarchy, dividers, borders, and minimal surface changes separate content. A surface must communicate selection, state, interactive grouping, or a real layer.

The elevated shadow `0 12px 28px rgba(0,0,0,.28)` is reserved for Bottom Nav, menus, dialogs, sheets, popovers, and tooltips. There is no glassmorphism or ornamental blur.

## Geometry

- 8px for small controls;
- 12px for buttons, inputs, and main controls;
- 16px for functional groups;
- 18px maximum for overlays;
- pills only for chips, filters, and compact controls whose semantics justify them.

Touch targets measure at least 44–48px. Borders are technical, thin, and discreet.

## Components

- **Primary button:** solid technical white, carbon text, 48px minimum, 12px radius.
- **Secondary button:** elevated surface, strong border, primary text.
- **Ghost button:** neutral until interaction.
- **Destructive button:** semantic error, never thermal signal.
- **Inputs:** `surfaceInteractive`, fine border, technical text, muted placeholder, visible focus line, no halo.
- **Selection controls:** contrast, weight, underline, marker, or border before accent fills.
- **Navigation:** elevated mineral surface, neutral icons, 13px labels, and a short thermal active marker.
- **Overlays:** `surfaceElevated`, fine border, firm radius, contained shadow, neutral separators.
- **Status:** info uses steel; success, warning, and error retain their own meaning.

## Motion

The standard range is 150–220ms with contained easing. Motion communicates fitting, confirming, advancing, or saving. Do not use bounce, overshoot, playful springs, animated glow, or decorative loops. Respect `prefers-reduced-motion`.

## Product constraints

Validate mobile first at 390×667, then 390×844 and desktop. Keep one clear primary action, large targets, fast reading, and one operational column. Avoid generic dashboards, nested cards, gamer/cyberpunk aesthetics, decorative SaaS patterns, and recurring mascots.

Training is the visual reference. Other screens migrate by feature without redefining local foundations.
