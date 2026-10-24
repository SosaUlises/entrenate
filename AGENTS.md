# Entrenate — Agent Instructions

Entrenate is a mobile-first strength and hypertrophy training platform:

- `frontend/`: Next.js, React, strict TypeScript and Tailwind CSS.
- `backend/`: ASP.NET Core with Domain, Application, Infrastructure and API layers.
- `docs/`: active product, design and technical contracts.

## Sources of truth

- Product scope and semantics: `PRODUCT.md`.
- Visual system: `DESIGN.md`.
- Agent workflow and validation: `docs/agent-workflow.md`.
- Current implementation and migration status: `docs/PROJECT-STATUS.md`.
- Entrenate Coach vision, functional scope, version roadmap and AI/adaptive constraints: `docs/coach.md`.

Read only the source required by the task. Start with a directed search, the target file and its direct dependencies. Load feature, architecture, API or legacy specification documents only when the requested work requires them.

## Entrenate Coach

Global Coach vision, principles and roadmap:

- `docs/coach.md`

For Coach-related work, first read `docs/coach.md`, then load only the
version-specific specification required by the task:

- Progress Coach: `docs/coach/v1-progress-coach.md`
- Routine Planner: `docs/coach/v2-routine-planner.md`
- Adaptive Training: `docs/coach/v3-adaptive-training.md`
- Discomfort & Restrictions: `docs/coach/v3-1-discomfort-restrictions.md`
- Nutrition Coach: `docs/coach/v4-nutrition-coach.md`
- Proactive Coach: `docs/coach/v5-proactive-coach.md`

Do not load unrelated Coach version specifications unless the task requires them.

The roadmap documents describe intended capabilities. They do not imply that
those capabilities are currently implemented. Use `docs/PROJECT-STATUS.md`
and the repository as the source of truth for implementation status.

## Scope and architecture

- Do not modify `backend/` unless the user explicitly requests backend work.
- Preserve Clean Architecture and SOLID boundaries, existing API contracts and backend ownership of business rules.
- Do not invent endpoints, DTO fields, metrics or product capabilities.
- Keep typed API access centralized and business rules out of presentation components.
- Keep feature-specific components within their feature until real reuse justifies promoting them.
- Make focused changes and avoid unrelated refactors.
- Preserve loading, empty, error, retry, accessibility and duplicate-submission behavior when applicable.

## Backend conventions

When working in `backend/`:

- Preserve the existing Clean Architecture dependency direction:
  - Domain must not depend on Application, Infrastructure, API, EF Core or Identity.
  - Application may depend on Domain but not on Infrastructure or EF Core.
  - Infrastructure implements persistence and external-service concerns.
  - API remains a thin delivery layer.
- Use MediatR for application use cases following the existing command/query patterns.
- Use FluentValidation for request/use-case validation where the existing architecture does so.
- Keep EF Core and persistence-specific code inside Infrastructure.
- Preserve the existing ASP.NET Core Identity and JWT authentication/authorization approach.
- Controllers must remain thin and delegate application work through the existing abstractions.
- Register dependencies in the appropriate layer/module; do not move feature configuration into `Program.cs` when an existing dependency-registration pattern applies.
- Prefer existing repositories, services and patterns before introducing new abstractions.
- Do not introduce interfaces, repositories, services or architectural patterns without a concrete need.


For Entrenate Coach features:

- Do not treat the LLM as the source of truth for training decisions.
- Prefer structured domain/application rules and deterministic engines for decisions that can be expressed explicitly.
- Use language models for natural-language understanding, explanation and conversational interaction where appropriate.
- Do not allow AI-generated output to bypass normal Application/Domain validation.
- Do not persist AI-proposed mutations without explicit product flow and user confirmation.
- Do not implement functionality from a later Coach version unless the active task explicitly requires it.

Follow the validation tier and handoff rules in `docs/agent-workflow.md`.

