---
name: entrenate-ux-ui
description: Audit, design, and implement Entrenate UX/UI with the approved Acero Térmico system. Use for authenticated frontend screens, Training, navigation, responsive behavior, dialogs, shared UI and exercise demos.
---

# Entrenate UX/UI

Use this skill as the operational guide for frontend UX/UI work. Do not load every product or design document automatically.

## Always apply

- Make the next real action obvious and preserve continuity across routine, session, history and progress.
- Use actual API/product state. Never invent data, metrics, actions or capabilities.
- Work mobile first: validate 390×667, then 390×844, then desktop when relevant.
- Keep one dominant action per state; make destructive actions secondary and explicit.
- Prefer typography, spacing, dividers and open rows. Add a surface only for selection, state, functional grouping or a real overlay.
- Keep semantic controls, visible focus, sufficient contrast, 44–48px touch targets and non-color state cues.
- Respect reduced motion. Motion must explain save, progress, transition, expansion or overlay state.
- Cover loading, empty, error, retry, disabled and success states that the touched flow can reach.
- Change only the requested slice. Preserve backend contracts, unrelated flows and existing feature boundaries.

## Acero Térmico

Acero Térmico is the active visual direction. Training is its reference implementation.

- Carbon/graphite form the base; technical white carries primary actions and critical data.
- Thermal accent is a restrained focus/performance signal, never a universal fill.
- Steel communicates neutral information and progress.
- Raleway supports performance metrics and selected entity titles; Manrope supports readable UI.
- Keep interfaces flat by default. Avoid nested cards, gradients, glow, glassmorphism, cyberpunk/gaming styling and recurring mascots.
- Use existing semantic tokens and shared primitives before local styling.

## Training

Training is a focused operational mode for brief glances between sets.

- Immediately expose exercise, set, target, required input and next action.
- Keep the primary set/rest/finish control reachable; do not show the normal Bottom Nav.
- Preserve unsaved input around contextual UI and warn before controlled exits or mode changes that discard it.
- Distinguish saving, saved, resting, pending, completed and error states.
- Guided shows one current task. Rest prioritizes timer and next task. Free behaves as a compact performance ledger.
- Technique is contextual, lazy, reduced-motion aware and must not break the parent flow.

## Workflow

1. Use directed rg; inspect the route/component and direct dependencies.
2. Inspect backend-facing contracts only when behavior or data integration changes.
3. Reuse the current architecture and implement the smallest coherent slice.
4. Validate responsive behavior, states and accessibility in scope.
5. Use the validation tier in docs/codex-workflow.md.

## Conditional references

- Always needed: this skill only.
- Visual-system decision or shared primitive: DESIGN.md and, for implementation details, docs/design-system.md.
- Product meaning/capability: the relevant section of PRODUCT.md.
- Screen/feature closure or explicit audit: references/review-checklist.md.
- Exercise Demo production/integration: the two canonical Exercise Demo V2 docs named in AGENTS.md; do not load general UX references.
- Historical only: references/ux-ui-v3.md and .impeccable/mocks/; do not use them as current guidance.

For audits, report concrete issues and do not edit unless asked. For implementation, do not expand into an unrelated redesign. The default final report format lives only in docs/codex-workflow.md.