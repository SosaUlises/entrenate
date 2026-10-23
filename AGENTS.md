# Entrenate — Agent Instructions

Entrenate is a mobile-first strength and hypertrophy training platform:

- `frontend/`: Next.js, React, strict TypeScript and Tailwind CSS.
- `backend/`: ASP.NET Core with Domain, Application, Infrastructure and API layers.
- `docs/`: active product, design and technical contracts.

## Sources of truth

- Product scope and semantics: `PRODUCT.md`.
- Visual system: `DESIGN.md`.
- Agent workflow and validation: `docs/agent-workflow.md`.
- Current migration status: `docs/PROJECT-STATUS.md`.

Read only the source required by the task. Start with a directed search, the target file and its direct dependencies. Load feature, architecture, API or legacy specification documents only when the requested work requires them.

## Scope and architecture

- Do not modify `backend/` unless the user explicitly requests backend work.
- Preserve Clean Architecture and SOLID boundaries, existing API contracts and backend ownership of business rules.
- Do not invent endpoints, DTO fields, metrics or product capabilities.
- Keep typed API access centralized and business rules out of presentation components.
- Keep feature-specific components within their feature until real reuse justifies promoting them.
- Make focused changes and avoid unrelated refactors.
- Preserve loading, empty, error, retry, accessibility and duplicate-submission behavior when applicable.

Follow the validation tier and handoff rules in `docs/agent-workflow.md`.
