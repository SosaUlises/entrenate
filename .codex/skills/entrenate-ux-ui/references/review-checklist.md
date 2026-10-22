# Entrenate UX/UI closure checklist

Use only for a screen/feature closure or explicit UX audit.

## Task and hierarchy

- Is the current context and next action clear?
- Does the UI represent real product/backend state?
- Is there one dominant action with secondary/destructive actions clearly subordinate?
- Could typography, spacing or a divider replace any non-semantic surface?

## Mobile and accessibility

Check 390×667, then 390×844 and relevant desktop behavior:

- no horizontal overflow or fixed/sticky overlap;
- primary action remains reachable with keyboard and safe areas;
- controls are semantic, labeled and roughly 44–48px minimum;
- focus is visible, state does not rely only on color and reduced motion works;
- functional copy is at least 13px.

## State and continuity

- Loading, empty, error, retry, disabled and success states remain understandable.
- Active Training remains recoverable and unsaved input is protected where required.
- Technique/dialog/sheet interactions preserve their parent state.
- No fake data, unavailable action or changed backend contract was introduced.

## Scope

- Only requested files/flows changed.
- Existing primitives and feature boundaries were reused where appropriate.
- No unrelated refactor or visual migration was added.
- The change makes the task clearer, faster or more reliable, rather than only more decorative.

Run the appropriate validation tier from docs/codex-workflow.md.