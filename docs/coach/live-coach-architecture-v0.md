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

## 8. STT provider and abstraction

### Selected provider

Groq Speech-to-Text (`whisper-large-v3-turbo` as primary fast model, with
`whisper-large-v3` as high-accuracy fallback) is the selected STT provider for
V0. It offers sub-second inference latency on audio snippets under 15 seconds,
matching the low-interruption requirements of active training.

### Application abstraction

```text
ILiveCoachSpeechToText
  TranscribeAsync(
    LiveCoachAudioInput input,
    CancellationToken cancellationToken)
    -> LiveCoachTranscriptResult

LiveCoachAudioInput
  Content: Stream
  MediaType: string
  ByteLength: long
  DurationMilliseconds: int?
  Locale: string             // default "es" (Rioplatense dialect hinted via prompt)
  InteractionId: Guid        // correlation, not persistence

LiveCoachTranscriptResult
  Transcript: string
  Confidence: decimal?       // nullable, provider-dependent
  DetectedLocale: string?
  IsFinal: bool
  Status: LiveCoachTranscriptStatus
```

`LiveCoachTranscriptStatus`:
- `Success`;
- `EmptyTranscript`;
- `LowConfidence`;
- `UnsupportedAudio`;
- `AudioTooLarge`;
- `AudioTooLong`;
- `Timeout`;
- `ProviderUnavailable`;
- `Canceled`.

The adapter in Infrastructure (`GroqLiveCoachSpeechToTextAdapter`) wraps Groq's
HTTP audio transcriptions endpoint (`/openai/v1/audio/transcriptions`), handles
cancellation tokens, enforces a strict 5-second HTTP timeout, and maps
transport errors to typed statuses.

Application validates media type, size, duration, empty stream and non-empty
transcript before invoking intent routing.

## 9. TTS provider and abstraction

### Selected provider

ElevenLabs HTTP API is the selected TTS provider for V0. It delivers natural
conversational Spanish with low latency, suitable for short gym guidance. V0
uses standard HTTP audio fetch (or streaming to client) without persistent
WebSocket connections.

Voice persona configuration:
- Model: ElevenLabs Multilingual V2 (`eleven_multilingual_v2`);
- Persona: neutral, articulate Argentine Spanish voice profile;
- Output format: `audio/mpeg` (mp3, 44.1kHz / 64-128 kbps).

### Application abstraction

```text
ILiveCoachTextToSpeech
  SynthesizeAsync(
    LiveCoachSpeechRequest request,
    CancellationToken cancellationToken)
    -> LiveCoachSpeechResult

LiveCoachSpeechRequest
  ResponseId: Guid
  Text: string
  Locale: string             // "es-AR"
  VoiceId: string?

LiveCoachSpeechResult
  Content: Stream
  MediaType: string          // "audio/mpeg"
  DurationMilliseconds: int?
  ByteLength: long
```

Typed failures are `UnsupportedText`, `Timeout`, `ProviderUnavailable` and
`Canceled`. Any TTS failure preserves the successful text response in
presentation.

### Voice preference

The user has an explicit `voiceOutputEnabled` preference (persisted in client
local storage for V0 to avoid profile-schema mutations). When false, the TTS
endpoint is never called, and the interaction moves directly from `thinking` to
`success` once text is rendered.

## 10. Transport decision and HTTP API contracts

### Transport decision

V0 uses authenticated HTTP request/response with decoupled STT, Live Coach
turn processing, and optional TTS:

1. Audio mode records locally after explicit activation;
2. Tap-to-stop finishes recording and uploads audio to the STT endpoint;
3. Presentation sends the resulting transcript (or text fallback) along with the
   active training session context to the Live Coach interaction endpoint;
4. The server validates session ownership and context freshness, runs intent
   routing and read-only tools, invokes the Coach provider, and returns the
   grounded text answer with a unique `responseId`;
5. Presentation renders the text response immediately;
6. If `voiceOutputEnabled` is true, presentation requests audio synthesis by
   passing `responseId` to the TTS endpoint and streams/plays the resulting audio.

Text fallback skips step 2 and starts directly at step 3 with `inputKind: "Text"`.

This decoupled design ensures:
- Text is always visible immediately without waiting for speech synthesis;
- STT errors are isolated from training context processing;
- Unauthorized or arbitrary text cannot be submitted to the paid TTS provider.

### Endpoint 1: STT transcription

- **Route:** `POST /api/coach/live/transcriptions`
- **Auth:** Authenticated user (`Bearer <token>`)
- **Content-Type:** `multipart/form-data`

**Form parameters:**
- `audio`: File binary (1 KB to 2 MB, max 15s duration);
- `interactionId`: Guid (correlation ID);
- `clientTurnId`: string (client-generated idempotency key);
- `mimeType`: string (e.g. `audio/webm;codecs=opus`, `audio/mp4`).

**Response `200 OK`:**
```json
{
  "interactionId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "clientTurnId": "c4d3a2b1-0000-0000-0000-000000000000",
  "transcript": "¿Cuántas repeticiones me tocan en esta serie?",
  "confidence": 0.94,
  "detectedLocale": "es",
  "isFinal": true,
  "status": "Success"
}
```

**Failure status codes:**
- `400 Bad Request`: `UnsupportedAudio`, `EmptyAudio`, `AudioPayloadTooLarge`, `AudioDurationExceeded`;
- `422 Unprocessable Content`: `EmptyTranscript`, `LowConfidence`;
- `429 Too Many Requests`: Rate limit exceeded;
- `504 Gateway Timeout`: Provider timed out (5s limit);
- `502 / 503 Bad Gateway / Service Unavailable`: Provider unavailable.

### Endpoint 2: Live Coach interaction

- **Route:** `POST /api/coach/live/respond`
- **Auth:** Authenticated user (`Bearer <token>`)
- **Content-Type:** `application/json`

**Request payload:**
```json
{
  "sessionId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "clientTurnId": "c4d3a2b1-0000-0000-0000-000000000000",
  "inputKind": "AudioTranscript",
  "text": "¿Cuántas repeticiones me tocan en esta serie?",
  "clientSnapshot": {
    "exerciseId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "setIndex": 1,
    "restTimer": {
      "isActive": true,
      "remainingSeconds": 45,
      "capturedAtUtc": "2026-10-29T22:00:00Z"
    }
  }
}
```

**Client snapshot semantics:**
`clientSnapshot` (o hint de cursor del cliente) **NO es fuente de verdad**. El
`sessionId` y el estado de la sesión persistido en el backend son siempre
autoritativos. Los datos de `exerciseId`, `setIndex` y `restTimer` enviados por
el cliente funcionan exclusivamente como una sugerencia temporal para detectar
deriva (drift) entre el momento de la captura y la ejecución de las herramientas,
mejorando la UX sin contradecir lo que el usuario ve en pantalla. El cliente
nunca puede sobreescribir ni definir el estado del entrenamiento.

**Response `200 OK`:**
```json
{
  "responseId": "8f2a1b3c-4d5e-6f7a-8b9c-0d1e2f3a4b5c",
  "clientTurnId": "c4d3a2b1-0000-0000-0000-000000000000",
  "text": "Te tocan entre 8 y 10 repeticiones con 2 RIR.",
  "intentKind": "DataQuery",
  "isStaleContext": false,
  "canSynthesizeVoice": true
}
```

**Internal tools isolation:**
La respuesta pública nunca expone la lista de `toolsUsed` ni nombres de
herramientas internas (`GetCurrentExercise`, `GetCurrentPrescription`, etc.).
Estos nombres residen exclusivamente en la lógica de Application y en la
telemetría operativa estructurada (`LiveCoachTurnMetrics`), garantizando que la
implementación interna permanezca opaca para el frontend.

**Failure status codes:**
- `400 Bad Request`: Validation failure (empty text, invalid ID format);
- `404 Not Found`: Session not found;
- `409 Conflict`: `SessionNotActive` (session completed or discarded), `InteractionAlreadyInProgress`;
- `429 Too Many Requests`: Rate limit exceeded;
- `503 Service Unavailable`: Coach conversation provider unavailable.

### Endpoint 3: Speech synthesis (TTS)

- **Route:** `POST /api/coach/live/speech`
- **Auth:** Authenticated user (`Bearer <token>`)
- **Content-Type:** `application/json`

**Request payload:**
```json
{
  "responseId": "8f2a1b3c-4d5e-6f7a-8b9c-0d1e2f3a4b5c"
}
```

**Security design rule:**
Clients **never** submit arbitrary text to the TTS endpoint. The endpoint
accepts only a valid `responseId` generated by the Live Coach interaction
endpoint within an active turn window (TTL: 60 seconds, stored in an ephemeral
in-memory cache). The backend looks up the exact approved text produced for that
`responseId` and synthesizes only that content. This prevents arbitrary text
synthesis, prompt abuse, and unauthorized credit consumption on ElevenLabs.

**Single-instance assumption & horizontal scaling path:**
The ephemeral cache (`IMemoryCache` with TTL: 60s) is strictly designed for V0
under the explicit assumption of a single API instance. Prior to horizontal
scaling (multiple backend instances behind a load balancer), this ephemeral
storage must be migrated to an ephemeral distributed cache (e.g. Redis) or
sticky routing. No Redis or additional infrastructure components are introduced
for V0.

**Response `200 OK`:**
- `Content-Type: audio/mpeg`
- Body: Binary audio stream.

**Failure status codes:**
- `404 Not Found`: `ResponseNotFoundOrExpired`;
- `400 Bad Request`: Voice output not permitted for this response;
- `503 Service Unavailable`: TTS provider unavailable (presentation keeps visible text).

## 11. Audio capture, browser strategy and limits

### Browser capture strategy

The frontend uses standard Web APIs: `navigator.mediaDevices.getUserMedia` and
`MediaRecorder`.

Audio capture constraints:
```javascript
{
  audio: {
    echoCancellation: true,
    noiseSuppression: true,
    autoGainControl: true,
    channelCount: 1,
    sampleRate: 44100
  }
}
```

### Browser and codec support matrix

Mobile gym environments run predominantly on iOS Safari and Android Chrome:

| Platform / Browser | Primary container & codec | Fallback container |
| --- | --- | --- |
| Android Chrome / Edge | `audio/webm;codecs=opus` | `audio/webm` |
| iOS Safari (WebKit) | `audio/mp4` / `audio/aac` | `audio/mp4` |
| Desktop Chrome / Firefox | `audio/webm;codecs=opus` | `audio/ogg;codecs=opus` |

Client container selection checks `MediaRecorder.isTypeSupported()` in this
exact order:
1. `audio/webm;codecs=opus`;
2. `audio/mp4`;
3. `audio/ogg;codecs=opus`;
4. `audio/webm`;
5. Default browser container (empty option in `new MediaRecorder(stream)`).

Server-side acceptance: The backend STT endpoint and Groq Whisper accept
`audio/webm`, `audio/ogg`, `audio/mp4`, `audio/aac`, `audio/wav` and
`audio/mpeg`. Unknown media types are rejected immediately with HTTP 400
`UnsupportedAudio`.

### Audio limits

- **Maximum capture duration:** 15 seconds. The frontend starts a 15-second
  timer on recording activation and automatically stops capture when it elapses.
  The server strictly rejects audio with duration $> 18$ seconds with
  `AudioDurationExceeded`.
- **Maximum payload size:** 2 MB. Compressed speech (Opus/AAC at 32-64 kbps)
  takes 60-120 KB for 15 seconds. A 2 MB cap provides safety margin while
  preventing memory exhaustion attacks. Payloads $> 2$ MB are rejected with
  HTTP 413 `PayloadTooLarge`.
- **Minimum payload size:** 1 KB. Payloads $< 1$ KB are rejected as
  `EmptyAudio` without calling the STT provider.
- **Resource cleanup:**
  - `MediaStreamTrack.stop()` is called immediately upon tap-to-stop, error or
    cancellation to release the hardware microphone;
  - Local recording blobs are dereferenced immediately after upload;
  - Any created object URLs are synchronously revoked via
    `URL.revokeObjectURL()`.

## 12. Confidence policy and failure behavior

### Provider confidence characteristics

Groq Whisper API does not return a calibrated 0.0–1.0 word confidence in its
standard response payload (it provides `avg_logprob` or plain text).
Consequently, `Confidence` is defined as a nullable `decimal?`.

### Evaluation policy

1. **Null confidence:** If `Confidence` is null and the transcript contains
   valid non-whitespace text, the transcript is treated as usable and moves to
   intent routing.
2. **Confidence threshold:** If `Confidence` is present:
   - Score $\ge 0.45$: Proceed to intent routing;
   - Score $< 0.45$: Halt processing before tool or Coach execution; return
     typed failure `LowConfidence` alongside the preliminary text.
3. **Empty transcript:** If the transcript is empty, whitespace-only, or
   contains only noise punctuation, return `EmptyTranscript`. No LLM execution
   occurs.
4. **Presentation recovery:**
   - On `EmptyTranscript`: Prompt the user with "No llegué a escucharte. Tocá
     para reintentar o escribí tu pregunta."
   - On `LowConfidence`: Present the interpreted transcript with options to
     confirm ("¿Quisiste decir...?"), retry voice, or switch to text fallback.

### Failure mapping table

| Failure | Architectural result | UI recovery |
| --- | --- | --- |
| Microphone denied | No audio object or STT call is created | Offer text input and link to browser settings |
| STT timeout | Cancel provider call; discard audio; return `Timeout` | Retry recording or switch to text |
| Empty transcript | Stop before context/tools/Coach provider | Retry recording or enter text |
| Low confidence | Stop before Coach generation | Show transcript for confirmation or retry |
| Coach unavailable | Return typed `CoachUnavailable`; no fabricated answer | Retry same grounded query or close |
| TTS unavailable | Preserve successful text response | Continue with text; do not fail turn |
| Rate limit exceeded | Return HTTP 429 with retry delay | Show brief wait notice; keep text intact |
| Cancellation | Abort cancellation token; discard transient resources | Return immediately to `idle` |

## 13. Privacy, telemetry and zero-retention rules

### Audio privacy

- Audio exists only in client volatile memory while explicitly recording and
  during the upload request;
- Transmitted only over authenticated TLS;
- Processed entirely as an in-memory stream on the server;
- **Zero audio persistence:** Raw audio is never written to disk, databases,
  S3, or cloud storage;
- Audio is discarded immediately after STT processing, failure or cancellation;
- Audio payloads and binary streams are strictly excluded from HTTP request
  logging, Serilog outputs, and OpenTelemetry traces.

### Transcript and response retention

- Transcripts and generated responses live only in transient memory during the
  interaction turn;
- A short-lived in-memory cache (`IMemoryCache`, TTL: 60 seconds) retains the
  response text solely to validate and serve the paired TTS request (`responseId`);
- This in-memory cache assumes a single API instance for V0; prior to horizontal
  scaling, it must be migrated to an ephemeral distributed cache, without introducing
  Redis or new infrastructure now;
- Live Coach V0 does not write to any conversation history or transcript table;
- Transcripts and responses disappear from client state when the panel is closed;
- Operational logs record only execution metadata: `TurnId`, `UserIdHash`,
  `SessionId`, `DurationMs`, `ByteSize`, `MimeType`, `IntentKind`, `ToolsUsed`,
  `StatusCode`, and `ErrorCategory`. Tool names are recorded exclusively in backend
  telemetry and are never exposed in public API responses. Default logs contain zero
  PII or transcript text.

### Provider credentials

Groq and ElevenLabs API keys reside exclusively in secure backend environment
configuration. Zero provider credentials or third-party client SDKs are
exposed to the frontend.

## 14. Retries, rate limits and concurrency

### Retries

- **Client-to-API:** At most 1 automatic retry on transient network errors
  (HTTP 502/503/504 or network dropouts) using the same `clientTurnId`.
  Automatic retry is never attempted on 4xx client errors (400, 401, 403, 404,
  409, 422).
- **API-to-Providers:** At most 1 retry with exponential backoff (250 ms) for
  transient provider timeouts or 5xx responses.

### Rate limits and quotas

To protect system stability and third-party provider quotas:
- **Per-user limit:** Maximum 10 Live Coach turns per minute;
- **Per-session limit:** Maximum 60 Live Coach turns per active training
  session;
- Exceeding limits returns HTTP 429 `TooManyRequests` with a `Retry-After` header.

### Concurrency and idempotency

- **In-flight limit:** Exactly one active interaction is permitted per user
  session. A new request while a turn is processing returns HTTP 409
  `InteractionAlreadyInProgress`;
- **Idempotency:** `clientTurnId` allows the backend to recognize repeated
  transmissions resulting from network retries and return the cached turn
  result without duplicate provider execution.

## 15. Stale context and session invariants

### Session state invariant

Live Coach is valid exclusively while a `SesionEntrenamiento` has status
`EnProgreso`.
- If the session was finished, cancelled or discarded while the user was
  speaking or waiting, the interaction endpoint rejects the request with HTTP
  409 Conflict (`SessionNotActive`);
- The interaction verifies that `sessionId` belongs to the authenticated user.

### Cursor drift and synchronization

Between the moment the user starts speaking and the moment the server executes
tools (typically 2–4 seconds), the user may have logged a set or navigated to
another exercise on screen.

1. The client sends a `clientSnapshot` (cursor hint): `exerciseId`, `setIndex`, and
   `capturedAtUtc`.
   - **Non-authoritative rule:** `clientSnapshot` is **NOT** a source of truth.
     The `sessionId` and the backend-persisted state are always authoritative.
   - `exerciseId`, `setIndex` and `restTimer` sent by the client serve exclusively
     to detect temporal drift and improve UX (such as acknowledging a set just logged
     on screen). They can never define or mutate training state.
2. The server compares this snapshot against the latest persisted database state
   for the active session:
   - **No drift:** Tools execute directly against the current cursor;
   - **Drift detected (e.g. user logged set 2 while speaking):** The persisted
     database state is authoritative. Tools reflect the updated state, and a
     safely formatted drift note is injected into the Coach prompt:
     `"Nota de contexto: el usuario completó la serie anterior durante la consulta y ahora se encuentra en la serie X"`.
3. This prevents the Coach from contradicting the screen or giving obsolete
   repetition targets for a set already finished.

### Rest timer calculation

The rest timer snapshot contains `isActive`, `remainingSeconds`, and
`capturedAtUtc`. The server calculates actual elapsed time:
`currentRemaining = remainingSeconds - (UtcNow - capturedAtUtc).TotalSeconds`.
If `currentRemaining <= 0`, the timer is treated as finished (`isActive: false`).
This provides grounded timer awareness without polling the client.

## 16. Required text fallback

Text input is a required V0 capability, not a secondary feature. It is active
when:
- The device or browser lacks microphone support;
- Operating system or browser microphone permission is denied;
- Audio capture or STT fails;
- The user is in a loud gym environment or prefers silent interaction;
- `voiceOutputEnabled` is disabled.

Text input sends the same `LiveCoachInteractionRequest` with
`inputKind: "Text"`. It traverses the identical authorized context projection,
intent routing, read-only tools, and Coach generation. It possesses no
additional permissions or mutation rights.

## 17. Latency budget and observability

### Latency targets

| Stage | Target budget | Hard alert threshold |
| --- | --- | --- |
| Visual state feedback after tap | $< 100$ ms | $> 250$ ms |
| Stop capture and release hardware mic | $< 150$ ms | $> 300$ ms |
| Upload + Groq STT | 600–1500 ms | $> 3000$ ms |
| Context projection and tool execution | 50–150 ms | $> 400$ ms |
| Groq Coach LLM text completion | 800–1800 ms | $> 3500$ ms |
| **Visible text rendered on client** | **1.6–3.6 s** | **$> 6.0$ s** |
| Optional ElevenLabs TTS synthesis | 400–1000 ms | $> 2500$ ms |
| Audio playback start after text render | $< 1.0$ s | $> 2.0$ s |

### Turn observability metric schema

Every turn emits a structured `LiveCoachTurnMetrics` telemetry event:

```text
LiveCoachTurnMetrics
  TurnId: Guid
  UserIdHash: string
  SessionId: Guid
  ClientTurnId: string
  InputKind: "AudioTranscript" | "Text"
  MimeType: string?
  AudioDurationMs: int?
  AudioSizeBytes: long?
  SttDurationMs: int?
  ToolExecutionDurationMs: int?
  LlmDurationMs: int?
  TtsDurationMs: int?
  TotalTurnDurationMs: int?
  IntentKind: string
  ToolsCalledCount: int
  StatusCode: int
  ErrorCategory: string?
```

## 18. Security and safety boundaries

- Session ownership is revalidated server-side for every interaction;
- Client cursor/rest data is untrusted input and validated against the active
  session and capture timestamp;
- Prompts treat transcript/question as untrusted user data, never system instructions;
- The provider-safe projection strips internal database identifiers and
  implementation terminology;
- Tool execution is closed, typed, read-only and authorization-aware;
- Provider output cannot call repositories, endpoints or commands;
- No provider may write to the database or control Training;
- No V0 endpoint exposes a mutation command;
- Deterministic Coach decisions remain authoritative when included in context;
- Cancellation and timeout flow through all provider/tool operations.

## 19. Implementation sequence

When implementation is explicitly approved, the recommended sequence is:

1. Freeze public request/response and typed failure contracts (DTOs in Application);
2. Implement authorized context projection, drift detection, and ownership tests;
3. Implement the read-only tool registry with fake adapters;
4. Implement intent routing and provider-safe prompt builder;
5. Integrate Groq conversation provider and validate text-only interaction end-to-end;
6. Implement `GroqLiveCoachSpeechToTextAdapter` with unit/integration tests;
7. Implement `ElevenLabsLiveCoachTextToSpeechAdapter` with `responseId` validation;
8. Add frontend browser capture (`MediaRecorder`), codec detection, and audio limits;
9. Validate cancellation, mobile Safari/Chrome lifecycle, and provider failures;
10. Measure real latency against the budget under gym network conditions.

## 20. Decision status

### Decisions closed in this specification

- **STT provider:** Groq Speech-to-Text (`whisper-large-v3-turbo` primary, `whisper-large-v3` fallback);
- **TTS provider:** ElevenLabs Multilingual V2 (`eleven_multilingual_v2`), Argentine Spanish persona;
- **Conversational LLM:** Existing Groq provider with grounded, read-only gym prompt;
- **Transport architecture:** Decoupled HTTP request/response (STT $\rightarrow$ Respond $\rightarrow$ TTS);
- **TTS security model:** `responseId` verification via ephemeral in-memory cache (TTL 60s, single API instance assumption; no Redis in V0);
- **Browser/codec matrix:** iOS Safari (`audio/mp4`), Android/Chrome (`audio/webm;codecs=opus`), Firefox (`audio/ogg;codecs=opus`);
- **Audio limits:** 15s max duration, 2 MB max size, 1 KB min size, 5s provider timeout;
- **Confidence policy:** Nullable score; $\ge 0.45$ accepted, $< 0.45$ low confidence, empty transcript rejected;
- **Privacy & retention:** Zero audio storage, zero conversation database history in V0, 60s ephemeral cache for TTS, sanitized telemetry;
- **Retries & rate limits:** 1 client retry, 1 provider retry, 10 turns/min per user, 60 turns/session, 1 concurrent interaction;
- **Stale context:** `EnProgreso` required, database truth wins on drift with prompt synchronization note, rest timer calculated from elapsed time;
- **Latency targets:** $< 3.6$ s text visible, $< 1$ s audio playback start after text.

### Decisions pending before implementation

- Exact ElevenLabs Voice ID for Argentine Spanish persona;
- Quality smoke test of Groq Whisper on Argentine gym terminology (e.g. "RIR", "press de banca", "mancuernas", "remo con barra");
- Exact UI placement for the audio/text input switch inside the Training sheet.

These pending decisions do not alter the architectural boundaries, contracts or invariants established in this specification.
