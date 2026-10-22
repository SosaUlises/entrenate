# Codex workflow for Entrenate

This is the single source of truth for validation depth, Impeccable use, reasoning level and default final reports.

## Validation tiers

### Micro

Use for copy, token, spacing, one local style or known registry/asset integration:

- `npm run check:quick` from `frontend`;
- lint the touched TS/TSX file directly when applicable: `npx eslint path/to/file.tsx`.

Do not run a build or Impeccable unless the change exposes a concrete wider risk.

### Screen

Use when closing or materially changing one screen:

- `npm run check:screen`;
- run Impeccable once after implementation when UI changed.

### Feature

Use for multi-screen behavior, shared feature state or shared UI:

- `npm run check:full`;
- run relevant existing tests;
- run Impeccable once when UI changed.

### Release

- run `npm run check:full`;
- build/test backend only if backend was touched;
- inspect the final scoped diff and generated artifacts.

No frontend test command currently exists, so `check:full` delegates to `check:screen`. Add tests to the tier only when real test infrastructure exists.

## Impeccable

Run manually for screen closure, shared components/foundations, broad responsive work or an explicit visual audit. Skip it for micro edits. Do not run both automatic and manual passes; repository hooks intentionally do not invoke it. Exclude `.impeccable/mocks/` and `.impeccable/questions/` from normal runtime audits.

## Tool and context discipline

- Start with directed `rg`, then read the target and direct imports.
- Read only relevant sections of large docs and contracts.
- Avoid global searches, backend inspection and global diffs for microtasks.
- Keep command output bounded; expand only when a concrete failure requires it.

## Suggested reasoning level

- Low: micro CSS/copy and known asset or registry integration.
- Medium: screen migration or localized refactor.
- High: architecture, difficult state/concurrency bugs, security or domain changes.

## Default final report

Unless the user asks for a specific report, return only:

1. files changed;
2. change made;
3. validation result;
4. real blocker or pending item, if any.