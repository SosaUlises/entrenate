# Entrenate Coach — V1 Progress Coach

## 1. Purpose

Progress Coach is the first intelligent capability of Entrenate.

Its purpose is to transform real training history into concrete, explainable and contextual recommendations for the user's next exposure to an exercise.

Progress Coach must help answer questions such as:

- ¿Cómo vengo progresando en este ejercicio?
- ¿Conviene aumentar la carga?
- ¿Debería mantener el peso e intentar más repeticiones?
- ¿La carga actual está siendo demasiado exigente?
- ¿Por qué Entrenate me recomienda este cambio?
- ¿Qué debería intentar la próxima vez?

V1 is primarily read-only.

It analyzes and recommends, but it does not automatically modify routines, sessions or prescriptions.

The responsibility of V1 is:

```text
observe
→ analyze
→ recommend
→ explain
```

Applying recommendations automatically belongs to later versions.

---

# 2. Core principle

The progression decision must not depend on an LLM.

The architecture must follow:

```text
Real Entrenate data
        ↓
Deterministic progression analysis
        ↓
Structured recommendation
        ↓
Coach explanation
        ↓
Optional natural-language interaction
```

The deterministic engine is the source of truth.

A language model may later:

- interpret questions;
- explain results;
- summarize evidence;
- generate natural-language responses.

A language model must not:

- invent progression decisions;
- invent exercise history;
- invent RIR values;
- invent loads;
- override deterministic rules;
- modify a routine directly;
- write directly to the database.

Given the same input data, the deterministic engine must always produce the same:

- decision;
- reason codes;
- evidence level.

---

# 3. Progression strategy

V1 uses **double progression** as its initial progression strategy.

Each exercise prescription already provides:

- target set count;
- minimum repetitions;
- maximum repetitions;
- minimum target RIR;
- maximum target RIR;
- target rest duration.

The basic progression model is:

```text
keep load
→ progress repetitions within the prescribed range
→ demonstrate consistent mastery
→ recommend increasing load
→ begin progressing through the repetition range again
```

Example:

```text
Press banca

Prescription:
4 × 6-10
RIR 1-2

100 kg

Session A
8 / 8 / 7 / 7

Session B
9 / 9 / 8 / 8

Session C
10 / 10 / 10 / 9

Session D
10 / 10 / 10 / 10
```

If the upper end of the repetition range is reached consistently while effort remains compatible with the target RIR, Progress Coach may recommend increasing load.

This is the initial V1 policy.

The architecture must not assume that double progression will be the only progression strategy supported forever.

---

# 4. V1 subversions

V1 is implemented incrementally.

## V1.0 — Progress Analysis

Purpose:

Build a structured interpretation of recent performance.

Includes:

- selecting recent valid exposures;
- classifying exposure completeness;
- classifying repetitions;
- classifying RIR;
- identifying load patterns;
- comparing exposures;
- detecting top-range mastery;
- detecting excessive difficulty;
- detecting recent trends;
- determining evidence quality.

No LLM is required.

---

## V1.1 — Progress Recommendation Engine

Purpose:

Convert structured analysis into one deterministic recommendation.

Possible decisions:

```text
InsufficientData
Maintain
IncreaseRepetitions
IncreaseLoad
ReduceLoad
ReviewPerformance
```

V1.1 does not modify routines.

No LLM is required.

---

## V1.2 — Coach Explanation

Purpose:

Transform a structured recommendation into a clear explanation for the user.

The first implementation may use deterministic copy.

LLM usage is optional and must only be introduced if it provides meaningful value.

The explanation must be grounded in:

- the decision;
- reason codes;
- observed evidence.

It must not invent new reasons.

---

## V1.3 — Progress Conversation

Purpose:

Allow natural-language questions about the user's progression.

Examples:

> ¿Cómo vengo con press banca?

> ¿Por qué me recomendaste mantener el peso?

> ¿Estoy progresando?

> ¿Qué debería intentar la próxima vez?

The LLM may interpret intent and explain data.

The deterministic progression engine remains the source of truth.

V1.3 must not expand into:

- routine generation;
- routine modification;
- injury adaptation;
- nutrition;
- general medical advice.

Those capabilities belong to later Coach versions.

---

# 5. Available data

Progress Coach may use only real data available in Entrenate.

## Prescription snapshot

From `EjercicioSesion`:

- `SeriesObjetivo`
- `RepeticionesMinimasObjetivo`
- `RepeticionesMaximasObjetivo`
- `RirObjetivoMinimo`
- `RirObjetivoMaximo`
- `DescansoObjetivoSegundos`

The session snapshot must be used instead of reading the current routine prescription.

Historical analysis must not change if the routine is edited later.

---

## Real performance

From completed `SerieEntrenamiento` records:

- `Peso`
- `Repeticiones`
- `Rir`
- `NumeroSerie`
- `FechaHoraRegistro`
- `Completada`

---

## Session context

From `SesionEntrenamiento`:

- session ID;
- session state;
- start timestamp;
- end timestamp;
- user ownership.

Only completed sessions are valid progression evidence.

---

## Existing progress metrics

Entrenate may already derive:

- total volume;
- best set;
- estimated 1RM;
- first estimated 1RM;
- latest estimated 1RM;
- best historical estimated 1RM;
- absolute estimated 1RM change;
- percentage estimated 1RM change.

These metrics may support explanations and trends.

They are not the primary rule for load progression.

---

# 6. Valid exposure

An `Exposure` represents one valid exposure to an exercise inside a completed training session.

For an exposure to exist:

```text
SesionEntrenamiento.Estado == Completada
```

and there must be at least one:

```text
SerieEntrenamiento.Completada == true
```

for the analyzed exercise.

Do not use the following as primary progression evidence:

```text
EnCurso
Cancelada
```

Cancelled sessions remain historical records but do not drive V1 progression decisions.

---

# 7. Recent exposure window

Progress Coach focuses primarily on recent exposures.

Initial interpretation:

```text
latest exposure
→ immediate performance

latest 2 exposures
→ recent consistency

latest 3 exposures
→ recent trend
```

Older history may provide context but must not dominate a next-session progression decision.

The engine should normally evaluate at most the most recent relevant progression context rather than treating the full lifetime history equally.

---

# 8. Planned sets and evaluated sets

Each exercise snapshot contains:

```text
SeriesObjetivo
```

If:

```text
SeriesObjetivo = 4
```

and the user performs:

```text
Set 1
Set 2
Set 3
Set 4
Set 5
```

the sets used to evaluate prescription compliance are:

```text
Set 1
Set 2
Set 3
Set 4
```

ordered by:

```text
NumeroSerie ASC
```

Set 5 is an extra set.

Extra sets:

- remain part of history;
- may be shown to the user;
- do not improve prescription compliance;
- do not compensate for missing planned sets;
- do not participate initially in `IncreaseLoad` or `ReduceLoad` rules.

This prevents extra work from distorting progression analysis.

---

# 9. Exposure completeness

## Full

An exposure is `Full` when:

```text
CompletedPlannedSets >= SeriesObjetivo
```

Example:

```text
SeriesObjetivo = 4
Completed sets = 4

→ Full
```

Also:

```text
SeriesObjetivo = 4
Completed sets = 5

→ Full
```

The fifth set is extra.

---

## Partial

An exposure is `Partial` when:

```text
CompletedPlannedSets < SeriesObjetivo
```

Example:

```text
SeriesObjetivo = 4
Completed sets = 2

→ Partial
```

Partial exposures contain useful information but have less value for load-changing decisions.

A partial exposure by itself must never trigger:

```text
IncreaseLoad
ReduceLoad
```

---

# 10. Repetition classification

For:

```text
MinReps = RepeticionesMinimasObjetivo
MaxReps = RepeticionesMaximasObjetivo
```

each evaluated set is classified as follows.

## BelowRange

```text
Reps < MinReps
```

Example:

```text
Prescription: 6-10
Performed: 5

→ BelowRange
```

---

## InRange

```text
MinReps <= Reps < MaxReps
```

Example:

```text
Prescription: 6-10
Performed: 8

→ InRange
```

---

## TopRange

```text
Reps >= MaxReps
```

Example:

```text
Prescription: 6-10
Performed: 10

→ TopRange
```

Also:

```text
Performed: 11

→ TopRange
```

V1 does not introduce a separate category for repetitions above the maximum.

---

# 11. RIR classification

For:

```text
MinRir = RirObjetivoMinimo
MaxRir = RirObjetivoMaximo
```

remember:

lower RIR means greater effort.

Each known RIR is classified as follows.

## HarderThanTarget

```text
Rir < MinRir
```

Example:

```text
Target: RIR 1-2
Performed: RIR 0

→ HarderThanTarget
```

---

## OnTarget

```text
MinRir <= Rir <= MaxRir
```

---

## EasierThanTarget

```text
Rir > MaxRir
```

Example:

```text
Target: RIR 1-2
Performed: RIR 3

→ EasierThanTarget
```

---

## Unknown

```text
Rir == null
```

`null` must never be interpreted as:

```text
RIR 0
```

or as any other RIR value.

---

# 12. RIR coverage

For each exposure:

```text
RirCoverage =
KnownRirSets / EvaluatedSets
```

Example:

```text
4 evaluated sets
3 have RIR

RirCoverage = 0.75
```

RIR is considered sufficiently representative when:

```text
RirCoverage >= 0.5
```

This threshold does not make RIR mandatory.

It only determines how strongly the engine can use subjective effort as evidence.

---

# 13. Load classification

V1 must avoid making load progression decisions from ambiguous loading patterns.

## StableLoad

An exposure is `StableLoad` when every evaluated planned set uses the same external load greater than zero.

Example:

```text
100
100
100
100

→ StableLoad
ReferenceLoad = 100
```

---

## MixedLoad

Example:

```text
100
100
95
90

→ MixedLoad
```

---

## ZeroLoad

Example:

```text
0
0
0
0

→ ZeroLoad
```

This may represent:

- push-ups;
- bodyweight movements;
- exercises where external resistance is not currently modeled.

V1 may only produce:

```text
IncreaseLoad
ReduceLoad
```

when enough comparable `StableLoad` evidence exists.

Do not infer external load progression for `ZeroLoad`.

---

# 14. Comparable exposures

Two exposures are comparable for load progression when:

- both are `Full`;
- both use `StableLoad`;
- both use the same reference load;
- both use the same relevant prescription.

Relevant prescription fields include:

- `SeriesObjetivo`
- `RepeticionesMinimasObjetivo`
- `RepeticionesMaximasObjetivo`
- `RirObjetivoMinimo`
- `RirObjetivoMaximo`

If these values change significantly, the engine must treat the newer prescription as a new progression context.

Historical data remains available for context but must not be combined automatically to justify:

```text
IncreaseLoad
ReduceLoad
```

---

# 15. Top Mastery exposure

An exposure represents mastery of the top of the prescribed repetition range when:

```text
Exposure == Full
```

and every evaluated planned set satisfies:

```text
Reps >= MaxReps
```

and no known RIR satisfies:

```text
Rir < MinRir
```

Example:

```text
Prescription:
4 × 6-10
RIR 1-2

Performed:
10 @2
10 @2
10 @1
10 @1

→ TopMastery
```

This can also qualify:

```text
10 @3
10 @3
10 @2
10 @2
```

because effort was not harder than prescribed.

A RIR above the target may indicate additional intensity margin.

---

# 16. Overloaded exposure

An exposure has a strong excessive-load signal when at least one of the following conditions is met.

## Repetition signal

At least 50% of evaluated sets are below the minimum repetition target:

```text
BelowRangeSets / EvaluatedSets >= 0.5
```

Example:

```text
Prescription:
4 × 6-10

Performed:
6
5
5
4

3 of 4 sets are below minimum

→ Overloaded
```

---

## RIR signal

If:

```text
RirCoverage >= 0.5
```

and at least 50% of known RIR values are harder than the target:

```text
HarderThanTargetSets / KnownRirSets >= 0.5
```

Example:

```text
Target:
RIR 1-2

Performed:
0
0
1
0

→ Overloaded
```

A single `Overloaded` exposure must not automatically produce:

```text
ReduceLoad
```

---

# 17. Progress trend

V1.0 may derive a simple recent trend:

```text
Improving
Stable
Declining
Mixed
```

Trend is supporting context.

It does not directly decide the recommendation.

Trend may contribute to:

- explanation;
- evidence level;
- `ReviewPerformance`.

Do not introduce:

- regression models;
- machine learning;
- probabilistic forecasting.

---

# 18. Recommendation decisions

V1.1 must produce exactly one primary decision:

```text
InsufficientData
Maintain
IncreaseRepetitions
IncreaseLoad
ReduceLoad
ReviewPerformance
```

---

# 19. Recommendation priority

Rules must be evaluated in a stable priority order.

Initial order:

```text
1. InsufficientData
2. ReduceLoad
3. IncreaseLoad
4. ReviewPerformance
5. IncreaseRepetitions
6. Maintain
```

Specific high-value signals must resolve before neutral fallback recommendations.

---

# 20. InsufficientData

Use:

```text
InsufficientData
```

when there is not enough usable information to produce a meaningful recommendation.

## No valid exposure

```text
ValidExposures == 0

→ InsufficientData
```

---

## Only one partial exposure

```text
Exposure count = 1
Latest exposure = Partial

→ InsufficientData
```

---

## Structurally unusable information

If the available history cannot be interpreted reliably because it is too incomplete or inconsistent:

```text
→ InsufficientData
```

Important distinction:

`InsufficientData` means:

> We do not yet have enough information.

`ReviewPerformance` means:

> We have information, but the situation does not support a simple progression decision.

---

# 21. First complete exposure

One complete exposure may produce basic guidance.

It must never produce:

```text
IncreaseLoad
ReduceLoad
```

by itself.

---

## First complete exposure inside the range

Example:

```text
Prescription:
4 × 6-10

Performed:
8
8
7
7
```

Result:

```text
IncreaseRepetitions
EvidenceLevel = Low
```

---

## First complete TopRange exposure

Example:

```text
10
10
10
10
```

Result:

```text
Maintain
EvidenceLevel = Low
```

Reason:

Performance suggests future progression, but Entrenate waits for another exposure before recommending a load increase.

---

## First clearly excessive exposure

Example:

```text
5 @0
5 @0
4 @0
4 @0
```

Result:

```text
ReviewPerformance
EvidenceLevel = Low
```

Do not reduce load automatically after one poor session.

---

# 22. IncreaseRepetitions

Recommend:

```text
IncreaseRepetitions
```

when the latest exposure is `Full` and:

- every evaluated set reaches at least `MinReps`;
- at least one evaluated set remains below `MaxReps`;
- there is no repeated overload signal;
- `IncreaseLoad` conditions are not satisfied.

Example:

```text
Prescription:
4 × 6-10
RIR 1-2

Performed:
9 @2
9 @2
8 @2
8 @1
```

Result:

```text
IncreaseRepetitions
```

Conceptual explanation:

> La carga actual parece adecuada. Antes de aumentarla, intentá acercarte al extremo superior del rango de repeticiones.

---

# 23. IncreaseLoad

`IncreaseLoad` requires consistency.

It must never be generated from one exposure alone.

---

## Path A — Sufficient RIR information

Require the latest two comparable exposures to satisfy:

```text
Exposure N-1 = TopMastery
Exposure N   = TopMastery
```

with the same:

```text
StableLoad
ReferenceLoad
Prescription
```

Recent combined RIR coverage must satisfy:

```text
CombinedRirCoverage >= 0.5
```

and there must not be a meaningful recent pattern of:

```text
Rir < MinRir
```

Result:

```text
IncreaseLoad
```

Evidence should normally be:

```text
Medium
```

or:

```text
High
```

depending on additional consistent history.

---

## Path B — Insufficient RIR information

If recent:

```text
CombinedRirCoverage < 0.5
```

the absence of RIR must not permanently block progression.

Require:

```text
3 consecutive comparable exposures
```

that are all:

- `Full`;
- TopMastery by repetitions;
- `StableLoad`;
- same reference load;
- same relevant prescription.

Result:

```text
IncreaseLoad
EvidenceLevel = Medium
```

The evidence remains `Medium` because subjective intensity data is missing.

---

# 24. Zero-load exercises

If:

```text
ReferenceLoad == 0
```

V1 must not produce:

```text
IncreaseLoad
```

even when the top of the repetition range is repeatedly mastered.

Example:

```text
Flexiones
3 × 10-15

15
15
15

15
15
15

15
15
15
```

V1 must not invent:

- external load;
- bodyweight;
- harder variation;
- exercise substitution.

Possible result:

```text
ReviewPerformance
```

with a structured reason equivalent to:

```text
ZeroExternalLoad
TopRangeConsistent
```

Conceptual explanation:

> Dominaste el rango actual, pero este ejercicio necesita una estrategia de progresión diferente a aumentar una carga externa registrada.

Bodyweight-specific progression belongs to a future extension.

---

# 25. ReduceLoad

`ReduceLoad` also requires consistency.

A single poor exposure must never trigger it.

---

## Path A — Sufficient RIR information

Require:

```text
2 recent comparable exposures
```

classified as:

```text
Overloaded
```

with the same stable load and comparable prescription.

Result:

```text
ReduceLoad
```

---

## Path B — Insufficient RIR information

When RIR coverage is insufficient, require stronger repetition-based evidence:

```text
3 recent comparable exposures
```

where at least 50% of evaluated sets repeatedly fall below:

```text
MinReps
```

Result:

```text
ReduceLoad
```

---

# 26. Single poor exposure

Example:

```text
Session A
8 / 8 / 8 / 7

Session B
9 / 9 / 8 / 8

Session C
5 / 5 / 4 / 4
```

Do not immediately recommend:

```text
ReduceLoad
```

Preferred result:

```text
ReviewPerformance
```

or, when appropriate:

```text
Maintain
```

Conceptual explanation:

> Tu última sesión estuvo por debajo de tu rendimiento reciente. Antes de modificar la carga queremos confirmar si fue una caída puntual o un patrón.

---

# 27. ReviewPerformance

`ReviewPerformance` is used when enough data exists to identify a meaningful situation but not enough to safely recommend a simple progression direction.

Initial cases include:

## Strong isolated deterioration

The most recent exposure is clearly `Overloaded`, while previous performance was appropriate.

---

## Repeated partial exposures

Example:

```text
Latest 2 exposures = Partial
```

Possible causes may include:

- adherence;
- time constraints;
- fatigue;
- planning;
- interruption.

V1 must not infer the cause automatically.

---

## Contradictory evidence

Example:

```text
Exposure N-1 = TopMastery
Exposure N   = Overloaded
```

without a structured explanation for the difference.

---

## Ambiguous load pattern

Loads change frequently and there are not enough comparable exposures.

Example:

```text
100
95
90
```

followed by:

```text
102.5
97.5
92.5
```

The engine should not pretend this is equivalent to a stable-load double-progression cycle.

---

## Repeated mastery with ZeroLoad

The user repeatedly masters the upper repetition range of an exercise without external load.

---

# 28. Maintain

`Maintain` is the conservative fallback when:

- the exposure is valid;
- there is not enough evidence to increase load;
- there is not enough evidence to reduce load;
- there is no anomaly strong enough for `ReviewPerformance`;
- there is no clear repetition progression target.

---

## Confirmation before increasing load

First exposure:

```text
10
10
10
10
```

Result:

```text
Maintain
```

Purpose:

confirm consistency before recommending a load increase.

---

## Good repetitions but excessive effort

Example:

```text
10 @0
9 @0
8 @1
8 @0
```

This must not produce:

```text
IncreaseLoad
```

If isolated, it may produce:

```text
Maintain
```

or:

```text
ReviewPerformance
```

depending on the surrounding context.

---

# 29. Evidence level

Every recommendation must include one:

```text
Low
Medium
High
```

This is not a statistical probability.

It represents the quality and consistency of the available evidence.

---

## Low

Typical situations:

- one complete exposure;
- partial history;
- significant missing RIR;
- a new progression context;
- isolated unusual performance.

---

## Medium

Typical situations:

- at least two useful recent exposures;
- reasonably consistent trend;
- enough information for a justified recommendation.

---

## High

Initial requirements:

- at least three useful recent exposures;
- latest two exposures are complete;
- relevant prescription is comparable;
- load is comparable when the recommendation concerns load;
- RIR coverage is good when intensity is part of the reasoning;
- signals point in the same direction.

A recommendation does not need `High` evidence to be valid.

---

# 30. Estimated 1RM

Estimated 1RM is a secondary progression signal.

It must not be the primary rule for:

```text
IncreaseLoad
ReduceLoad
```

It may support explanations such as:

> Tu e1RM estimado también mejoró durante este período.

But a strong estimated 1RM from one set must not override poor prescription performance.

Example:

```text
Set 1
very strong

Sets 2-4
far below prescription
```

Result must not automatically become:

```text
IncreaseLoad
```

---

# 31. Prescription changes

If one of the following changes between exposures:

```text
SeriesObjetivo
RepeticionesMinimasObjetivo
RepeticionesMaximasObjetivo
RirObjetivoMinimo
RirObjetivoMaximo
```

the engine should treat this as a new progression context.

Older history remains available for context.

It must not automatically combine old and new prescriptions to justify:

```text
IncreaseLoad
ReduceLoad
```

The historical snapshot in `EjercicioSesion` makes this comparison possible.

---

# 32. Load changes

If the user changes load:

```text
100 kg
→
102.5 kg
```

the evidence required for another increase must rebuild around the new load.

Example:

```text
100 kg
10 / 10 / 10 / 10
10 / 10 / 10 / 10

102.5 kg
8 / 8 / 7 / 7
```

The final exposure is the first evidence point for:

```text
ReferenceLoad = 102.5
```

Do not immediately recommend another increase because 100 kg had already been mastered.

---

# 33. Exact load suggestions

V1 distinguishes between:

```text
direction of progression
```

and:

```text
exact numeric load recommendation
```

V1.1 decides:

```text
IncreaseLoad
ReduceLoad
Maintain
...
```

It does not yet decide:

```text
100 kg → 102.5 kg
```

Do not globally assume:

- +2.5 kg;
- +5 kg;
- +5%;
- -5%;
- any fixed increment.

Exact load progression depends on factors such as:

- exercise;
- equipment;
- available increments;
- current load;
- potentially experience.

A future extension may introduce a dedicated `LoadSuggestion` policy.

---

# 34. Reason codes

The engine should return structured reasons rather than relying only on natural-language text.

Initial reason codes may include:

```text
InsufficientHistory
PartialExposure
RepeatedPartialExposure

WithinRepRange
TopRangeReached
TopRangeConsistent
BelowMinimumReps

RirOnTarget
RirTooLow
RirMissing

StableLoad
MixedLoad
ZeroExternalLoad

RecentPerformanceDrop
PrescriptionChanged
LoadChanged
```

The exact technical names may be adjusted during implementation.

Reason codes must:

- be deterministic;
- be testable;
- support frontend presentation;
- support future Coach explanations;
- prevent the LLM from inventing causes.

---

# 35. Decision matrix

Initial summary:

```text
NO VALID DATA
→ InsufficientData

1 PARTIAL EXPOSURE
→ InsufficientData

1 FULL EXPOSURE WITHIN RANGE
→ IncreaseRepetitions / Low

1 FULL TOP-RANGE EXPOSURE
→ Maintain / Low

1 VERY POOR EXPOSURE
→ ReviewPerformance / Low

2 COMPARABLE TOP-MASTERY EXPOSURES + SUFFICIENT RIR
→ IncreaseLoad

3 COMPARABLE TOP-MASTERY EXPOSURES + INSUFFICIENT RIR
→ IncreaseLoad

2 COMPARABLE OVERLOADED EXPOSURES + SUFFICIENT RIR
→ ReduceLoad

3 COMPARABLE BELOW-RANGE EXPOSURES + INSUFFICIENT RIR
→ ReduceLoad

LATEST FULL EXPOSURE WITHIN RANGE BUT NOT MASTERED
→ IncreaseRepetitions

STRONG ISOLATED PERFORMANCE DROP
→ ReviewPerformance

REPEATED PARTIAL EXPOSURES
→ ReviewPerformance

INCOMPARABLE LOADS OR CONTRADICTORY DATA
→ ReviewPerformance or Maintain

ZERO-LOAD EXERCISE REPEATEDLY MASTERED
→ ReviewPerformance

NO STRONG SIGNAL
→ Maintain
```

---

# 36. Functional examples

## Case A — Progress repetitions

Prescription:

```text
4 × 6-10
RIR 1-2
```

Performance:

```text
100 × 9 @2
100 × 9 @2
100 × 8 @2
100 × 8 @1
```

Decision:

```text
IncreaseRepetitions
```

Reason:

The current load is appropriate, but the upper repetition target has not yet been mastered.

---

## Case B — Ready to increase load

Exposure 1:

```text
100 × 10 @2
100 × 10 @2
100 × 10 @1
100 × 10 @1
```

Exposure 2:

```text
100 × 10 @2
100 × 10 @1
100 × 10 @1
100 × 10 @1
```

Decision:

```text
IncreaseLoad
```

---

## Case C — First excellent session

Only one exposure exists:

```text
100 × 10 @2
100 × 10 @1
100 × 10 @1
100 × 10 @1
```

Decision:

```text
Maintain
EvidenceLevel = Low
```

Reason:

The performance is excellent, but consistency has not yet been confirmed.

---

## Case D — Repeated excessive load

Prescription:

```text
4 × 6-10
RIR 1-2
```

Exposure 1:

```text
100 × 6 @0
100 × 5 @0
100 × 5 @0
100 × 4 @0
```

Exposure 2:

```text
100 × 6 @0
100 × 5 @0
100 × 4 @0
100 × 4 @0
```

Decision:

```text
ReduceLoad
```

---

## Case E — One bad session

History:

```text
8 / 8 / 8 / 8

9 / 9 / 8 / 8

5 / 5 / 4 / 4
```

Decision:

```text
ReviewPerformance
```

Not:

```text
ReduceLoad
```

yet.

---

## Case F — Good progression without RIR

Latest three exposures:

```text
100 kg
10 / 10 / 10 / 10

100 kg
10 / 10 / 10 / 10

100 kg
10 / 10 / 10 / 10
```

RIR:

```text
null
```

Decision:

```text
IncreaseLoad
EvidenceLevel = Medium
```

---

## Case G — Bodyweight movement

Exercise:

```text
Flexiones
3 × 10-15
```

Recent exposures:

```text
15 / 15 / 15
15 / 15 / 15
15 / 15 / 15
```

External load:

```text
0
```

Do not return:

```text
IncreaseLoad
```

Return:

```text
ReviewPerformance
```

Reason:

The current progression model cannot determine the correct next difficulty for a zero-external-load exercise.

---

# 37. Structured recommendation contract

The progression engine must produce a structured result before any natural-language explanation is generated.

Conceptually:

```json
{
  "exerciseId": "...",
  "decision": "IncreaseRepetitions",
  "evidenceLevel": "Medium",
  "analysis": {
    "validExposureCount": 3,
    "latestExposure": {
      "isComplete": true,
      "loadType": "StableLoad",
      "referenceLoad": 100,
      "rirCoverage": 1.0
    }
  },
  "currentPrescription": {
    "plannedSets": 4,
    "minReps": 6,
    "maxReps": 10,
    "minRir": 1,
    "maxRir": 2
  },
  "reasonCodes": [
    "WithinRepRange",
    "StableLoad",
    "RirOnTarget"
  ]
}
```

This is a conceptual product contract.

The final Application DTOs and technical types must be designed after auditing the existing backend.

Do not implement this JSON literally without inspecting the current architecture.

---

# 38. Explanation requirements

Every visible recommendation must be able to answer:

## What was detected?

Example:

> Alcanzaste el extremo superior del rango en las cuatro series.

## What is recommended?

Example:

> La próxima vez podrías aumentar la carga.

## Why?

Example:

> Lo lograste manteniendo el esfuerzo dentro del RIR objetivo en tus últimas dos exposiciones.

Explanations must be derived from structured evidence.

An LLM must not add reasons that the deterministic engine did not identify.

---

# 39. Initial frontend integration

Progress Coach should first integrate with the existing Progress experience.

Conceptually:

```text
Press banca

Progress
+11.11% estimated 1RM since first valid record

Coach
────────────────────────

Status
Ready to progress

Recommendation
Increase load

Why
You reached the upper repetition range while
maintaining target effort across recent exposures.

[ View analysis ]
```

V1 does not require a full conversational interface before the deterministic engine is useful.

---

# 40. Out of scope for V1

Progress Coach V1 must not:

- create routines;
- edit routines;
- automatically modify prescriptions;
- automatically change weight;
- automatically change repetitions;
- automatically change set count;
- replace exercises;
- adapt workouts based on time constraints;
- adapt training based on equipment availability;
- manage pain or injuries;
- diagnose fatigue;
- prescribe deloads;
- provide nutrition guidance;
- create a PR entity;
- train custom ML models;
- use ML for progression decisions;
- modify the database directly from an LLM;
- implement bodyweight progression logic;
- determine exact load increments.

---

# 41. Technical success criteria

Given the same:

```text
exercise
prescription snapshots
completed exposures
completed sets
weights
repetitions
RIR values
```

the progression engine must always produce the same:

```text
decision
evidence level
reason codes
```

The result must not depend on:

- prompt wording;
- model provider;
- temperature;
- generative output;
- conversational history.

A future language layer may change how the recommendation is worded.

It must never change the underlying deterministic decision.

---

# 42. V1 completion criteria

Progress Coach V1 is complete when Entrenate can:

1. identify valid recent exposures for an exercise;
2. interpret prescription compliance;
3. classify repetition performance;
4. interpret available RIR data;
5. distinguish full and partial exposures;
6. distinguish stable, mixed and zero external load;
7. detect top-range mastery;
8. detect repeated excessive difficulty;
9. detect ambiguous or contradictory performance;
10. produce exactly one deterministic recommendation;
11. provide an evidence level;
12. provide structured reason codes;
13. explain the recommendation using real evidence;
14. expose the result in the Progress experience;
15. answer basic progression questions without inventing information.

Automatic application of recommendations belongs to later Coach versions.

---

# 43. Current implementation priority

The implementation order for V1 is:

```text
V1.0
Progress Analysis
        ↓
V1.1
Progress Recommendation Engine
        ↓
V1.2
Coach Explanation
        ↓
V1.3
Progress Conversation
```

Before implementing V1.0, audit the existing backend specifically against this specification.

The audit must determine:

- which required inputs already exist;
- whether historical snapshots contain all required prescription data;
- whether existing progress repositories can supply the required exposures efficiently;
- whether new Application models are required;
- whether any Domain changes are actually necessary;
- whether the current data model can distinguish StableLoad, MixedLoad and ZeroLoad;
- whether current RIR nullability supports the defined rules;
- how progression analysis should be represented structurally;
- how the recommendation result should be exposed to the frontend.

Do not implement V1.2 or V1.3 before V1.0 and V1.1 are deterministic, tested and validated.
# 44. Normative clarifications after backend audit

The backend audit confirmed that the current persistence model is sufficient for V1.0 and V1.1.

The following rules resolve ambiguities discovered before implementation and supersede any conflicting wording earlier in this specification.

## 44.1 Planned set identity

Planned sets are identified by their explicit `NumeroSerie`.

For:

```text
SeriesObjetivo = N
```

the planned set numbers are:

```text
1..N
```

An evaluated planned set is a completed set whose:

```text
1 <= NumeroSerie <= SeriesObjetivo
```

An extra set is a completed set whose:

```text
NumeroSerie > SeriesObjetivo
```

Extra sets never replace missing planned set numbers.

Example:

```text
SeriesObjetivo = 4

Persisted completed sets:
1
2
4
5
```

Interpretation:

```text
Evaluated planned sets:
1
2
4

Missing planned set:
3

Extra sets:
5

Exposure:
Partial
```

Therefore:

```text
CompletedPlannedSets =
count of completed sets with NumeroSerie between 1 and SeriesObjetivo
```

and:

```text
Full =
CompletedPlannedSets == SeriesObjetivo
```

Otherwise:

```text
Partial
```

Because `(EjercicioSesionId, NumeroSerie)` is unique, duplicate planned set numbers do not need special handling in normal V1 operation.

---

# 44.2 Exposure identity

A Progress Coach `Exposure` corresponds to exactly one:

```text
EjercicioSesion
```

Therefore its internal identity is:

```text
ExerciseSessionId
```

An Exposure also contains:

```text
SessionId
ExerciseId
HoraInicio
PrescriptionSnapshot
CompletedSets
```

Do not aggregate multiple `EjercicioSesion` records only because they share:

```text
SessionId + ExerciseId
```

Two occurrences of the same exercise inside the same training session remain separate exposures because they may have different:

- order;
- prescription snapshots;
- sets;
- loads;
- performance.

The Progress Coach read model must preserve `ExerciseSessionId`.

---

# 44.3 Multi-exposure consistency requires distinct sessions

Rules that require consistency across multiple exposures must not satisfy that requirement using two occurrences of the same exercise from the same training session.

Therefore:

```text
2 comparable exposures
```

means comparable exposures from:

```text
2 distinct SessionId values
```

and:

```text
3 comparable exposures
```

means comparable exposures from:

```text
3 distinct SessionId values
```

When evaluating progression from the latest exposure, search backwards for comparable exposures belonging to previous completed sessions.

Additional occurrences of the same exercise inside the current session may still be analyzed independently, but they do not count as another session of progression evidence.

---

# 44.4 V1.0 does not produce EvidenceLevel

V1.0 — Progress Analysis is responsible only for deterministic observations.

It must produce facts such as:

```text
Exposure completeness
Evaluated set count
Extra set count

Rep classifications
RIR classifications
RIR coverage

Load classification
Reference load

TopMastery
Overloaded

PrescriptionChanged
LoadChanged

Comparable exposure count
```

V1.0 does not produce:

```text
EvidenceLevel
ProgressDecision
ReasonCodes
```

Those belong to V1.1.

This keeps analysis independent from recommendation policy.

---

# 44.5 EvidenceLevel belongs to V1.1

After a recommendation has been selected, V1.1 assigns one deterministic evidence level:

```text
Low
Medium
High
```

## Low

Use `Low` when the recommendation relies primarily on a single useful exposure or an isolated event.

Examples:

```text
first complete exposure
first TopMastery
single overloaded exposure
single recent anomaly
```

`InsufficientData` always uses:

```text
Low
```

## Medium

Use `Medium` when at least two relevant recent exposures from distinct sessions provide sufficient consistent evidence for the recommendation.

Examples:

```text
2 comparable TopMastery exposures with sufficient RIR
2 comparable Overloaded exposures with sufficient RIR
repeated partial exposures
repeated consistent performance
```

Recommendations reached through the no-RIR fallback paths are capped at:

```text
Medium
```

even if three exposures are available.

## High

Use `High` only when:

```text
at least 3 relevant exposures
from 3 distinct completed sessions
```

exist inside the same progression context and:

```text
the latest 2 exposures are Full
```

and the recent evidence does not contain contradictory progression states.

For load-sensitive recommendations:

```text
IncreaseLoad
ReduceLoad
```

High additionally requires:

```text
StableLoad
same ReferenceLoad
same relevant prescription
CombinedRirCoverage >= 0.5
```

across the relevant recent evidence.

If any High requirement is not satisfied:

```text
EvidenceLevel = Medium
```

when the recommendation otherwise has sufficient evidence.

---

# 44.6 No-RIR load decisions cannot be High

The fallback progression rules that compensate for missing RIR intentionally require more exposure history.

However, missing subjective effort data still reduces evidence quality.

Therefore:

```text
IncreaseLoad through the 3-exposure no-RIR path
→ EvidenceLevel = Medium
```

and:

```text
ReduceLoad through the 3-exposure no-RIR path
→ EvidenceLevel = Medium
```

They must not return `High`.

---

# 44.7 Isolated Overloaded exposure

If the latest exposure is:

```text
Overloaded
```

but the preceding comparable exposure was not `Overloaded`, the result is:

```text
ReviewPerformance
```

not:

```text
Maintain
ReduceLoad
```

This applies whether overload was detected through:

```text
repetitions
RIR
```

The purpose is to recognize the anomaly without reacting aggressively to one poor session.

---

# 44.8 Good repetitions with excessive RIR effort

An exposure such as:

```text
Prescription:
4 × 6-10
RIR 1-2

Performed:
10 @0
9 @0
8 @1
8 @0
```

meets the RIR overload condition because at least 50% of known RIR values are below `MinRir`.

If this is an isolated occurrence:

```text
ReviewPerformance
```

must be preferred over:

```text
Maintain
IncreaseRepetitions
IncreaseLoad
```

If the overload pattern repeats according to the `ReduceLoad` rules, the recommendation may then become:

```text
ReduceLoad
```

---

# 44.9 Repeated partial exposures

If the latest two valid exposures from distinct completed sessions are both:

```text
Partial
```

the decision is:

```text
ReviewPerformance
```

The engine must not infer why they are partial.

It may emit:

```text
RepeatedPartialExposure
```

as a reason code.

---

# 44.10 Contradictory recent performance

If the latest two comparable exposures from distinct sessions contain opposite strong states:

```text
one TopMastery
one Overloaded
```

the decision is:

```text
ReviewPerformance
```

unless a higher-priority deterministic rule explicitly applies.

The engine must not infer the cause of the variation.

---

# 44.11 Trend postponed

`ProgressTrend` is not required for the first implementation of V1.0 or V1.1.

Do not initially implement:

```text
Improving
Stable
Declining
Mixed
```

until a separate deterministic algorithm for trend has been approved.

Trend must not block:

```text
ProgressAnalysis
ProgressRecommendation
```

The first version of these contracts should omit Trend rather than invent or approximate it.

---

# 44.12 Unexpected malformed historical data

Progress Coach should operate on data that satisfies the existing Domain and persistence invariants.

Do not introduce speculative recommendation rules for malformed historical records.

If an impossible historical state prevents deterministic analysis, the application should treat it as an unexpected data condition rather than inventing a training recommendation.

`InsufficientData` is intended for insufficient valid training evidence, not for hiding corrupted data.

---

# 44.13 Current implementation boundary

With these clarifications, V1.0 may now begin.

The first implementation target is:

```text
ProgressExposureAnalyzer
```

inside:

```text
Entrenate.Application/Coach/Progress/Analysis
```

Its responsibility is to analyze one already-provided exposure.

It must not initially:

```text
query the database
calculate EvidenceLevel
produce ProgressDecision
produce ReasonCodes
calculate Trend
expose an API endpoint
use an LLM
```

Its output should contain only deterministic analysis facts required by later stages.

## Exposure with no evaluated planned sets

If an Exposure contains completed sets but none of them belong to the planned
range `1..SeriesObjetivo`, the Exposure is `Partial`.

Because there are no evaluated planned sets:

- `RirCoverage` is null;
- `LoadClassification` is null;
- `ReferenceLoad` is null;
- `IsTopMastery` is false;
- `IsOverloaded` is false.

Extra sets do not provide progression evidence by themselves.