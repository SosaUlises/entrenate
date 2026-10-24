# Entrenate Coach

## Vision

Entrenate Coach is the intelligent layer of Entrenate.

It is not a generic chatbot, mascot or animated character. It uses real
training data to help users make better training decisions.

## Core principle

Data from Entrenate
→ deterministic engines / product rules
→ structured decision
→ Coach
→ explanation / natural-language interaction

The LLM is not the source of truth for training decisions.

## Identity

- Serious
- Premium
- Dark-first
- Minimal
- Performance-oriented
- No GymBro
- No animated mascot
- No anime identity

## Versions

### V0 — Foundation

Shared context, structured decisions and safe mutation boundaries.

### V1 — Progress Coach

Analyze training history and produce deterministic progression
recommendations.

Detailed specification:

`docs/coach/v1-progress-coach.md`

### V2 — Routine Planner

Generate and modify training plans from structured constraints.

Detailed specification:

`docs/coach/v2-routine-planner.md`

### V3 — Adaptive Training

Adapt training according to time, equipment, fatigue and preferences.

Detailed specification:

`docs/coach/v3-adaptive-training.md`

### V3.1 — Discomfort & Restrictions

Conservative adaptations when users report discomfort or limitations.

Detailed specification:

`docs/coach/v3-1-discomfort-restrictions.md`

### V4 — Nutrition Coach

General nutrition guidance related to training goals.

Detailed specification:

`docs/coach/v4-nutrition-coach.md`

### V5 — Proactive Coach

Surface contextual insights without requiring the user to start a conversation.

Detailed specification:

`docs/coach/v5-proactive-coach.md`

## Global constraints

- Do not bypass Domain/Application rules.
- Do not let LLM output write directly to the database.
- Mutations require preview and explicit confirmation.
- Do not expose capabilities before they are implemented.
- Do not implement later Coach versions while working on an earlier one unless
  an explicit prerequisite requires it.

## Current priority

V1 — Progress Coach.

Read:

`docs/coach/v1-progress-coach.md`