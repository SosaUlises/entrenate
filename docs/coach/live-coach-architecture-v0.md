# Live Coach V0 technical architecture

## 1. Purpose and status

This document defines the technical boundaries and provider-agnostic contracts
for Live Coach V0. It is an architecture specification, not an implementation.

V0 remains read-only. It does not add STT, TTS, realtime transport, provider
integrations, tool implementations, audio persistence or backend endpoints yet.

The interaction and privacy behavior in
`docs/coach/live-coach-v0.md` is normative. This architecture must preserve it.

## 2. Architectural principles

- Training remains usable without Live Coach.
- Existing deterministic engines and persisted Training state are the source of
  truth.
- The LLM explains authorized data; it does not reconstruct session state.
- Presentation owns capture/playback and visual state, not training rules.
- Application owns authorization, context projection, routing and tool policy.
- Infrastructure owns external STT, TTS and Coach provider adapters.
- API remains a thin authenticated boundary.
- Providers never receive database access or control Training.
- Every operation is cancelable and scoped to one authenticated interaction.
- No V0 component writes training state or persists conversation/audio.

## 3. Conceptual pipeline

```text
Training
  -> Live Coach UI state machine
  -> explicit audio capture OR text input
  -> STT abstraction (audio only)
  -> authorized session-context projector
  -> intent router
  -> read-only tool allowlist (when required)
  -> safe Coach prompt/context projection
  -> Coach conversation provider
  -> grounded text response
  -> visible text output
  -> optional TTS abstraction and audio playback
```

The frontend never calls STT, TTS or the Coach provider directly. A future
authenticated API operation coordinates the server-side pipeline and propagates
cancellation. Text input enters after the audio/STT stage and uses the same
context, routing and response path.

TTS is downstream from the completed grounded text response. A TTS failure
therefore cannot erase a valid text answer.

## 4. Responsibilities by layer

### Training presentation

- owns the Live Coach state machine;
- requests microphone permission only after explicit activation;
- captures transient audio or accepts text fallback;
- sends one interaction at a time;
- displays transcript correction only when the eventual confidence policy
  requires it;
- renders the grounded text answer;
- requests and plays optional TTS output;
- stops capture/playback and ignores late results after cancellation;
- preserves unsaved Training fields independently from Live Coach.

Presentation cannot derive session facts, select tools, execute mutations or
call provider-specific SDKs.

### API boundary

- authenticates the current user;
- validates request size, media type, locale and identifiers;
- forwards request cancellation;
- rejects sessions that do not belong to the authenticated user;
- maps typed Application failures to stable, non-provider-specific responses;
- never returns provider credentials or raw provider errors.

### Application

- verifies the active session and the client cursor;
- builds the minimum authorized context snapshot;
- classifies intent into a closed enum;
- validates requested tools against the V0 allowlist;
- executes read-only queries through existing repositories/services;
- creates a safe provider projection without internal identifiers;
- enforces scope, grounding, prompt-injection protection and response policy;
- orchestrates provider abstractions with one cancellation token.

### Domain and deterministic engines

Domain entities and existing deterministic engines remain authoritative for
session state, prescriptions and Coach decisions. Live Coach does not add a
parallel training model and cannot override their results.

### Infrastructure

- implements provider-agnostic STT, TTS and Coach contracts;
- applies provider authentication, transport and response parsing;
- disables or bounds provider retention according to approved privacy terms;
- emits operational metrics without logging raw audio or sensitive prompt
  content.

## 5. Authorized session context

The internal Application contract is deliberately small. Identifiers are used
for authorization and repository lookup, but are removed from the safe provider
projection unless a typed tool needs an opaque internal reference.

```text
LiveCoachSessionContext
  SessionId: Guid
  RoutineDayId: Guid?
  CapturedAtUtc: DateTime
  CurrentExercise: LiveCoachExerciseContext?
  CurrentSet: LiveCoachSetContext?
  CurrentPrescription: LiveCoachPrescriptionContext?
  CompletedSets: IReadOnlyList<LiveCoachCompletedSetContext>
  Rest: LiveCoachRestContext?
  Summary: LiveCoachSessionSummaryContext

LiveCoachExerciseContext
  ExerciseSessionId: Guid       // internal lookup only
  ExerciseId: Guid              // internal lookup only
  Name: string
  Order: int

LiveCoachSetContext
  SetNumber: int
  IsTargetSet: bool
  IsAlreadySaved: bool

LiveCoachPrescriptionContext
  TargetSets: int
  MinRepetitions: int
  MaxRepetitions: int
  MinRir: int
  MaxRir: int
  RestSeconds: int
  Notes: string?

LiveCoachCompletedSetContext
  ExerciseName: string
  SetNumber: int
  Weight: decimal
  Repetitions: int
  Rir: int?
  RecordedAtUtc: DateTime

LiveCoachRestContext
  IsActive: bool
  TargetSeconds: int?
  RemainingSecondsAtCapture: int?
  CapturedAtUtc: DateTime

LiveCoachSessionSummaryContext
  StartedAtUtc: DateTime
  ExerciseCount: int
  TargetSetCount: int
  CompletedTargetSetCount: int
```

### Context sources and validation

- `SessionId` comes from the active Training session and is revalidated against
  the authenticated user on every interaction.
- `RoutineDayId`, exercises, prescriptions and saved sets come from the active
  session snapshot in the backend.
- the current exercise/set cursor is supplied by Training UI because it is a
  presentation position, then validated against the active session snapshot;
- rest state may be supplied by Training UI because the current timer is local;
  it carries `CapturedAtUtc` and is treated as unavailable when stale;
- unsaved set form values are excluded in V0;
- profile, unrelated routines, nutrition, body data and full history are not
  included by default.

The provider-safe context uses names, prescription values and saved results. It
does not expose `SessionId`, GUIDs, user identity, hashes, policy versions,
diagnostic codes or repository terminology.

## 6. Read-only tool allowlist

Tools execute inside Application against the authorized interaction scope. The
LLM may request only a symbolic tool name and its declared input. The tool
registry rejects unknown tools, extra arguments, arbitrary IDs and calls that
do not match the active session.

All tools return a typed envelope:

```text
LiveCoachToolResult<T>
  Status: Available | NotAvailable | StaleContext | Unauthorized | Failed
  Data: T?
  SafeMessage: string?
```

Provider/internal exceptions never pass through `SafeMessage`.

### `GetCurrentExercise`

- **Input:** empty object; current interaction scope is implicit.
- **Output:** exercise name, order and whether it is the current active cursor.
- **Source of truth:** active session snapshot plus validated Training cursor.
- **Failures:** no active session, missing/invalid cursor, stale session,
  unauthorized session or repository failure.

### `GetCurrentSet`

- **Input:** empty object.
- **Output:** set number, target/extra classification and saved status.
- **Source of truth:** validated Training cursor plus active session saved sets.
- **Failures:** missing current exercise/set, cursor outside prescription,
  stale context, unauthorized session or repository failure.

### `GetCompletedSets`

- **Input:** `scope = CurrentExercise | Session`; defaults to
  `CurrentExercise`. No exercise ID is accepted.
- **Output:** ordered saved sets with exercise name, set number, weight,
  repetitions, RIR and recorded time.
- **Source of truth:** persisted sets from the active training session.
- **Failures:** no active session, unavailable current exercise for scoped
  query, unauthorized session or repository failure.

### `GetPreviousPerformance`

- **Input:** optional bounded `sessionLimit`; current exercise is implicit and
  the server enforces the maximum.
- **Output:** recent completed exposures for the current exercise, ordered by
  date, with saved sets and prescription snapshot when available.
- **Source of truth:** existing completed-session progress repositories.
- **Failures:** no current exercise, no history, unauthorized scope, canceled
  query or repository failure. No history is `NotAvailable`, not an error.

### `GetCurrentPrescription`

- **Input:** empty object.
- **Output:** target sets, repetition range, RIR range, rest and notes for the
  current exercise.
- **Source of truth:** `EjercicioSesion` in the active session, including an
  adapted prescription when the session was created by Adaptive Training.
- **Failures:** no current exercise, stale session, unauthorized session or
  repository failure.

### `GetRestTimer`

- **Input:** empty object.
- **Output:** active flag, target seconds, remaining seconds at capture and
  capture timestamp.
- **Source of truth:** authorized Training client-state snapshot for the current
  interaction.
- **Failures:** no rest state, stale client snapshot or invalid timer values.
  Absence is `NotAvailable`, not an inferred zero.

### `GetSessionSummary`

- **Input:** empty object.
- **Output:** session start, exercise count, target-set count and completed
  target-set count.
- **Source of truth:** active session and persisted saved sets.
- **Failures:** no active session, unauthorized session or repository failure.

## 7. Intent routing

Application routes every normalized transcript/text input to exactly one intent
kind before generating a response:

```text
LiveCoachIntentKind
  ConversationalQuestion
  DataQuery
  MutationRequest
  OutOfScope
```

### Conversational question

Questions answerable from the base authorized context proceed directly to the
Coach provider. The response must remain grounded in that context.

### Data query

The router produces a structured list of requested allowlisted tools. The
Application tool registry validates and executes them, then supplies only typed
safe results to the Coach provider. The provider cannot construct repository
queries or choose arbitrary identifiers.

### Mutation request

V0 returns a stable natural-language capability response. No tool executes and
no state changes. Future mutation work requires a separate allowlist,
Application command, validation and confirmation design.

### Out of scope

The response briefly states that the request is outside Live Coach V0 and does
not attempt another capability. Medical, injury, nutrition and unrelated
requests remain outside scope unless a later approved Coach capability owns
them.

Intent classification may use deterministic rules and/or a provider-generated
structured classification, but Application validates the resulting enum and
tool names. Free-form model output never becomes executable access.

## 8. STT abstraction

```text
ILiveCoachSpeechToText
  TranscribeAsync(
    LiveCoachAudioInput input,
    CancellationToken cancellationToken)
    -> LiveCoachTranscriptResult

LiveCoachAudioInput
  Content: Stream
  MediaType: string
  DurationMilliseconds: int
  Locale: string             // V0 requests es-AR
  InteractionId: Guid        // correlation, not persistence

LiveCoachTranscriptResult
  Transcript: string
  Confidence: decimal?
  DetectedLocale: string?
  IsFinal: bool
```

The adapter must support cancellation and a bounded timeout. Application
validates media type, size, duration, empty input and final transcript before
intent routing. Confidence is optional because not every provider exposes a
comparable score; the eventual low-confidence policy cannot assume it exists.

Typed failure categories:

- `UnsupportedAudio`;
- `EmptyAudio`;
- `EmptyTranscript`;
- `LowConfidence` when the approved policy can establish it;
- `Timeout`;
- `ProviderUnavailable`;
- `Canceled`.

Browser permission denial and capture-device failure occur before this contract
and are presentation failures, not STT provider errors.

## 9. TTS abstraction

```text
ILiveCoachTextToSpeech
  SynthesizeAsync(
    LiveCoachSpeechRequest request,
    CancellationToken cancellationToken)
    -> LiveCoachSpeechResult

LiveCoachSpeechRequest
  Text: string
  Locale: string
  InteractionId: Guid

LiveCoachSpeechResult
  Content: Stream
  MediaType: string
  DurationMilliseconds: int?
```

The text answer is always the canonical response. TTS is an optional rendering
of that text and cannot modify it. Cancellation must stop synthesis when
possible and playback immediately in presentation.

Typed failures are `UnsupportedText`, `Timeout`, `ProviderUnavailable` and
`Canceled`. They preserve the valid text answer.

The user has an explicit `voiceOutputEnabled` preference. When false, the TTS
contract is not called and the interaction transitions from `thinking` to
`success` after rendering text. The storage location for this preference is an
implementation decision; V0 does not require a profile-schema change.

## 10. Transport decision

### Options

| Option | Advantages | Costs/risks | V0 fit |
| --- | --- | --- | --- |
| HTTP request/response | Simple auth, cancellation and observability; broad mobile-web compatibility; matches current API | Full answer waits for provider completion | **Recommended** |
| HTTP streaming | Earlier text/audio chunks and better perceived latency | More state, proxy/runtime compatibility and partial-response handling | Defer |
| Bidirectional WebSocket/realtime | Continuous low-latency audio and barge-in | Persistent connection, reconnect, ordering, privacy and mobile lifecycle complexity | Reject for V0 |

### Recommended V0 transport

Use authenticated HTTP request/response with one interaction in flight:

1. audio mode records locally after explicit activation;
2. tap-to-stop closes capture and sends a bounded audio request;
3. the server performs STT, context projection, routing/tools and Coach
   generation under one cancellation token;
4. the response returns the final grounded text;
5. presentation renders text immediately;
6. when voice output is enabled, presentation requests TTS through a separate
   cancelable HTTP operation and plays the returned transient audio.

Text fallback skips capture/STT and sends the same interaction request with
text input. The server rejects requests that contain both audio and text.

This choice minimizes V0 surface area, aligns with the existing API and works
across mobile browsers without a persistent socket. It preserves a later path
to streaming without changing the Application context, routing or provider
contracts.

## 11. Privacy and data lifecycle

### Audio

- exists only in client memory while explicitly recording and while preparing
  the request;
- is transmitted only after tap-to-stop, over authenticated TLS;
- is bounded by type, size and duration validation;
- is streamed through server processing where possible and never written to
  application persistence;
- is discarded after STT, failure or cancellation;
- is excluded from request logs, telemetry and error payloads.

### Transcript and answer

- exist transiently for the interaction;
- are not stored as Live Coach conversation history in V0;
- are excluded from default operational logs;
- may be shown in the open panel but disappear when the interaction is closed;
- require separately approved consent/retention policy before any analytics or
  diagnostics can retain their content.

### Permission handling

The browser owns the microphone permission prompt. Training requests permission
only after explicit activation, shows `listening` only after capture begins and
never treats permission state as proof that recording is active. Denial routes
to a recoverable UI state and does not affect Training.

### Provider privacy

Provider selection requires documented retention, training-use, residency and
deletion terms. A provider that cannot satisfy the approved V0 policy is not a
valid adapter even if it implements the interface.

## 12. Required text fallback

Text input is a required V0 fallback, not an optional later enhancement. It is
available when:

- the device/browser has no compatible microphone;
- permission is denied;
- capture or STT fails;
- the user chooses not to use audio;
- voice output is disabled.

Text and audio use the same authorized context, intent routing, tools, Coach
provider and response policy. Text fallback must not receive broader access or
different mutation rights.

## 13. Failure behavior

| Failure | Architectural result | UI recovery |
| --- | --- | --- |
| Microphone denied | No audio object or STT call is created | Offer text input and permission retry where supported |
| STT timeout | Cancel provider call; discard audio; return typed `Timeout` | Retry recording or switch to text |
| Empty transcript | Stop before context/tools/Coach provider | Retry recording or enter text |
| Low confidence | Stop before Coach generation when policy can establish it | Show transcript for correction or retry; threshold remains pending |
| Coach unavailable | Return typed provider-unavailable failure; no fabricated answer | Retry same grounded text query or close |
| TTS unavailable | Preserve successful text response | Continue with text; allow voice retry only if useful |
| Network failure | Abort current stage; discard transient media | Explicit retry from the last safe input or close |
| Cancellation | Propagate cancellation token; ignore late result | Return immediately to `idle` |

Training actions, inputs and timers continue independently in every case.

## 14. Latency budget

V0 targets perceived responsiveness without claiming realtime behavior.
Budgets are product targets to validate during provider evaluation, not current
service guarantees.

| Stage | Target |
| --- | --- |
| Visual state feedback after tap/cancel | under 100 ms |
| Stop capture and release microphone | under 150 ms |
| Upload + STT | typically 0.8-2.5 s |
| Context projection and read tools | typically under 500 ms |
| Coach final text response | typically 1-4 s |
| Final text visible after tap-to-stop | target under 5 s; investigate above 8 s |
| Optional TTS preparation after text | typically 0.5-1.5 s |

Progressive feedback allowed in V0:

- immediate state labels and cancel action;
- local capture duration/level feedback that does not imply transcription;
- text rendered before optional TTS is ready.

V0 does not stream partial LLM text or audio. It does not display fake progress,
invent partial answers or speak before a grounded final response exists.

## 15. Security and safety boundaries

- session ownership is revalidated server-side for every interaction;
- client cursor/rest data is untrusted input and validated against the active
  session and capture timestamp;
- prompts treat transcript/question as user data, never system instructions;
- the provider-safe projection removes internal identifiers and implementation
  terminology;
- tool execution is closed, typed, read-only and authorization-aware;
- provider output cannot call repositories, endpoints or commands;
- no provider may write to the database or control Training;
- no V0 endpoint exposes a mutation command;
- deterministic Coach decisions remain authoritative when included in context;
- cancellation and timeout flow through all provider/tool operations.

## 16. Implementation sequence

When implementation is explicitly approved, the recommended sequence is:

1. freeze public request/response and typed failure contracts;
2. implement authorized context projection and ownership tests;
3. implement the read-only tool registry with fake adapters;
4. implement intent routing and provider-safe projection tests;
5. add text-only interaction end to end;
6. add browser capture behind capability detection;
7. select and integrate STT after privacy approval;
8. add optional TTS after the text path is stable;
9. validate cancellation, mobile lifecycle and provider failures;
10. evaluate streaming only from measured latency, not assumption.

## 17. Decisions still pending before implementation

- supported browser/device matrix and audio codec/container;
- maximum audio duration, byte limit and server timeout values;
- low-confidence policy when a provider exposes confidence and correction UX;
- exact API request/response shapes and versioning;
- whether the text fallback appears persistently or only after audio failure;
- persistence location for `voiceOutputEnabled` without expanding profile scope;
- provider choices and approved retention/residency contracts;
- exact safe prompt, intent-classification mechanism and tool-call limits;
- cursor synchronization and rest-timer staleness tolerance;
- retry budgets, rate limits, abuse controls and observability fields;
- measured latency acceptance thresholds for provider selection.

These decisions do not reopen V0's fixed boundaries: read-only behavior,
explicit activation, transient media, typed grounding, no arbitrary access and
manual Training independence.
