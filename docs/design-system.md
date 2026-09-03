# Entrenate — Design System

## 1. Purpose

This document defines the visual system for Entrenate.

It is the source of truth for:

- brand direction;
- color usage;
- typography;
- shape language;
- visual hierarchy;
- authentication screens;
- reusable UI behavior.

The frontend must follow these rules consistently.

---

# 2. Brand personality

Entrenate should feel:

- modern;
- athletic;
- technological;
- premium;
- minimal;
- focused;
- approachable.

Avoid:

- stereotypical gym aesthetics;
- excessive neon;
- gaming aesthetics;
- visual clutter;
- unnecessary decoration;
- excessive photography;
- gradients everywhere;
- extremely aggressive typography;
- overly sharp components.

Core concepts:

**Progress · Discipline · Performance**

---

# 3. Visual direction

Primary direction:

**Dark + minimal + dynamic**

Entrenate uses a dark interface with restrained purple and blue accents.

The interface should rely mainly on neutral dark surfaces.
Purple and blue should guide attention, not dominate the entire screen.

General principle:

> Less visual noise, more focus on the action the user must perform.

---

# 4. Theme

The initial product experience is dark-first.

## Background

`#0C0C12`

## Surface

`#171923`

## Elevated Surface

`#20222E`

---

# 5. Brand colors

## Primary

`#A78BFA`

## Primary Strong

`#8B5CF6`

## Secondary

`#3B82F6`

---

# 6. Neutral colors

## Text Primary

`#F5F5F7`

## Text Secondary

`#A1A1AA`

## Border

`rgba(255, 255, 255, 0.10)`

## Border Strong

`rgba(255, 255, 255, 0.16)`

---

# 7. Brand gradient

```text
#A78BFA → #3B82F6
```

Recommended CSS:

```css
linear-gradient(90deg, #A78BFA 0%, #3B82F6 100%)
```

Use for:

- selected primary CTAs;
- logo assets;
- progress highlights;
- hero moments;
- occasional important states.

Do not use it for every button, border or card.

---

# 8. Semantic colors

## Success
`#22C55E`

## Warning
`#F59E0B`

## Error
`#EF4444`

## Info
`#3B82F6`

Semantic colors are functional and not part of the brand identity.

---

# 9. Typography

Entrenate uses two font families.

## Raleway

Use for:

- Entrenate wordmark;
- page headings;
- important section titles;
- prominent metrics;
- selected display numbers.

Recommended weights:

- 600 SemiBold;
- 700 Bold;
- 800 ExtraBold.

## Manrope

Use for:

- body text;
- navigation;
- buttons;
- form fields;
- labels;
- helper text;
- metadata;
- general UI.

Recommended weights:

- 400 Regular;
- 500 Medium;
- 600 SemiBold.

---

# 10. Suggested type scale

## Display
Raleway 800 — `text-4xl / text-5xl`

## Page title
Raleway 700 — `text-2xl / text-3xl`

## Section title
Raleway 600–700 — `text-lg / text-xl`

## Card title
Raleway 600 — `text-base / text-lg`

## Body
Manrope 400 — `text-sm / text-base`

## UI label
Manrope 500–600 — `text-sm`

## Metadata
Manrope 400–500 — `text-xs / text-sm`

---

# 11. Shape language

Chosen direction:

**Dynamic**

Characteristics:

- rounded;
- modern;
- friendly;
- clean;
- mobile-oriented.

Recommended tokens:

```text
small controls: 12px
inputs/buttons: 16px
cards: 20px
large containers: 24px when justified
```

General visual range: `16–20px`.

Do not turn every element into a pill.

Pills are reserved for:

- compact status;
- filters;
- chips;
- tags;
- segmented controls.

---

# 12. Shadows

Prefer surface contrast and subtle borders over strong shadows.

Recommended elevated shadow:

```css
0 12px 32px rgba(0, 0, 0, 0.24)
```

Do not add glow effects to normal cards.

---

# 13. Buttons

## Primary

- minimum recommended height: 48px;
- rounded;
- Manrope SemiBold;
- strong hierarchy.

Use either:

- solid `Primary Strong`;
- brand gradient for selected high-priority CTA moments.

## Secondary

- dark/neutral surface;
- subtle border;
- primary text.

## Text action

Use violet for important textual actions such as:

- Forgot password;
- Register;
- Back;
- View details.

---

# 14. Inputs

Inputs should be:

- mobile-friendly;
- clearly labeled;
- rounded;
- dark;
- subtly bordered.

Recommended height: `48–52px`
Recommended radius: `16px`

States:

- default;
- hover;
- focus;
- error;
- disabled.

Focus should use the brand violet.

Do not use placeholder text as the only label.

---

# 15. Cards

Recommended:

```text
background: Surface / Elevated Surface
radius: 20px
border: 1px solid rgba(255,255,255,0.08)
padding: 16–24px
```

Avoid unnecessary card nesting.

---

# 16. Iconography

Direction:

**Line icons**

Characteristics:

- simple;
- rounded;
- visually consistent;
- approximately 2px stroke.

Use one icon family consistently.

---

# 17. Spacing

Preferred rhythm:

```text
4px
8px
12px
16px
20px
24px
32px
40px
48px
64px
```

Prefer generous breathing room over dense layouts.

---

# 18. Layout principles

Entrenate is mobile-first.

Prioritize:

- one clear primary action per screen;
- readable content;
- thumb-friendly controls;
- minimal horizontal crowding;
- consistent vertical rhythm.

Do not compress a desktop dashboard into mobile.

---

# 19. Authentication visual direction

Chosen direction:

**Minimal and clean**

Use:

- dark background;
- balanced/centered form;
- generous empty space;
- visible logo;
- clear title;
- concise supporting copy;
- rounded inputs;
- one prominent primary CTA;
- restrained decoration.

Avoid:

- gym photography;
- visual clutter;
- large marketing sections;
- social-login buttons that are not actually implemented.

---

# 20. Login screen

Recommended hierarchy:

1. Entrenate logo;
2. "Iniciar sesión";
3. supporting message;
4. email;
5. password;
6. forgot-password action;
7. primary login CTA;
8. register link.

Suggested supporting copy:

```text
Continuá tu progreso.
```

---

# 21. Register screen

Recommended hierarchy:

1. back navigation when appropriate;
2. logo;
3. "Creá tu cuenta";
4. concise supporting copy;
5. first name;
6. last name;
7. email;
8. password;
9. confirm password if required by UX;
10. password requirements;
11. create-account CTA;
12. login link.

Do not request sports-profile information during registration.

Sports profile setup belongs to onboarding.

---

# 22. Forgot password

Recommended hierarchy:

1. logo;
2. title;
3. brief explanation;
4. email;
5. send/reset CTA;
6. back to login.

Show a clear confirmation state after success.

---

# 23. Reset password

Recommended hierarchy:

1. logo;
2. title;
3. new password;
4. confirm password;
5. password requirements;
6. save CTA;
7. return to login after success.

---

# 24. Feedback states

Every async action must support:

- idle;
- loading;
- success;
- expected error.

Prefer inline validation when possible.

Use toasts only when the message does not naturally belong next to an element.

---

# 25. Motion

Recommended duration:

```text
150–250ms
```

Use subtle, functional motion only.

Avoid:

- bouncing;
- constant glowing;
- decorative loops.

---

# 26. Tailwind implementation

Expose brand values as CSS variables.

Example:

```css
:root {
  --background: #0C0C12;
  --surface: #171923;
  --surface-elevated: #20222E;

  --primary: #A78BFA;
  --primary-strong: #8B5CF6;
  --secondary: #3B82F6;

  --text-primary: #F5F5F7;
  --text-secondary: #A1A1AA;

  --success: #22C55E;
  --warning: #F59E0B;
  --error: #EF4444;
}
```

Prefer semantic tokens over repeated raw values such as `bg-[#171923]`.

---

# 27. Brand assets

Recommended location:

```text
frontend/public/branding/
```

Suggested structure:

```text
branding/
├── logo/
├── symbol/
├── icons/
└── decorative/
```

Do not stretch, distort or arbitrarily recolor the Entrenate symbol.

---

# 28. Design decision rule

When multiple solutions are visually valid, prefer the one that is:

1. simpler;
2. clearer;
3. easier to use on mobile;
4. more consistent with existing components;
5. less visually noisy.

Entrenate should feel premium through restraint, not through decoration.
