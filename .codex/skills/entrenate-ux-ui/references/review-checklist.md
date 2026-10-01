# Entrenate UX/UI Review Checklist

Use this checklist before completing a UX/UI audit or implementation.

## Product clarity

- Can the user tell where they are?
- Can the user tell what happens next?
- Is the dominant action obvious?
- Does the screen reflect real backend/product state?
- Did we avoid inventing data or behavior?

## Training continuity

When applicable:

- Is an active session easy to resume?
- Is Training protected from unrelated navigation?
- Can unsaved input be lost accidentally?
- Is the primary set/rest/finish action reachable?
- Does opening technique preserve current inputs?
- Is transition between set/rest/exercise understandable?

## Hierarchy

- Is there one clear dominant CTA?
- Are secondary/destructive actions visually secondary?
- Are metrics more prominent than supporting copy where appropriate?
- Are headings meaningfully differentiated?
- Did we avoid solving hierarchy by wrapping everything in cards?

## Surfaces

For every new surface/card, ask:

- What semantic job does this surface perform?
- Is it selection, state, elevation, interactive grouping, or overlay?
- Could spacing/dividers/typography communicate the same thing more clearly?

Remove the surface if it has no job.

## Color

- Is violet reserved primarily for focus/action?
- Is blue used for information/progress rather than decoration?
- Are success/warning/error colors functional?
- Did we avoid gradients/glow/neon as a shortcut to "technology"?

## Typography

- Is Manrope used for readable UI/body content?
- Is Raleway being used intentionally for performance hierarchy?
- Can important numbers be scanned quickly?
- Are labels subordinate to values?

## Mobile

Verify `390×667`:

- Is the primary action reachable?
- Does content require avoidable scrolling?
- Does numeric keyboard cover the action?
- Do fixed/sticky elements overlap content?
- Do sheets fit the viewport?
- Are touch targets at least roughly 44–48 px?

Then verify `390×844`.

## Accessibility

- Keyboard focus remains visible.
- Controls remain semantic.
- Labels are understandable without relying only on color.
- Reduced motion is respected.
- Modal/sheet close behavior remains accessible.
- Density reductions did not create tiny controls.

## States

Where relevant, verify:

- loading;
- empty;
- error;
- retry;
- saving;
- saved;
- disabled;
- active;
- completed.

Do not use a giant container for every state.

## Scope

- Did the task change only the requested area?
- Did we preserve backend contracts unless explicitly in scope?
- Did we avoid unrelated refactors?
- Did we reuse existing architecture when suitable?
- Did we avoid duplicating components for equivalent contexts?

## Validation

From `frontend`:

```bash
npm run lint
npx tsc --noEmit
npm run build
```

If build fails externally, report the real cause.

## Final question

Before calling the work complete:

> Is this interaction actually faster, clearer, or more reliable for the person training—or did we only make it look different?
