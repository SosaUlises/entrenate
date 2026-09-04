# Entrenate — Codex Instructions

## Project

Entrenate is a mobile-first fitness tracking platform focused on strength and hypertrophy training.

Before implementing or modifying frontend code, read:

- `docs/spec.md`
- `docs/design-system.md`
- `docs/frontend-architecture.md`

These documents are the source of truth for MVP scope, visual design and frontend structure.

## Repository

This is a monorepo.

Expected structure:

```text
entrenate/
├── backend/
├── frontend/
├── docs/
├── AGENTS.md
└── ...
```

Responsibilities:

- `backend/`: ASP.NET Core Web API and domain/application/infrastructure layers.
- `frontend/`: Next.js frontend.
- `docs/`: product, design and technical documentation.

Do not modify `backend/` unless the user explicitly requests backend changes.

## Current project status

Backend authentication endpoints already exist for:

- register;
- login;
- forgot password;
- reset password.

The frontend has not been initialized yet.

The first frontend milestone is the authentication experience.

## Frontend stack

Use:

- Next.js;
- React;
- TypeScript;
- Tailwind CSS.

Use the Next.js App Router.

Keep TypeScript strict.

Do not introduce `any` unless there is a documented technical reason.

## Frontend principles

The frontend must be:

- mobile-first;
- responsive;
- accessible;
- minimal;
- reusable;
- strongly typed;
- easy to evolve.

Prefer clear code over unnecessary abstractions.
Do not add architectural patterns only for theoretical purity.
Do not duplicate UI when a reusable component is appropriate.
Do not create generic abstractions before at least one real use case exists.

## Backend ownership

The backend owns central business rules.

The frontend owns:

- UI;
- navigation;
- visual state;
- forms;
- client-side interaction;
- API consumption;
- timers;
- charts;
- user experience.

Do not move domain rules to React merely for convenience.
Do not invent API contracts.
Inspect the existing backend endpoints, request DTOs and response DTOs before integrating them.

## Design system

All UI must follow `docs/design-system.md`.

Do not introduce new:

- brand colors;
- typography;
- border-radius conventions;
- spacing conventions;
- button styles;
- input styles;
- shadows;
- gradients;
- visual patterns;

without updating the design system first.

## Styling

Use Tailwind CSS.

Prefer design tokens through CSS variables and Tailwind utilities instead of scattering raw HEX values throughout components.
Avoid arbitrary values when an existing token or consistent spacing value can be used.
Keep global CSS minimal.
Use reusable UI primitives for repeated patterns.

## Component philosophy

Prefer this hierarchy:

1. `components/ui/`
   - reusable visual primitives;
2. `components/layout/`
   - app-level layout pieces;
3. `features/<feature>/components/`
   - feature-specific components.

A component should stay inside a feature when it only makes sense for that feature.
Move it to `components/ui/` only when it is genuinely reusable across features.

## Feature organization

Frontend behavior should be grouped by product feature.

Initial features:

- auth;
- profile;
- exercises;
- routines;
- workouts;
- history;
- progress.

The first implemented feature is `auth`.

## API access

Centralize HTTP/API access.
Do not call `fetch` independently from many visual components.
Keep API-specific logic out of presentation components.
Use typed request and response models.
Errors returned by the API must be translated into understandable user-facing messages.
Do not display raw backend exception messages.

## Authentication UX

The authentication experience must follow the minimal visual direction defined in the design system.

Initial screens:

1. Login
2. Register
3. Forgot password
4. Reset password

Do not add Google, Apple, Facebook or other social login buttons unless those authentication methods actually exist in the backend.

## Accessibility

At minimum:

- semantic HTML;
- associated labels for form controls;
- visible keyboard focus;
- sufficient contrast;
- buttons must be actual buttons;
- links must be actual links;
- form errors must be understandable;
- interactive controls must be usable on mobile.

Do not remove focus outlines without replacing them with an accessible focus state.

## Responsive behavior

Design mobile-first.
Authentication screens should be fully usable from small mobile screens.
Do not design desktop first and then shrink it.

The future workout-in-progress experience is the most important mobile UX in the MVP, so foundational components should remain touch-friendly.

## Testing and quality

For each meaningful feature:

- handle loading;
- handle success;
- handle validation errors;
- handle expected API errors;
- avoid duplicate submissions;
- avoid obvious layout shifts.

Add tests where they provide meaningful value, especially around critical logic.
Do not create tests that only reproduce implementation details.

## Scope

Stay inside MVP 1 unless the user explicitly updates the scope.

Do not implement:

- AI;
- machine learning;
- automatic training recommendations;
- nutrition;
- social features;
- subscriptions;
- payments;
- wearables;
- gyms;
- coaches;

unless explicitly requested.

## Working process

Before coding:

1. read the relevant docs;
2. inspect the repository;
3. inspect the existing backend contract when integration is involved;
4. understand the current structure;
5. propose the smallest coherent change.

While coding:

1. preserve existing conventions;
2. keep components focused;
3. reuse existing design primitives;
4. avoid duplicated logic;
5. keep types explicit.

After coding:

1. run the relevant checks;
2. report what changed;
3. mention dependencies added;
4. mention assumptions;
5. mention anything still pending.

## First frontend milestone

When asked to initialize the frontend, create only the foundation first:

- Next.js;
- TypeScript;
- Tailwind CSS;
- global design tokens;
- fonts;
- base layout;
- reusable UI primitives;
- initial feature structure.

Do not implement all product screens in the initialization task.

After the foundation is reviewed, implement authentication screens incrementally.
