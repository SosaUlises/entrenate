# Live Coach V0

## Vision

Live Coach is a focused presence inside an active Entrenate training session.
It is an interface for timely guidance, not a character, mascot or generic
chatbot. Its visual identity is an abstract status glyph: two eyes and a small
mouth rendered as precise technical geometry.

V0 establishes the product and visual foundation only. It does not introduce
realtime communication, a new decision engine or backend behavior.

## Problem

Training is an operational context where the user cannot spend time navigating
away from the current exercise. Future realtime guidance needs a recognizable,
low-interruption presence that can communicate state at a glance without
competing with the current set, rest timer or save action.

## Scope

V0 provides:

- a discreet Live Coach entry inside an active Training session;
- an abstract face aligned with Acero Termico V5;
- a mobile-first sheet that demonstrates the visual system;
- local simulation of the defined visual states;
- accessible labels, keyboard behavior and reduced-motion handling.

The implementation is a frontend prototype. State changes are local and do not
represent actual listening, processing or speech.

The interaction contract below is normative for the future V0 implementation.
The current prototype only demonstrates its visual states.

## UX placement

Live Coach lives in the Training header only while an active session exists.
The compact trigger keeps the face visible without occupying the primary work
area. Opening it presents a bottom sheet over Training; closing it returns to
the exact session state without changing or discarding training input.

Live Coach does not appear in empty, loading or error states. It does not add a
global navigation destination in V0.

## Visual language

- carbon and graphite surfaces from Acero Termico V5;
- Technical White for the eyes and primary status;
- ThermalAccent copper limited to the small mouth or a compact state signal;
- semantic success and error colors only for their corresponding state;
- no glow, gradient, violet, blue legacy color or decorative aura;
- no human features, body, avatar, name or personality styling;
- subtle opacity changes only for active simulated states, disabled under
  reduced motion.

The face is a system indicator. Copy and motion must never present it as a
sentient companion.

## Visual states

| State | Meaning in the future interaction | V0 presentation |
| --- | --- | --- |
| `idle` | Available without interrupting | Stable face, neutral status |
| `listening` | Receiving user input | Copper state signal with subtle pulse |
| `thinking` | Processing available context | Steel signal with restrained pulse |
| `speaking` | Delivering a response | Small mouth receives subtle motion |
| `success` | Interaction completed | Stable face with semantic success signal |
| `error` | Interaction could not continue | Stable face with semantic error signal |

State is always communicated by label and description in addition to color or
motion.

## Interaction model

### Explicit activation

Live Coach V0 uses explicit push-to-talk semantics. It never uses a wake word,
continuous listening or background microphone capture.

The V0 gesture is tap-to-start / tap-to-stop:

1. while `idle`, the user taps the Live Coach presence;
2. if microphone permission is not yet granted, the operating system permission
   request opens while Live Coach remains inactive;
3. once permission is granted, Live Coach enters `listening` and capture starts;
4. the user taps the primary listening control again to finish the question;
5. capture stops and the microphone is released before Live Coach enters
   `thinking`;
6. a prepared response enters `speaking` when audio playback is enabled, then
   `success`; without audio playback, the visible response moves directly from
   `thinking` to `success`.

Press-and-hold is not required in V0. Automatic endpoint detection may assist
later, but it cannot replace the visible stop control or keep the microphone
open after the user cancels.

Only one interaction may exist at a time. Repeated activation while a state
transition is pending is ignored. Closing the panel, leaving Training, ending
the session or cancelling the interaction invalidates pending results and
releases microphone/audio resources immediately.

### Immediate cancellation

`listening`, `thinking` and `speaking` always expose an immediately reachable
`Cancelar` action. Closing the sheet is equivalent to cancellation.

Cancellation:

- stops microphone capture or audio playback;
- aborts the in-flight operation when technically possible;
- ignores any late response from an operation that could not be aborted;
- discards transient audio;
- returns Live Coach to `idle`;
- never changes or discards Training input.

## State machine

| State | Entry | User feedback | Exit | Cancellation |
| --- | --- | --- | --- | --- |
| `idle` | Initial state, dismissal, cancellation or completed recovery | Stable face, `Disponible` label, no microphone indicator | Explicit tap starts permission check and then `listening`; denied permission enters `error` | Not applicable |
| `listening` | Permission granted after explicit activation | Persistent `Escuchando` label, copper signal and system microphone indicator | Explicit stop closes capture and enters `thinking`; capture failure enters `error` | Always available; returns to `idle` |
| `thinking` | Audio capture ended and usable input is ready for processing | `Procesando` label and restrained steel motion; microphone is off | Grounded response enters `speaking`, or `success` when audio playback is disabled; failure enters `error` | Always available; late results are ignored |
| `speaking` | Response text is ready and TTS playback begins | Visible response text, `Respondiendo` label and functional mouth motion | Playback completion enters `success` | Always available; playback stops and state returns to `idle` |
| `success` | Response completed successfully | Stable face, semantic success signal and response text while the panel remains open | New explicit interaction enters `listening`; close/dismiss returns to `idle` | Dismissible; no active operation remains |
| `error` | Permission, capture, STT, Coach, TTS or network failure | Natural error message, semantic error signal and one relevant recovery action | Retry enters the appropriate prior state; dismiss returns to `idle` | Dismissible; all active resources are already stopped |

The label and recovery action are authoritative. Animation and color are never
the only indication of state. Reduced-motion mode preserves every state change
without pulses or mouth motion.

## Privacy and audio lifecycle

- microphone access starts only after an explicit user activation and granted
  operating-system permission;
- `listening` is the only state in which the microphone may be active;
- the product UI and the operating system indicator must make active capture
  unambiguous;
- the microphone is released before `thinking` begins;
- there is no ambient recording, wake-word monitoring or background capture;
- raw audio is transient and must be discarded after STT, cancellation or
  failure;
- Entrenate does not persist raw audio in V0;
- Live Coach V0 does not persist conversation history or transcripts as a
  product feature;
- provider retention, telemetry or diagnostic storage cannot be enabled
  implicitly and requires a separately approved privacy and architecture
  decision.

If a permission request resolves after the user has cancelled or left Training,
the result is ignored and capture must not start.

## Interruption policy

Live Coach never opens, listens or speaks spontaneously in V0. It may respond
only to the explicit interaction that the user initiated.

- it does not interrupt an active set, rest timer, save operation or session
  completion flow;
- opening or closing Live Coach preserves unsaved Training input;
- no proactive audio is allowed, including reminders or unsolicited coaching;
- any future proactive behavior must be contextual, separately approved,
  user-controllable and silent by default until the user opts in;
- if Live Coach is unavailable, Training remains fully usable through its
  existing manual controls.

## Grounding contract

Live Coach may reason only over an application-provided, authorized snapshot
for the current user and interaction. The allowed context is:

- the active training session;
- the current exercise;
- the current set;
- the current prescription;
- sets already saved in the active session;
- additional data explicitly recovered through approved, authenticated
  capabilities or tools.

Unsaved form input is not authoritative session state and is not included
unless a future contract explicitly models it. Internal IDs, hashes, policy
versions, diagnostic codes and implementation terminology are not user-facing
context.

The LLM cannot infer, reconstruct or invent missing session state. When a fact
is not present in the authorized snapshot or an approved retrieval result,
Live Coach must say that it cannot determine it. Any future tool call must use
an explicit allowlist, authorization and typed result; arbitrary retrieval is
not permitted.

## Response contract

Responses are optimized for brief listening in a gym:

- answer the question directly in the first sentence;
- normally use one to three short sentences;
- avoid greetings, preambles, repeated context and redundant conclusions;
- prefer concrete, natural Rioplatense Spanish;
- offer more detail only when the user requests it or when omitting it would
  make the answer unsafe or misleading;
- show the same grounded response as text while audio is playing;
- never fill a provider failure with an invented fallback answer.

## Mutation policy

Live Coach V0 does not implement mutations. The following classification is a
contract for later design, not authorization to add actions now.

### Queries

Read-only questions may be answered directly from authorized context. They do
not modify Training and require no confirmation.

### Reversible or simple actions

A future allowlisted action may proceed only when the user's intent is
unequivocal, the Application/Domain layer validates it and Live Coach gives
clear visual feedback about the result. The LLM never writes state directly.

### Sensitive actions

Actions that finish or cancel a session, remove recorded work, change a
prescription, alter a routine or otherwise have material consequences require
an explicit confirmation that names the exact effect. Ambiguous speech is not
confirmation. Destructive or session-lifecycle actions require a visible
confirmation control in Training.

## Failure and recovery UX

Every failure stops capture/playback, preserves Training and offers at most one
primary recovery action.

| Failure | User-facing behavior | Recovery |
| --- | --- | --- |
| Microphone permission denied | Enter `error`; state clearly that the microphone is off | `Reintentar permiso` when the platform allows it, plus close/continue Training |
| Audio not understood | Enter `error`; do not send an invented or guessed question | `Intentar de nuevo` returns to explicit `listening` |
| STT unavailable | Enter `error`; microphone is released and no Coach request is sent | Retry when transient, otherwise close and continue manually |
| Coach/provider unavailable | Enter `error`; do not fabricate an answer | `Reintentar` the same grounded query or close |
| Connection lost | Enter `error`; cancel in-flight work and retain no audio | Retry explicitly after connectivity returns or close |
| User cancellation | Return directly to `idle`; this is not presented as an error | User may start a new interaction later |

TTS failure does not discard a valid grounded text response: audio stops, the
text remains visible and the UI explains that voice playback is unavailable.
Training is never blocked by a Live Coach failure.

## What V0 can do

- show the Live Coach presence during an active session;
- open and close its visual panel;
- let the reviewer simulate the six visual states;
- demonstrate responsive, keyboard and reduced-motion behavior;
- preserve all existing Training state while the panel is open.

The future interaction implementation may answer grounded read-only questions
under this contract. That capability is not present in the current prototype.

## What V0 cannot do

- capture microphone input or perform speech-to-text;
- synthesize speech or play audio;
- call an LLM, Coach endpoint or any new backend service;
- inspect technique, camera data, biometrics or ambient audio;
- analyze the session or produce real recommendations;
- save conversation history or claim memory;
- add, remove, complete or modify sets, exercises or the routine;
- start, finish, pause or cancel a training session;
- act without explicit user input.

## Success criteria

- the entry is discoverable without competing with Training's primary action;
- all six states remain recognizable without relying only on color;
- the face reads as a technical interface signal, not a mascot;
- the sheet is usable at 390x667 and 390x844 with safe-area support;
- opening, changing mock states and closing the sheet never mutates Training;
- keyboard focus, touch targets and reduced motion remain functional;
- the prototype introduces no network request, persistence or backend contract.
- microphone use can occur only in the explicit `listening` interval;
- cancellation is reachable during every active interaction state;
- a provider, audio or connection failure never blocks manual Training;
- responses never claim session facts absent from the authorized snapshot.

## Decisions closed by this specification

- explicit tap-to-start / tap-to-stop activation;
- no wake word, permanent listening or spontaneous speech;
- immediate cancellation in every active interaction state;
- microphone active only during `listening` and transient audio only;
- authorized snapshot grounding with no invented session state;
- concise, direct, text-visible responses;
- no mutations in V0 and explicit confirmation boundaries for future actions;
- manual Training remains independent from Live Coach availability;
- the face is a state indicator, never a mascot or character.

## Decisions pending before architecture

The next block must resolve these technical/product choices before selecting an
architecture:

- supported browsers/devices and microphone API compatibility;
- maximum utterance duration and timeout behavior;
- STT confidence threshold and whether low-confidence transcripts require
  correction before sending;
- whether text input is a required fallback for V0;
- whether users may independently disable TTS while keeping Live Coach text;
- approved STT/TTS providers, data residency, retention and privacy terms;
- transport and cancellation semantics for request/response or streaming;
- typed context snapshot, retrieval/tool allowlist and authorization boundary;
- latency targets, observability and consented telemetry;
- exact future mutation allowlist and confirmation UX.

STT, TTS, realtime transport, WebSocket, providers, tools, backend changes and
audio persistence remain outside the current implementation.
