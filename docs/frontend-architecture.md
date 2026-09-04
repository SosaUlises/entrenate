# Entrenate — Frontend Architecture

## 1. Purpose

This document defines the initial frontend architecture for Entrenate MVP 1.

The goal is to provide enough structure to keep the project consistent without overengineering it.

---

# 2. Stack

Use:

- Next.js;
- React;
- TypeScript;
- Tailwind CSS.

Use the Next.js App Router.

The application must be mobile-first.

---

# 3. Repository location

Frontend:

```text
entrenate/frontend/
```

Backend:

```text
entrenate/backend/
```

Do not mix frontend source files with backend projects.

---

# 4. Initial project structure

Recommended direction:

```text
frontend/
├── public/
│   └── branding/
│       ├── logo/
│       ├── symbol/
│       ├── icons/
│       └── decorative/
│
├── src/
│   ├── app/
│   │   ├── (auth)/
│   │   ├── (app)/
│   │   ├── layout.tsx
│   │   └── globals.css
│   │
│   ├── components/
│   │   ├── ui/
│   │   └── layout/
│   │
│   ├── features/
│   │   ├── auth/
│   │   ├── profile/
│   │   ├── exercises/
│   │   ├── routines/
│   │   ├── workouts/
│   │   ├── history/
│   │   └── progress/
│   │
│   ├── lib/
│   ├── services/
│   ├── types/
│   └── styles/
│
├── .env.local
├── package.json
├── tsconfig.json
└── ...
```

Create folders only when needed.

---

# 5. App Router

Recommended routing:

```text
src/app/
├── (auth)/
│   ├── login/
│   │   └── page.tsx
│   ├── register/
│   │   └── page.tsx
│   ├── forgot-password/
│   │   └── page.tsx
│   └── reset-password/
│       └── page.tsx
│
├── (app)/
│   └── ...
│
├── layout.tsx
└── globals.css
```

Use route groups to separate auth and authenticated experiences without affecting public URLs.

---

# 6. Feature-based organization

Business-facing frontend behavior belongs under:

```text
src/features/
```

Example auth feature:

```text
src/features/auth/
├── components/
│   ├── login-form.tsx
│   ├── register-form.tsx
│   ├── forgot-password-form.tsx
│   └── reset-password-form.tsx
│
├── services/
│   └── auth.service.ts
│
├── schemas/
│   └── auth.schemas.ts
│
└── types/
    └── auth.types.ts
```

Only create subfolders that are actually needed.

---

# 7. Shared UI components

Reusable visual primitives belong in:

```text
src/components/ui/
```

Initial likely components:

```text
button.tsx
input.tsx
form-field.tsx
password-input.tsx
spinner.tsx
alert.tsx
```

Do not create dozens of primitives in advance.

---

# 8. Layout components

Application-level reusable layout pieces belong in:

```text
src/components/layout/
```

Examples:

```text
auth-shell.tsx
app-header.tsx
mobile-navigation.tsx
desktop-sidebar.tsx
```

Only auth layout is needed for the first milestone.

---

# 9. Server and Client Components

Use Server Components by default.

Use Client Components when required for:

- form interaction;
- local state;
- effects;
- browser APIs;
- timers;
- interactive charts;
- client-side mutations.

Do not mark large page trees with `"use client"` without a real reason.

---

# 10. Forms

Recommended stack:

- React Hook Form;
- Zod;
- `@hookform/resolvers`.

Use schemas for frontend validation.

Keep frontend validation aligned with backend validation.

Frontend validation improves UX but does not replace backend validation.

---

# 11. API layer

Centralize API communication.

Generic infrastructure:

```text
src/services/api-client.ts
```

Feature-specific API logic:

```text
src/features/auth/services/auth.service.ts
```

Responsibilities:

```text
api-client.ts
→ base URL
→ JSON handling
→ shared headers
→ shared response/error handling

auth.service.ts
→ login
→ register
→ forgot password
→ reset password
```

Do not place raw API calls directly inside low-level visual components.

---

# 12. API base URL

Configure through environment variables.

Example:

```env
NEXT_PUBLIC_API_URL=https://localhost:xxxx
```

Do not hardcode development API URLs in components.

---

# 13. API contracts

Before implementing an API call:

1. inspect the real backend endpoint;
2. inspect request DTO;
3. inspect response DTO;
4. inspect expected validation errors;
5. model frontend types accordingly.

Do not infer implemented contracts from the product spec when real backend code exists.

---

# 14. Authentication milestone

Backend authentication already includes:

- register;
- login;
- forgot password;
- reset password.

Frontend implementation order:

1. foundation;
2. Login;
3. Register;
4. Forgot Password;
5. Reset Password.

Implement one screen at a time.

---

# 15. Authentication state

Do not decide token/session storage without inspecting the current ASP.NET Core authentication implementation.

Determine whether the API uses:

- secure cookies;
- bearer tokens;
- Identity API endpoints;
- custom JWT;
- another mechanism.

Do not store authentication tokens in `localStorage` by default.

---

# 16. Validation

Frontend validation:

- immediate feedback;
- better UX;
- prevention of obviously invalid requests.

Backend validation:

- authoritative validation;
- security;
- domain integrity.

Never assume frontend validation is enough.

---

# 17. Error handling

The frontend should distinguish at least:

- validation;
- unauthorized;
- not found;
- conflict;
- server error;
- network error.

Do not expose raw internal backend exception messages.

---

# 18. Loading behavior

For mutations:

- disable relevant submit action;
- show loading state;
- prevent duplicate requests;
- preserve entered values unless success requires reset.

Avoid blocking the whole page for a local form submission.

---

# 19. Tailwind strategy

Use Tailwind as the primary styling system.

Keep brand values in CSS variables.

Use Tailwind utilities for:

- layout;
- spacing;
- typography;
- responsive behavior;
- interaction states.

Keep `globals.css` focused on:

- Tailwind/base imports;
- CSS variables;
- fonts;
- global body rules.

---

# 20. Design tokens

Initial semantic tokens:

```text
background
surface
surface-elevated

primary
primary-strong
secondary

text-primary
text-secondary

success
warning
error
```

Exact Tailwind configuration should follow the installed Tailwind version instead of forcing an outdated pattern.

---

# 21. Class composition

If repeated class composition becomes annoying, use a small utility such as:

- `clsx`;
- `tailwind-merge`.

If both are needed, create a small `cn()` helper.

Do not add a heavy styling abstraction.

---

# 22. Icons

Use one icon family.

Recommended:

**Lucide React**

Reason:

- line-based;
- consistent;
- compatible with the selected visual direction.

Do not mix unrelated icon packs.

---

# 23. Fonts

Use:

- Raleway → brand, headings, metrics;
- Manrope → body, forms, navigation, UI.

Do not use Space Grotesk.

Prefer Next.js font loading mechanisms.

---

# 24. Brand assets

Place assets under:

```text
frontend/public/branding/
```

Do not use generated brand-sheet collages in the UI.
Use individual asset files.

---

# 25. State management

Do not introduce Redux, Zustand or another global state library during initialization without a real need.

Prefer:

- Server Components;
- URL state;
- local state;
- feature hooks;
- framework capabilities.

Introduce global state only when actual product behavior requires it.

---

# 26. Data fetching

Do not add a client caching library by default.

For initial auth flows, ordinary request/mutation logic is enough.

Re-evaluate when authenticated screens require repeated server-state caching and invalidation.

---

# 27. Accessibility

Forms must include:

- visible labels;
- accessible descriptions;
- keyboard navigation;
- focus states;
- understandable errors.

Recommended minimum touch target:

```text
44px
```

Entrenate generally targets:

```text
48px+
```

---

# 28. Mobile-first

Base styles represent phone layouts.

Add responsive behavior as available space increases.

Conceptual strategy:

```text
base → phone
sm/md → larger phone/tablet
lg → desktop
```

Do not design desktop first and undo it for mobile.

---

# 29. Authentication layout

Conceptual structure:

```text
AuthShell
├── Brand
├── Heading
├── SupportingText
├── Form
└── SecondaryAction
```

Desktop can center the same experience in a constrained column.

Do not add a large illustration panel unless the design system is intentionally changed.

---

# 30. Security

Frontend must not contain:

- backend secrets;
- private API keys;
- connection strings;
- email-provider secrets.

Only browser-safe public configuration may use `NEXT_PUBLIC_*`.

---

# 31. Environment files

Do not commit secrets.

Use:

```text
.env.local
```

Optionally add:

```text
.env.example
```

with placeholders only.

---

# 32. Code style

Prefer:

- descriptive names;
- focused components;
- explicit types;
- readable control flow;
- composition.

Avoid:

- huge page components;
- vague utility dumping grounds;
- duplicate API parsing;
- premature generic abstractions.

---

# 33. Dependencies

Initial likely dependencies beyond Next.js:

```text
react-hook-form
zod
@hookform/resolvers
lucide-react
```

Optional only if needed:

```text
clsx
tailwind-merge
```

Avoid large UI libraries initially.

The Entrenate visual system is custom.

---

# 34. First implementation task

The first Codex frontend task should be limited to:

1. initialize `frontend/`;
2. configure Next.js + TypeScript + Tailwind;
3. configure Raleway and Manrope;
4. establish design tokens;
5. configure global dark background;
6. create initial reusable UI primitives required for auth;
7. create auth layout foundation;
8. do not integrate every auth endpoint yet;
9. do not create Dashboard;
10. do not modify backend.

After review, implement Login as the first real screen.

---

# 35. Future architecture

As MVP 1 grows:

```text
features/
├── auth/
├── profile/
├── exercises/
├── routines/
├── workouts/
├── history/
└── progress/
```

The architecture should evolve from real complexity.

Do not create AI, nutrition, social or subscription modules during MVP 1.
