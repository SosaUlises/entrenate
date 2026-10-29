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

## What V0 can do

- show the Live Coach presence during an active session;
- open and close its visual panel;
- let the reviewer simulate the six visual states;
- demonstrate responsive, keyboard and reduced-motion behavior;
- preserve all existing Training state while the panel is open.

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

## Next block

Before realtime implementation, define the interaction contract: activation,
permissions, interruption rules, privacy copy, audio ownership, supported
questions, deterministic grounding and failure recovery. STT/TTS, streaming and
backend integration remain separate future decisions.
