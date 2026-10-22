# Entrenate — Codex Instructions

## Scope and ownership

Entrenate is a mobile-first strength and hypertrophy training platform. This monorepo contains:

- `frontend/`: Next.js, React, strict TypeScript and Tailwind CSS;
- `backend/`: ASP.NET Core with Domain, Application, Infrastructure and API layers;
- `docs/`: active product, design and technical contracts.

Do not modify `backend/` unless the user explicitly requests backend work. Preserve Clean Architecture/SOLID boundaries, existing API contracts and the backend ownership of business rules. Do not invent endpoints, DTO fields, metrics or product capabilities.

## Conditional reading map

Read only what the task needs. Start with directed `rg`, the target file and its direct imports.

| Task | Read |
| --- | --- |
| Micro CSS/copy/local layout | Target component, direct imports and relevant tokens only. Do not read product spec, architecture or backend. |
| UX/UI screen or shared UI | `.codex/skills/entrenate-ux-ui/SKILL.md`; read `DESIGN.md` only for visual-system decisions and `docs/design-system.md` only for tokens/shared primitives. |
| Product semantics or scope | Relevant section of `PRODUCT.md`; use the matching `docs/spec.md` heading only when legacy MVP detail is needed. |
| Frontend architecture/data flow | Relevant heading of `docs/frontend-architecture.md`, actual feature code, services and types. |
| API integration | Actual frontend service/types plus the relevant backend endpoint, request/response DTO and validation. |
| Backend/domain | Relevant backend layer and the matching `docs/spec.md` section; do not read the entire spec by default. |
| Exercise Demo V2 | Only `docs/exercise-demo-visual-standard-v2.md`, `docs/exercise-demo-task-template.md`, the V2 registry and target assets. |

Useful `docs/spec.md` sections: auth 5–6; exercises 7; routines 8–10; training 12–19 and 28; history/progress 22–25; domain/backend 26 and 29–31; API 34; security 39; roadmap 45–46 and 51–53 is historical.

## Implementation rules

- Work mobile first and keep controls accessible, semantic and touch friendly.
- Use existing CSS variables, Tailwind utilities and primitives. Do not introduce visual foundations without updating the canonical design contract.
- Keep feature-only components under `features/<feature>`; promote to shared UI only after real reuse exists.
- Centralize typed API access; keep API details and business rules out of presentation components.
- Preserve loading, empty, error, retry and duplicate-submission behavior where applicable.
- Prefer focused changes and clear code over speculative abstractions or unrelated refactors.
- Never remove visible keyboard focus without an accessible replacement.

## Efficient operation

- Use directed `rg` before opening full files. Read sections of large documents instead of dumping them.
- Do not inspect backend for purely visual work.
- Do not scan `.impeccable/mocks/` or `.impeccable/questions/` during normal runtime audits; they are historical design artifacts.
- Keep diffs and tool output scoped to touched files.
- Follow the validation, Impeccable, reasoning and reporting policy in `docs/codex-workflow.md`.
