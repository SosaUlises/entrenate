# Entrenate project status

## Visual system

- Acero Térmico V5 is the official visual system.
- Training is the visual source of truth.
- Coach uses Acero Térmico V5 and does not introduce a separate visual identity.

## Brand

- The official wordmark uses Blanco Técnico with a ThermalAccent copper detail.
- Master: `frontend/public/brand/entrenate-wordmark-master.png`.
- The previous violet and blue branding has been retired.

## V5 completed

- Training
- Foundations / Shared UI
- Home
- Bottom Nav
- Routines
- Exercise Catalog
- Progress
- History / Activity
- Profile
- Auth
- Onboarding

## Current work

- Frontend Progress Coach V1 closed.
- Next: return to Entrenate Coach backend work.
- Known external blocker: the currently configured local Groq credential returns
  `401 invalid_api_key`; the frontend and backend contracts remain unchanged.

## Entrenate Coach

Entrenate Coach is the premium intelligent layer of Entrenate.

### Product placement

- Coach is a standalone premium product area inside Entrenate.
- Coach has its own entry point in the primary navigation.
- Coach Hub is the root surface for Coach capabilities.
- Coach follows the existing Acero Térmico V5 visual system.
- Planned capabilities must not expose functional flows before implementation.
- Unavailable capabilities may appear only as clearly disabled / coming-soon entries.

### Coach capabilities

Available:

- Progress Coach

Planned:

- Routine Planner
- Adaptive Training
- Nutrition Coach
- Proactive Coach

### Progress Coach backend

Progress V1.0 / V1.1:

- deterministic progress analysis;
- deterministic recommendation engine;
- authenticated endpoint:
  `GET /api/coach/progress/exercises/{exerciseId}`.

Progress V1.2:

- deterministic explanation added to the progress response.

Progress V1.3:

- structured Application conversation context;
- controlled prompt builder;
- provider-agnostic conversation contract;
- Groq provider implementation;
- authenticated stateless conversation endpoint:
  `POST /api/coach/progress/exercises/{exerciseId}/conversation`;
- real E2E flow validated against PostgreSQL + Groq.

### Coach principles

- Deterministic engines and product rules are the source of truth.
- The LLM explains and converses but does not decide progression.
- LLM output cannot contradict the structured Coach decision.
- Coach must not invent training data.
- Provider-specific infrastructure must remain replaceable.
- Mutations must respect Domain/Application rules.
- Future Coach capabilities must not be implemented prematurely.

### Frontend Progress Coach V1

Closed:

- Coach is available as the fifth primary navigation destination.
- Coach Hub exposes Progress as its active entry.
- Progress Coach exercise selection and deterministic analysis are available.
- Temporary, non-persistent Progress Coach conversation is available.
- Loading, empty, error, retry, insufficient-data and disabled states are covered.
- Routine Planner, Adaptive Training and Nutrition Coach remain disabled as
  coming-soon capabilities.

Next:

- Return to Entrenate Coach backend work.

Coming-soon entries:

- Routine Planner
- Adaptive Training
- Nutrition Coach

## Onboarding

- Welcome completed.
- Primary objective completed.
- Experience completed.
- Weekly training days completed.
- Preferred training days completed.
- Training duration completed.
- Training environment completed.
- Equipment completed.
- Age completed.
- Weight completed.
- Sex completed.
- Onboarding V5 closed.

## Exercise Demo

- The neutral Exercise Demo V2 system is canonical.
- GymBro and legacy demos have been removed.
