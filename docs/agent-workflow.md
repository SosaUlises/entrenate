# Agent workflow

## Validation

- Micro task: run `npm run check:quick` from `frontend/`.
- Screen migration: run `npm run check:screen` from `frontend/` and run Impeccable once at closure.
- Feature or architecture work: run `npm run check:full` from `frontend/`.

## Continuity

- When possible, the same agent must start and finish a task.
- Before changing agents, validate the work and leave a clean working tree with a commit.

## Context discipline

- Start with directed searches, the target file and its direct dependencies.
- Avoid global searches and documents unrelated to the task.

## Project status updates

Update docs/PROJECT-STATUS.md only when:
- a screen or feature is completed;
- a major project decision changes;
- the current/next project milestone changes.

Do not update it for micro-polish, spacing, copy, or isolated UI tweaks.

## Final report

Keep the report short:

1. files changed;
2. change made;
3. validation result;
4. real blocker, if any.
