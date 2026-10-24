# Entrenate project status

## Visual system

- Acero Térmico V5 is the official visual system.
- Training is the visual source of truth.

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

## Current work

- Onboarding V5

## Entrenate Coach

- Progress V1.0/V1.1 is available through the authenticated backend endpoint
  `GET /api/coach/progress/exercises/{exerciseId}`.
- Progress V1.2 adds a deterministic explanation to that endpoint.
- Progress V1.3 has a structured Application context, controlled prompt builder
  and provider-agnostic contract. Infrastructure includes a Groq Responses API
  adapter and the authenticated stateless conversation endpoint is available at
  `POST /api/coach/progress/exercises/{exerciseId}/conversation`.

### Onboarding

- Welcome is completed.
- Primary objective screen is completed.
- Experience screen is completed.
- Weekly training days screen is completed.
- Preferred training days screen is completed.
- Training duration screen is completed.
- Training environment screen is completed.
- Equipment screen and its visual card pass are completed.
- Sex screen is completed.
- Next: remaining onboarding steps.

## Exercise Demo

- The neutral V2 system is canonical.
- GymBro and legacy demos have been removed.
