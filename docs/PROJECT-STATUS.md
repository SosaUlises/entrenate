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

- Live Coach V0 has a documented product/UX foundation and a frontend-only
  visual prototype inside active Training sessions. Its six states are local
  simulations; there is no realtime, audio, persistence or backend contract.
- Live Coach V0 interaction is closed at product/UX level: explicit
  tap-to-start/tap-to-stop activation, immediate cancellation, transient
  microphone use, authorized grounding and failure recovery are normative.
- Live Coach V0 technical architecture is defined as a provider-agnostic,
  read-only HTTP request/response pipeline with typed context, a closed tool
  allowlist, required text fallback and optional TTS.
- Next for Live Coach: close browser/audio limits, provider/privacy, concrete
  API contracts and confidence/retry policies before implementation.
- Frontend Adaptive Training V3 is closed. It configures today's conditions,
  generates and reviews the adapted proposal, presents structured before/after
  changes, supports temporary conversation and explicitly confirms the proposal
  to create the backend-owned active training session.
- Adaptive Training confirmation handles idempotent success, stale proposals,
  existing active-session recovery and direct navigation into Training.
- Coach Hub exposes Adaptive Training through the existing routine/day
  selection flow; Home and routine detail retain their contextual entries.
- Frontend Routine Planner V2 is closed with proposal generation, review,
  temporary conversation and explicit confirmation/persistence.
- Next: continue Entrenate Coach backend or the next requested capability.
- The shared Groq conversation provider retries once with a larger output
  budget when the provider reports `incomplete` due to `max_output_tokens`;
  partial answers are never returned as completed responses.

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
- Routine Planner proposal generation
- Routine Planner confirmation
- Adaptive Training

Planned:

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

### Routine Planner backend

Routine Planner V2.0–V2.4:

- deterministic weekly structure, exercise selection and prescription;
- complete in-memory proposal with canonical hash and content-derived ID.

Routine Planner V2.5a:

- authenticated, stateless proposal endpoint:
  `POST /api/coach/routine-planner/proposals`;
- no draft persistence.

Routine Planner V2.5b:

- authenticated confirmation endpoint:
  `POST /api/coach/routine-planner/proposals/{proposalId}/confirm`;
- backend regeneration and stale-proposal detection;
- transactional activation and idempotent persistence scoped by user.

Routine Planner V2.6:

- authenticated stateless conversation endpoint:
  `POST /api/coach/routine-planner/proposals/{proposalId}/conversation`;
- shared provider output-limit handling applies equally to Routine Planner and
  Progress Coach without changing their API contracts.

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
- Nutrition Coach remains disabled as a coming-soon capability.

Next:

- Return to Entrenate Coach backend work.

Coming-soon entries:

- Nutrition Coach

### Frontend Routine Planner V2

Completed:

- Coach Hub exposes Routine Planner as an available capability.
- The entry screen presents the user's current training-profile context.
- `Generar propuesta` opens a real preview generated through
  `POST /api/coach/routine-planner/proposals`.
- The preview presents feasibility, structure, availability, weekly calendar
  and collapsible sessions with complete exercise prescription.
- Planner warnings and feasibility restrictions are presented in natural
  language without exposing diagnostic codes or internal identifiers.
- Temporary, non-persistent conversation about the current proposal is
  available and handles stale proposals through explicit regeneration.
- Valid proposals can be explicitly confirmed and persisted through the
  backend-owned confirmation contract.
- Confirmation handles idempotent success, stale proposals and navigation to
  the real routine detail; `NotFeasible` proposals remain reviewable but cannot
  be confirmed.

Next:

- Frontend Routine Planner V2 is closed.

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
