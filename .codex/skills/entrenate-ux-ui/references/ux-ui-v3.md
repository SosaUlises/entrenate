# Entrenate UX/UI V3 — Product and Visual Reference

## North star

> Entrenate always knows what point of the training journey the user is in and shows the next thing they need to do.

The product should feel like a fitness performance system rather than a collection of unrelated screens.

Primary qualities:

- precision;
- performance;
- continuity;
- speed;
- clarity.

Technology is expressed through state, feedback, metrics, hierarchy, and useful motion—not decoration.

## Product state model

Use the user's actual product state to determine hierarchy.

### No routine

User need:
- configure the first real training routine.

Dominant action:
- Create first routine.

### Has routine(s), no active session

User need:
- choose what to train.

Dominant action:
- choose/start a real routine day.

### Active session

User need:
- resume training immediately.

Dominant action:
- Continue training.

The active session takes priority over routine discovery.

### Performing a set

User need:
- see target and record result.

Dominant action:
- Complete set.

### Resting

User need:
- know remaining rest and what comes next.

Dominant actions:
- wait;
- skip rest;
- continue when rest is complete.

### Exercise transition

User need:
- understand that context changed.

Desired behavior:
- short semantic transition;
- no unnecessary tap if automatic progression is safe.

### Workout ready to finish

User need:
- close the session deliberately.

Dominant action:
- Finish workout.

Do not auto-complete a workout solely because target sets are complete unless product behavior explicitly changes.

## Navigation target

### Normal app mode

Primary destinations:

- Home;
- Routines;
- Exercises.

Only real, usable features belong in primary navigation.

History and Progress should not occupy primary navigation until they exist as usable features.

### Active Training mode

Training is a focused mode.

Target behavior:

- no normal Bottom Nav;
- current workout task dominates;
- secondary menu contains mode switch/exit/cancel as appropriate;
- exiting does not imply canceling;
- protect unsaved inputs before leaving.

## Home target states

Home is a next-action surface.

### No routine

Show:

- greeting/context;
- clear first-routine guidance;
- one dominant create action;
- GymBro only when useful for orientation.

### Routines available, no active session

Show:

- real routine/day options or a direct path to them;
- no fabricated weekday/calendar logic;
- no fake recommendations.

### Active session

Prioritize:

- active status;
- routine/day or current exercise context that actually exists;
- Continue training CTA.

Other Home content becomes secondary.

## Training target

Training should be the strongest expression of the V3 language.

### Guided hierarchy

Target scan order:

1. exercise progress/context;
2. exercise name/media + technique access;
3. current set;
4. target;
5. inputs;
6. primary completion action.

Keep the viewport focused.

### Guided input principles

- weight and reps should be quick to enter;
- RIR remains optional where product rules allow;
- avoid unnecessary scroll;
- account for numeric keyboard;
- preserve entered values when opening contextual sheets.

### Save feedback

Completing a set should provide immediate, restrained feedback that the data was accepted before/while entering rest.

Avoid toast spam.

### Rest

Timer receives strong performance typography.

Useful context only:

- time;
- next exercise/set;
- minimal progress;
- skip/continue action.

### Exercise change

When moving from one exercise to another, use a short semantic cue so the user understands the context changed.

Do not create an extra interaction just for ceremony.

## Free mode target

Free mode is a dense performance tool.

Prefer a compact structure such as:

- exercise row/header;
- set number;
- kg;
- reps;
- RIR;
- completion/saved state.

Keep one exercise expanded when that reduces scroll.

Completed exercises can collapse into summaries.

A finish action should remain reachable.

## Exercise catalog

Standalone Exercises is for browsing/reference.

It should not behave like a multi-select selector without a completion destination.

When opened from routine editing/creation, the same catalog may enter contextual selection mode.

## Routines

Routine list:

- open rows;
- clear name;
- useful metadata;
- subtle separators;
- minimal decoration.

Routine detail:

- day/exercise structure easy to scan;
- avoid repeated competing primary CTAs;
- the action to start a chosen day must remain unambiguous.

Create/edit:

- progressive disclosure;
- avoid all days expanded simultaneously;
- draft continuity is valuable;
- do not turn routine editing into another long onboarding unless evidence supports it.

## Surface rules

Use surfaces for meaning:

- selection;
- temporary state;
- elevation;
- interaction grouping;
- dialogs/sheets.

Do not use surfaces merely as visual separators.

Prefer:

- spacing;
- typography;
- dividers;
- open rows.

Especially avoid:

- page surface → card → inner card → bordered input → bordered button repetition.

## Color

Base identity:

- dark background;
- violet brand accent;
- blue secondary accent.

V3 semantics:

- Violet = focus / primary interaction / primary action.
- Blue = information / progress / non-destructive status.

Functional success/warning/error colors may be used when state requires them.

No decorative rainbow.

## Typography

Manrope:
- UI;
- labels;
- paragraphs;
- controls;
- supporting copy.

Raleway:
- exercise names where appropriate;
- primary metrics;
- timers;
- set progress;
- weight/reps performance hierarchy.

Use scale/weight/spacing before adding a new container.

## GymBro

GymBro is useful when acting as:

- onboarding guide;
- empty-state guide;
- recovery helper;
- workout completion identity;
- exercise visual;
- future coaching persona.

GymBro should not be repeated as ambient decoration.

## Exercise media

GymBro thumbnails belong to Entrenate identity.

Entrenate Exercise Demo is preferred when available.

External Workout Guide content can remain a fallback while migration continues.

Technique demos must:

- remain contextual;
- preserve parent state;
- load lazily;
- respect reduced motion;
- remain technical/educational rather than decorative.

## Motion

Motion is functional.

Appropriate uses:

- save confirmation;
- perform → rest transition;
- rest complete;
- exercise transition;
- expand/collapse;
- sheet/dialog entrance;
- active state changes.

Avoid ambient movement, looping decoration, or effects whose only purpose is to look futuristic.

## Mobile standard

Design order:

1. `390×667` hard case;
2. `390×844`;
3. desktop.

If the main action is difficult to reach at `390×667`, the design is not finished.

Always account for:

- keyboard;
- safe area;
- fixed/sticky controls;
- bottom nav when present;
- bottom sheets;
- large text/metric hierarchy.

## V3 rules considered approved

1. Home represents real user state.
2. An active session is always recoverable.
3. Training is focused and does not use the normal Bottom Nav.
4. Primary navigation contains only real features.
5. Standalone Exercises is a catalog; routine-context Exercises is a selector.
6. Prefer one dominant CTA per state.
7. Every surface needs a semantic reason.
8. Raleway has a performance/metric role.
9. Violet and blue have distinct semantic roles.
10. GymBro is contextual, not filler.
11. Motion communicates state.
12. `390×667` is the hard mobile validation target.

## Scope rule

These are target principles, not permission to redesign the entire app in every task.

For each request:

- change only the requested area;
- preserve unaffected flows;
- do not proactively migrate unrelated screens;
- call out a nearby V3 inconsistency instead of silently expanding scope.
