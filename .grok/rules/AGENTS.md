# AGENTS.md

**Critical: Read this file and `.grok/rules/coding-standards.md` before making any changes.**

---

## 1. Core Instructions for AI Agents

You **must** follow these rules on every task:

1. **Read the standards first**
   - Always read `.grok/rules/coding-standards.md` before writing or modifying any C#/.NET code.
   - Follow it strictly (especially member ordering, braces rule, no `goto`, no unauthorized external libraries, and modern C# preferences).

2. **Production Quality Mindset**
   - Prioritize **robustness**, **correctness**, and **maintainability** over cleverness or micro-optimizations.
   - Write code that is safe, defensive, and easy to understand six months from now.
   - Fail fast with clear errors when something is wrong.

3. **No External Libraries**
   - Do **not** introduce new NuGet packages or external libraries unless explicitly requested or already approved in the design.
   - Stick to the .NET BCL and existing Microsoft.Extensions packages in the solution.

---

## 2. Technology Stack

- **Language**: C# (latest stable or latest stable - 1)
- **Platform**: .NET (latest stable or latest stable - 1)
- **Nullable Reference Types**: Enabled in all projects
- **Architecture**: Hybrid Vertical Slice / Clean Architecture
- **Primary Patterns**: Constructor injection, records for DTOs/value objects, async/await for I/O

---

## 3. Development Workflow

### Before Writing Code
- Understand the existing structure and patterns in the codebase.
- Check for similar implementations before creating new ones.
- Prefer extending or improving existing code over duplicating logic.

### When Making Changes
- Follow the exact member ordering defined in `.grok/rules/coding-standards.md`.
- Always use braces for code blocks, even single-line statements.
- Use modern C# features (records, primary constructors, pattern matching, etc.).
- Keep methods small and focused.
- Add or update XML documentation on public APIs when appropriate.

### Testing & Validation
- After making changes that affect behavior, **run the relevant tests**.
- Prefer adding or updating tests when modifying logic.
- Never suggest changes that would break existing tests without good reason.

### Build & Test Commands

Use these commands (adjust paths as needed for your solution):

```bash
# Build the entire solution
dotnet build

# Run all tests
dotnet test

# Run tests for a specific project
dotnet test MyProject.Tests/

# Run with coverage (if configured)
dotnet test --collect:"XPlat Code Coverage"
```

- Always verify that `dotnet build` succeeds with no warnings before considering a change complete.

---

## 4. Error Handling & Logging

- Use structured logging via `ILogger<T>`.
- Throw specific, meaningful exceptions.
- Never swallow exceptions silently.
- Propagate `CancellationToken` in all async methods.

---

## 5. Git & Change Management

- Make focused, atomic changes.
- Write clear commit messages that explain *why* the change was made.
- Group related changes logically.
- Do not commit generated files, secrets, or build artifacts.

---

## 6. When to Ask for Clarification

Ask before proceeding if:
- The request is ambiguous or could be interpreted in multiple ways.
- A change would require introducing a new external library.
- You're unsure about the architectural direction or existing patterns.
- The task involves significant refactoring of core components.

---

## 7. Additional Rule Files

This project may contain additional rule files in `.grok/rules/`:

- `coding-standards.md` ← **Primary reference** (read this first)
- `architecture.md` (if present)
- Other domain-specific rules

Always check the `.grok/rules/` directory for the latest guidance.

---

**Remember**: Your goal is to produce clean, robust, production-ready C# code that follows the standards defined in this repository.

_Last Updated: July 2026_
_Version: 1.0_
