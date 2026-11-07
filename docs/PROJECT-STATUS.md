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

- Live Coach V0 — STT vertical slice is closed: in-browser audio capture
  (`getUserMedia`, `MediaRecorder`, codec selection hierarchy, 15s limit), authenticated
  endpoint `POST /api/coach/live/transcriptions`, `ILiveCoachSpeechToText` abstraction,
  `GroqLiveCoachSpeechToTextAdapter` (`whisper-large-v3-turbo`), in-memory transient audio
  processing without persistence, typed error handling, and real transcript display in
  the Training Live Coach sheet.

- Live Coach V0 — TTS vertical slice is closed: authenticated endpoint
  `POST /api/coach/live/speech`, `ILiveCoachTextToSpeech` abstraction,
  `ElevenLabsLiveCoachTextToSpeechAdapter`, strict `responseId` validation against
  ephemeral cache (TTL 60s), zero arbitrary text acceptance from client,
  `audio/mpeg` stream response, voice preference toggle, speaking state animation
  in Acero Térmico V5, audio playback lifecycle management with URL cleanup,
  and robust error recovery where text remains visible if TTS fails.

- Live Coach V0 — contextual text response vertical slice is closed:
  authenticated endpoint `POST /api/coach/live/respond`, authoritative active-session
  projection, `ILiveCoachContextBuilder`, deterministic read-only tools in
  `LiveCoachToolRegistry`, intent routing for queries/mutations/out-of-scope,
  grounded LLM response via shared `ICoachConversationProvider` (Groq),
  ephemeral response cache (`ILiveCoachResponseCache`), non-authoritative
  `clientSnapshot` for drift detection, and full frontend integration in Training
  with live voice-to-text-to-coach response and text fallback.

- Live Coach V0 — visual/UX polish is closed in Training: the shared animated face,
  concise state hierarchy, compact text fallback, playback-synchronized speaking state,
  long-response scrolling, reduced motion and accessible sheet controls are aligned
  with Acero Térmico V5.

- Live Coach V0 — Register Current Set mutation vertical slice is closed:
  deterministic intent/extraction, server-side ephemeral pending actions,
  authoritative current-set revalidation, reuse of Training's real set command,
  single-use protection, cancellation and immediate Training session synchronization.

- Live Coach V0 — voice confirmation of pending actions is closed:
  explicit conservative `sí` / `no` routing resolves only the active server-side
  pending action for the authenticated user and session, shares the tactile
  confirm/cancel executor, rejects ambiguous/stale/expired input without mutation,
  preserves single-use concurrency and synchronizes the authoritative Training
  session after success.

- Live Coach V0 — pending set proposal editing is closed:
  active `RegisterCurrentSet` pending actions can be corrected by voice before
  confirmation, updating only weight, repetitions and RIR in the server-side
  ephemeral payload while preserving `actionId`, TTL, ownership, stale checks
  and single-use semantics.

- Live Coach V0 — rest timer control is closed:
  deterministic `StartRestTimer`, `StopRestTimer` and read-only rest queries operate
  over the existing shared `guidedRest` state, use the prescribed rest duration,
  bypass the conversation provider and never create a second timer.

- Live Coach V0 — auto-rest after set confirmation is closed:
  after a successful `RegisterCurrentSet` confirmation, Training synchronizes the
  authoritative updated session and starts the prescribed rest on the same shared
  timer when applicable, without restarting an already active timer or blocking
  the set mutation.

- Live Coach V0 — Performance Queries V1 is closed:
  deterministic read-only queries expose previous-set performance, previous RIR,
  remaining sets, previous exercise performance, next exercise, remaining exercises
  and structural session progress without provider calls or invented metrics.
  Ambiguous requests such as “best set” or comparisons ask for an explicit criterion
  instead of inventing one.

- Training / Live Coach — Exercise Skip V0 is closed:
  `EjercicioSesion` supports an explicit persisted `Omitido` state, omitted exercises
  are ignored by `CurrentPendingTrainingSetResolver`, completed series are preserved,
  Progress continues to consume only real completed sets, History exposes omitted
  exercises, and Live Coach can propose skipping the current exercise through a
  confirmed server-side pending action.

- Training — omitted exercises can be reactivated while the session remains
  `EnCurso`; reactivation restores the exercise to the authoritative pending resolver
  without altering previously completed sets.

- Frontend Adaptive Training V3 is closed. It configures today's conditions,
  generates and reviews the adapted proposal, presents structured before/after
  changes, supports temporary conversation and explicitly confirms the proposal
  to create the backend-owned active training session.

- Adaptive Training confirmation handles idempotent success, stale proposals,
  existing active-session recovery and direct navigation into Training.

- Coach Hub exposes Adaptive Training through the existing routine/day selection flow;
  Home and routine detail retain their contextual entries.

- Frontend Routine Planner V2 is closed with proposal generation, review,
  temporary conversation and explicit confirmation/persistence.

- The shared Groq conversation provider retries once with a larger output budget
  when the provider reports `incomplete` due to `max_output_tokens`;
  partial answers are never returned as completed responses.

- Next: add Live Coach voice reactivation for omitted exercises and continue with
  the next controlled in-session capabilities.

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
- Live Coach V0

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

### Live Coach backend / Training integration

Live Coach V0 currently includes:

- authenticated STT through Groq Speech-to-Text;
- authenticated TTS through ElevenLabs;
- deterministic and grounded contextual session queries;
- server-side ephemeral pending actions;
- voice and tactile confirmation/cancellation;
- voice correction of pending `RegisterCurrentSet` proposals;
- authoritative set registration through Training commands;
- deterministic rest timer start/stop/query flows;
- automatic prescribed rest after successful set confirmation;
- deterministic performance/session queries;
- explicit exercise omission through Training domain state;
- Training synchronization after Coach mutations;
- stale, expiry, ownership, concurrency and single-use protections.

Current Live Coach mutations must always operate through controlled backend-owned
contracts and authoritative Training state.

### Coach principles

- Deterministic engines and product rules are the source of truth.
- The LLM explains and converses but does not decide progression.
- LLM output cannot contradict the structured Coach decision.
- Coach must not invent training data.
- Provider-specific infrastructure must remain replaceable.
- Mutations must respect Domain/Application rules.
- Live Coach mutations must use explicit deterministic routing and authoritative
  Training state.
- Pending mutations remain server-side, ephemeral and single-use.
- Mutations that can discard or materially alter training state require explicit
  confirmation.
- Client snapshots are advisory for transient state and must not override
  authoritative Training data.
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

- Continue Live Coach V0 and the next controlled Coach capabilities.

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