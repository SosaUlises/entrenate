---
name: entrenate-ux-ui
description: Audit, design, and implement UX/UI changes for the Entrenate fitness app using the approved UX/UI V3 "Dark Performance Interface" principles. Use when reviewing or changing authenticated frontend experiences such as Home, Routines, Exercises, Training, Guided/Free modes, navigation, dialogs/sheets, exercise demos, responsive mobile behavior, or shared UI patterns.
---

# Entrenate UX/UI

Use this skill for UX/UI work in Entrenate.

Entrenate is a mobile-first strength/hypertrophy training product. The goal is not merely to make screens visually attractive. The interface must make the user's next action obvious, preserve training continuity, and feel like a precise fitness-performance tool.

Before making decisions, read:

- `references/ux-ui-v3.md` for the approved product and visual direction.
- `references/review-checklist.md` before finalizing a UX/UI change.

Treat the V3 reference as the target design language. Do not migrate unrelated screens unless the task explicitly asks for them.

## Core product principle

Use this as the north star:

> Entrenate always knows what point of the training journey the user is in and shows the next thing they need to do.

Optimize for:

- clarity;
- continuity;
- speed;
- precision;
- performance;
- low cognitive load.

Do not optimize for decorative futurism.

## First inspect the real implementation

Before proposing or changing UX/UI:

1. Read the relevant `AGENTS.md` instructions.
2. Inspect the actual route, feature components, shared primitives, styles, hooks, services, and types involved.
3. Inspect backend-facing types/contracts used by the screen when they affect UI behavior.
4. Reuse established architecture and primitives where reasonable.
5. Do not infer product capabilities that do not exist in the code or API.

Do not invent:

- calendar semantics that are not represented by backend data;
- fake metrics;
- fake activity;
- fake history/progress;
- unavailable actions;
- new API fields.

## Decide whether the task is audit or implementation

### If the request is an audit, analysis, or proposal

- Do not modify code unless explicitly asked.
- Identify real problems from the current implementation.
- Distinguish UX problems from purely cosmetic issues.
- Prioritize comprehension and task completion over visual novelty.
- Cite concrete routes/components in the report.
- Prefer a small number of high-impact recommendations.

### If the request is implementation

- Change only the approved/requested slice.
- Do not perform unrelated redesigns.
- Preserve existing backend contracts unless the task explicitly includes backend changes.
- Prefer small, composable changes over broad rewrites.
- Keep business logic separate from presentation.
- Do not move configuration into inappropriate layers for convenience.

## Mobile-first requirement

The hard mobile validation viewport is:

- `390×667`

Also check:

- `390×844`
- desktop after mobile behavior is sound.

A core training action should not require unnecessary scrolling on `390×667`.

Always check:

- sticky/fixed controls;
- safe-area spacing;
- bottom navigation overlap;
- keyboard overlap;
- touch targets;
- modal/bottom-sheet height;
- text wrapping;
- dense input layouts.

## Training is the highest-priority experience

During Training, optimize for a user who looks at the phone for only a few seconds between sets.

The UI should answer immediately:

- What exercise am I doing?
- What set am I on?
- What is the target?
- What do I need to enter?
- What happens next?

Rules:

- Training is a focused operational mode.
- Do not show the normal Bottom Nav during an active Training experience.
- Keep destructive actions secondary and confirmed.
- Preserve unsaved inputs when opening contextual UI such as technique demos.
- Warn before abandoning unsaved series input.
- Keep the primary action reachable.
- Prefer one dominant CTA per state.
- Use progressive disclosure instead of showing every control at once.
- Distinguish `saving`, `saved`, `resting`, `completed`, `error`, and `pending` states clearly.
- Use short functional transitions to communicate state changes.
- Do not add motion only for decoration.

### Guided mode

Guided should emphasize:

- exercise context;
- current set;
- target;
- weight/reps/RIR input;
- one primary `Complete set` action.

Keep context compact enough for small phones.

### Rest state

Rest is a distinct state, not a generic form.

The timer should be one of the strongest typographic elements on screen.

Show only what is useful:

- remaining time;
- what comes next;
- skip/continue action;
- minimal progress context when helpful.

### Free mode

Free mode should feel like a performance log, not a stack of forms.

Prefer:

- compact rows;
- one expanded exercise at a time;
- completed exercises collapsed or summarized;
- visible set status;
- accessible finish action.

Avoid:

- exercise card → set card → input card nesting;
- repeating a large Save button for every row when a lighter interaction can work;
- forcing the user to scroll to the very end to finish the workout.

## Navigation and continuity

Primary navigation must represent real, usable product areas.

Target V3 normal navigation:

- Home;
- Routines;
- Exercises.

Do not place nonexistent features in primary navigation.

Exercises has two contexts:

- standalone: educational/reference catalog;
- from routine create/edit: contextual exercise selector.

Do not mix selection behavior into the standalone catalog without a destination for that selection.

An active training session must always be easy to recover.

When a session is active:

- Home should prioritize `Continue training`;
- other normal app areas may expose a compact resume affordance when appropriate;
- do not require the user to try starting another session to discover the active one.

## Home is state-driven

Home must represent real user state instead of static copy.

Conceptual states:

1. No routine
   - dominant action: create first routine.

2. Has routines, no active session
   - dominant action: choose/start training from a real routine/day.

3. Active session
   - dominant action: continue training.
   - active-session context takes priority over routine discovery.

Do not fabricate "Today", weekdays, recommended schedule, streaks, or next workout logic unless backed by actual product data.

GymBro may guide first use, recovery, empty states, or meaningful milestones. Do not use GymBro merely to fill space.

## Routines

Keep routine browsing easy to scan.

Prefer:

- open rows;
- dividers;
- typography;
- concise metadata.

Avoid turning every routine/day/exercise into nested cards.

For create/edit flows:

- use progressive disclosure;
- avoid keeping every day expanded at once;
- retain draft state across reasonable interruptions when implementing persistence;
- keep create/edit/detail concepts aligned.

Do not change routine API semantics merely to simplify a visual task.

## Exercise demos

GymBro remains the Entrenate visual identity for exercise thumbnails.

`Ver técnica` is contextual and should:

- open without navigating away from the training task;
- preserve current form/input state;
- load demos on demand;
- respect reduced motion;
- fail without breaking the session.

Prefer Entrenate-owned exercise demos when available.

External Workout Guide content may remain as a fallback where currently supported. Preserve required attribution for external assets.

Do not make exercise media decorative; it is technical information.

## Visual language: Dark Performance Interface

The interface should feel:

- dark;
- precise;
- athletic;
- modern;
- intelligent;
- premium;
- operational.

It should not feel:

- cyberpunk;
- gamer;
- generic SaaS;
- admin dashboard;
- Tailwind template;
- AI-themed decoration.

### Surfaces

Use this rule:

> A surface must communicate something. It must not exist only to separate content.

Good reasons for a surface:

- selection;
- elevation;
- temporary state;
- interactive grouping;
- overlay/sheet/dialog.

Prefer for normal content:

- open layout;
- typography;
- spacing;
- dividers;
- rows.

Avoid card-inside-card layouts.

### Color semantics

Current brand base remains dark with restrained violet/blue.

Target semantics:

- violet: focus, interaction, primary action;
- blue: information, progress, non-destructive status.

Use success/warning/error colors only when they communicate functional state.

Do not use violet for every interactive or highlighted element.

Avoid:

- heavy gradients;
- pervasive glow;
- neon decoration;
- glassmorphism as a default;
- fake AI/futuristic motifs.

### Typography

- Raleway: exercise names, important numbers, metrics, timers, performance-oriented hierarchy.
- Manrope: body copy, controls, supporting UI.

Use typography to create hierarchy before adding containers.

Performance values can be visually strong:

- set count;
- weight;
- reps;
- timer;
- progress.

Do not make every heading visually equivalent.

### Motion

Motion must communicate:

- saved;
- state changed;
- exercise changed;
- content expanded/collapsed;
- contextual overlay entered/exited.

Prefer short, restrained transitions.

Always respect `prefers-reduced-motion`.

## GymBro

GymBro is a contextual coach/identity asset, not general decoration.

Good contexts:

- onboarding;
- first routine/empty state;
- recovery/help;
- workout completion;
- exercise imagery;
- future coaching/AI interventions.

Avoid placing GymBro beside unrelated metrics simply because space exists.

## Interaction rules

- One dominant CTA per state whenever possible.
- Destructive actions remain secondary.
- Keep touch targets roughly 44–48 px minimum.
- Preserve visible focus states.
- Use native semantic controls where possible.
- Do not remove accessibility to achieve a denser visual.
- Prefer direct manipulation and inline state over avoidable modals.
- Use sheets/dialogs for contextual tasks that should preserve the parent state.
- Avoid redundant confirmation steps for reversible actions.
- Confirm destructive/irreversible actions.

## Error, loading, and empty states

Every touched flow should account for:

- loading;
- empty;
- error;
- retry;
- success where relevant.

Keep them proportional to the screen.

Do not create a large card for every error.

Error copy should explain what the user can do next.

## Implementation workflow

For an implementation task:

1. Inspect the current route and components.
2. State the UX problem being solved internally before coding.
3. Check the V3 reference.
4. Identify the smallest set of files needed.
5. Reuse current primitives where they fit.
6. Implement only the requested scope.
7. Verify mobile hard case `390×667`.
8. Verify `390×844`.
9. Verify desktop if the component has desktop behavior.
10. Check loading/error/empty/reduced-motion states that apply.
11. Run validation.

From `frontend`, normally run:

```bash
npm run lint
npx tsc --noEmit
npm run build
```

If build fails because of an external/network condition, report that specifically instead of changing architecture to work around the environment.

## Definition of done

A UX/UI task is not done only because it looks better.

It is done when:

- the primary task is clearer or faster;
- the next action is evident;
- the screen works at `390×667`;
- hierarchy is stronger without decorative clutter;
- active/saved/error states are understandable;
- accessibility is not degraded;
- existing contracts and unrelated flows still work;
- validation commands pass, or external failures are clearly identified.

Before finalizing, read `references/review-checklist.md`.

## Final report

For implementation tasks, report concisely:

1. files changed;
2. UX problem addressed;
3. main V3 decisions applied;
4. mobile behavior checked;
5. states/accessibility considered;
6. lint result;
7. TypeScript result;
8. build result.

Do not claim visual verification if no browser/screenshot tooling was available.
