# Entrenate — Design System

## Source of truth

The official visual system is **Acero Térmico**. Its canonical design specification is [`DESIGN.md`](../DESIGN.md). This file records the frontend implementation contract and does not redefine the visual language.

Training is the reference implementation. New product surfaces inherit shared tokens and primitives before adding feature-specific treatment.

## Foundation contract

The CSS variables in `frontend/src/app/globals.css` implement the canonical tokens:

- `background`, `surface`, `surfaceElevated`, `surfaceInteractive`, `surfaceActive`;
- `technicalWhite`, `primary`, `primaryForeground`, `primaryPressed`;
- `thermalAccent`, `steel`;
- `textPrimary`, `textSecondary`, `textMuted`;
- `border`, `borderStrong`, `focus`;
- `success`, `warning`, `error`, `info`.

`primary` supplies the ceramic action color. The shared primary button blends it with elevated graphite to control brightness; it is not a brand accent. `thermalAccent` is a small interaction or performance signal. `steel` and `info` communicate neutral information. Semantic colors retain their own meanings.

A temporary `secondary` alias exists only for feature screens awaiting scoped migration. Do not use it in new shared components.

## Shared component contract

- Primary buttons use a controlled ceramic blend of technical white and elevated graphite, with carbon text and a technical border.
- Secondary buttons use an elevated mineral surface and technical border.
- Outline buttons use a transparent surface, strong technical border and primary text.
- Ghost actions stay neutral until interaction.
- Destructive actions use `error`, never `thermalAccent`.
- Shared button sizes are `sm` (44px), `md` (48px) and `lg` (52px), all with 12px radius and consistent icon spacing.
- Inputs use `surfaceInteractive`, a fine border, visible focus, and no halo or glow.
- Selected chips, filters, tabs, and segmented controls use contrast, weight, underline, marker, or border before accent fills.
- Navigation uses mineral surfaces, neutral icons, readable 13px labels, and a short thermal marker for the active destination.
- Dialogs, sheets, popovers, and menus use `surfaceElevated`, a fine border, firm radii, and the elevated shadow only because they overlap content.
- Alerts use `info`/steel, `success`, `warning`, and `error` according to meaning.

## Typography and data

- Raleway: important metrics, timers, highlighted numbers, and entity titles when identity helps.
- Manrope: body text, labels, controls, descriptions, navigation, and metadata.
- Functional text must be at least 13px.
- Comparable numbers use tabular numerals. Units remain secondary and aligned with their value.

## Geometry and motion

- small radius: 8px;
- medium/control radius: 12px;
- large/group radius: 16px;
- overlay radius: up to 18px;
- touch targets: 44–48px minimum;
- standard motion: 150–220ms;
- reduced motion must be respected.

Pills are reserved for controls whose semantics require them. Shadows are reserved for real overlay layers. Gradients, glow, halos, bounce, and decorative looping motion are not part of Acero Térmico.

## Implementation rules

Use semantic Tailwind utilities backed by CSS variables. Avoid raw colors in components. A feature may introduce a local composition, but it must not redefine the foundation palette.

Before shipping a shared primitive, verify contrast, focus visibility, disabled/loading/active/error states, 390×667 and 390×844 behavior, horizontal overflow, and that state never depends on color alone.

Auth and Onboarding keep their current layout and behavior until their dedicated migration. They inherit the global palette and shared primitives without legacy radial halos.
