# ReactCore Fullstack Constitution

This document defines the non-negotiable principles governing all feature development in the ReactCore project (React + ASP.NET Core fullstack application).

## Core Principles

### I. Clean Architecture

All features must respect strict layered separation of concerns.

**Backend Structure**:

- Controllers → Services → Repositories → Data
- Controllers: Accept HTTP requests, validate input format, delegate to services
- Services: Contain business logic, orchestrate operations, handle domain rules
- Repositories: Data access abstraction layer, work with entities
- Data: Entity Framework DbContext, migrations, database configuration

**Frontend Structure**:

- Pages → Components → Services → State
- Pages: Route-level components that compose features
- Components: Reusable UI elements, no business logic
- Services: API communication layer, external service calls
- State: Global state management (Context API/Redux), not scattered throughout components

**Non-Negotiable Rule**: No business logic in controllers or components. Logic violations are blocking issues in code review.

**Rationale**: Clean separation enables testability, maintainability, and allows independent component evolution. Business logic in UI code is a path to unmaintainable, untestable systems.

---

### II. Test-Driven Development (TDD) Mandatory

Tests must be written and approved BEFORE implementation begins. This is not optional.

**Requirements**:

- All new features require contract tests (API specifications)
- All new APIs require integration tests (end-to-end workflows)
- All services and repositories must have unit tests
- Coverage minimum 70% for services and repositories
- No implementation code is merged without passing tests

**Implementation Pattern**:

1. Write failing test that documents expected behavior
2. Submit test for review (approval required before coding)
3. Implement code to make test pass
4. Refactor if needed while keeping tests passing
5. Merge only when tests pass and coverage meets threshold

**Test Organization**:

- `tests/contract/` - API contract tests (request/response validation)
- `tests/integration/` - Full workflow tests (database + API + services)
- `tests/unit/` - Service/repository unit tests
- Frontend tests: Component rendering, state changes, user interactions

**Rationale**: TDD enforces thinking through requirements before coding, prevents regression bugs, documents intended behavior through tests, enables safe refactoring.

---

### III. Security by Default

Security is not added after the fact; it is built into every feature from day one.

**Authentication & Authorization**:

- All endpoints require authentication UNLESS explicitly marked as public
- JWT tokens must be validated on every request to protected endpoints
- Role-based access control (RBAC) must be enforced in authorization middleware
- Frontend must use ProtectedRoute components to prevent unauthorized access

**Data Protection**:

- No sensitive data (passwords, tokens, PII) in logs
- Sensitive fields must be encrypted at rest (AES-256)
- All data in transit must use HTTPS (TLS 1.2+)
- API keys and credentials must use environment variables, never hardcoded

**Input Validation & SQL Injection Prevention**:

- Backend MUST re-validate all frontend input (never trust frontend validation)
- Use parameterized queries ALWAYS (Entity Framework prevents this automatically)
- Never concatenate user input into SQL strings
- Whitelist allowed input patterns; don't blacklist

**Password Requirements**:

- Minimum 8 characters, mixed case, numbers, special characters
- Hashed with bcrypt (cost factor ≥12) server-side
- No dictionary words or common patterns
- Password history tracked (users cannot reuse last 3 passwords)

**Rate Limiting**:

- Login attempts: 5 per 15 minutes, then 10-minute lockout
- API requests: 100 per minute per user (tuned per endpoint)
- Return 429 Too Many Requests on limit exceeded

**Rationale**: Security breaches are costly and damage user trust. Proactive security design prevents most common attack vectors and aligns with OWASP guidelines.

---

### IV. Database Migrations Always

All database schema changes MUST go through Entity Framework Core migrations. Never modify the database directly.

**Requirements**:

- Schema changes only through `dotnet ef migrations add [MigrationName]`
- Migrations stored in `src/backend/Data/Migrations/`
- Every migration must include descriptive comment explaining the change
- Migrations must be reviewed and tested in local SQL Server before merge
- Rollback plan documented for production migrations
- Never drop columns/tables without data archival plan

**Migration Workflow**:

1. Modify EF Core model classes
2. Run: `dotnet ef migrations add DescriptiveNameHere`
3. Review generated migration code
4. Test locally: `dotnet ef database update`
5. Include migration file in PR for review
6. Test against production-like data before deployment

**Rationale**: Migrations enable version control of schema, enable rollback capability, document schema evolution, prevent data loss, enable audit trails, support CI/CD automation.

---

### V. Documentation Required

All code must include sufficient documentation for developers and future maintainers.

**API Documentation**:

- Every endpoint documented with request/response examples
- Include HTTP method, path, required/optional parameters
- Document authentication requirements
- Document error responses and status codes
- Example: Create README with API section showing curl examples

**Code Comments**:

- Complex business logic must include inline comments explaining WHY (not WHAT)
- Public methods include XML documentation (C#) or JSDoc (TypeScript)
- Regex patterns include comments explaining the pattern
- Non-obvious algorithms include high-level explanation

**React Components**:

- All components have PropTypes or TypeScript interfaces
- Component purpose documented in header comment
- Complex state changes explained
- Custom hooks documented with parameter/return types

**Project README**:

- Setup instructions: database creation, dependency installation, migration commands
- Local development: how to run frontend and backend
- Testing: how to run tests, expected coverage
- Architecture overview: high-level system design, component boundaries
- Deployment instructions: environment setup, configuration, deployment process

**Rationale**: Documentation enables onboarding, prevents knowledge silos, helps during incidents, ensures consistency across team, reduces context-switching costs.

---

## Development Workflow

### Code Review Requirements

- All code changes require peer review
- Reviewer must verify:
  - Tests written first (TDD pattern)
  - No business logic in controllers/components (Clean Architecture)
  - Security requirements met (authentication, authorization, input validation)
  - Documentation complete and accurate
  - Coverage meets 70% threshold for services/repositories
- Blocking issues: Architecture violations, missing tests, security gaps

### Quality Gates

**Before Merge**:

- ✅ Tests pass locally (`npm test` frontend, `dotnet test` backend)
- ✅ Code review approved
- ✅ Test coverage meets minimum thresholds
- ✅ No hardcoded secrets in code
- ✅ Documentation complete

**Before Deployment**:

- ✅ Integration tests pass in staging environment
- ✅ Migrations tested in staging database
- ✅ Security audit completed (CORS, HTTPS, rate limiting)
- ✅ Deployment plan documented and approved

---

## Governance

### Constitution Authority

This constitution supersedes all other project practices and guidelines. In case of conflict between this constitution and other documentation, this constitution prevails.

### Compliance Verification

- All pull requests must explicitly verify compliance with each principle
- Code review checklist includes constitution compliance items
- Violations are blocking and prevent merge
- Repeated violations result in pair programming with senior developer

### Amendment Process

Changes to constitution require:

1. **Proposal**: Document rationale for change
2. **Review**: Discuss with team, document trade-offs
3. **Approval**: Consensus from senior architects
4. **Documentation**: Update constitution with effective date
5. **Communication**: Announce to full team with migration plan for existing features
6. **Versioning**: Increment CONSTITUTION_VERSION following semantic versioning

**MAJOR**: Backward-incompatible changes to principles
**MINOR**: New principles or significant clarifications
**PATCH**: Clarifications, wording improvements, examples

### Runtime Development Guidance

For implementation details not covered by core principles, refer to:

- [`.github/copilot-instructions.md`](https://github.com/yourusername/ReactCore/blob/main/.github/copilot-instructions.md) - AI agent development patterns
- [`.specify/templates/spec-template.md`](https://github.com/yourusername/ReactCore/blob/main/.specify/templates/spec-template.md) - Specification format
- [`.specify/templates/plan-template.md`](https://github.com/yourusername/ReactCore/blob/main/.specify/templates/plan-template.md) - Technical planning
- [`.specify/templates/tasks-template.md`](https://github.com/yourusername/ReactCore/blob/main/.specify/templates/tasks-template.md) - Task breakdown format

---

**Version**: 1.0.0 | **Ratified**: 2025-12-13 | **Last Amended**: 2025-12-13
