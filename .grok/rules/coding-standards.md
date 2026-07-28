# C# / .NET Coding Standards

**Purpose**  
This document defines the coding standards for our .NET codebase. All code — whether written by humans or AI agents — must follow these rules to produce production-quality, robust, performant, and maintainable software.

**Scope**  
Applies to all new code and refactored code. Legacy code should be brought into compliance when touched.

**For AI Agents**  
When writing, reviewing, or refactoring C#/.NET code, you **must** follow every rule in this document. Prioritize correctness, robustness, readability, and long-term maintainability.

---

## 1. Target Framework & Tooling

- Target the **latest stable .NET version** or **latest stable - 1**.
- Use the **latest stable C# language version**.
- Enable **Nullable Reference Types** (`<Nullable>enable</Nullable>`) in all projects.
- Use a shared `.editorconfig` at the repository root.
- Treat warnings as errors in Release builds and CI.

---

## 2. General Design Principles

- **Robustness first**: Code must handle invalid input, nulls, and edge cases defensively.
- **Immutability preferred**: Favor records, readonly record structs, and init-only properties.
- **Composition over inheritance**.
- **Fail fast** with clear, meaningful exceptions or result types.
- Keep methods small and focused.
- Avoid magic strings and numbers.
- **No external libraries** unless explicitly called out in the design or requested by the user. Stick to the .NET Base Class Library (BCL) and Microsoft.Extensions packages that are already part of the project.

---

## 3. Naming Conventions

| Element              | Convention                          | Example                              |
|----------------------|-------------------------------------|--------------------------------------|
| Classes / Records    | PascalCase                          | `OrderService`, `UserCreatedEvent`   |
| Interfaces           | `I` + PascalCase                    | `IOrderRepository`                   |
| Methods              | PascalCase (verb + noun)            | `GetUserByIdAsync`, `CalculateTotal` |
| Properties           | PascalCase                          | `FirstName`, `IsActive`              |
| Private fields       | `_camelCase`                        | `_userRepository`                    |
| Local variables      | `camelCase`                         | `userId`, `orderTotal`               |
| Constants            | PascalCase                          | `MaxRetryCount`                      |
| File names           | Match primary type                  | `UserService.cs`                     |

- Use descriptive names. Avoid abbreviations except well-known ones (`Id`, `Db`).
- Boolean properties should start with `Is`, `Has`, `Can`, or `Should`.

---

## 4. Code Style Rules

### Braces (Always Required)
**Always** surround code blocks with braces, even when the block contains only one statement.

**Correct:**
```csharp
if (x > 0)
{
    throw new ArgumentException("x must be greater than zero");
}
```

**Incorrect:**
```csharp
if (x > 0)
    throw new ArgumentException("x must be greater than zero");
```

This rule applies to `if`, `else`, `for`, `foreach`, `while`, `do`, `switch`, `try`, `catch`, and `finally`.

### Never Use `goto`
The `goto` statement is **forbidden** in all code.

---

## 5. Class Member Ordering

Classes **must** follow this strict member ordering:

1. **Constructors** (first)
2. **Public members**
3. **Protected members** (wrapped in `#region protected`)
4. **Private members** (wrapped in `#region private`)

**Example:**

```csharp
public sealed class MyClass
{
    public MyClass()
    {
    }

    public int Calculate(int x)
    {
        // public implementation
    }

    #region protected

    protected virtual void OnCalculated()
    {
        // protected implementation
    }

    #endregion

    #region private

    private int _count;

    private void InternalHelper()
    {
        // private implementation
    }

    #endregion
}
```

This ordering and region usage must be followed consistently across the codebase.

---

## 6. Modern C# Language Features (Strongly Preferred)

Use modern C# features aggressively:

- `record` and `readonly record struct` for DTOs and value objects.
- Primary constructors.
- Pattern matching and `switch` expressions.
- `required` properties and `init` setters.
- `nameof()` instead of magic strings.
- Collection expressions and spread syntax.
- `Span<T>` / `ReadOnlySpan<T>` in performance-sensitive code.
- Full `async` / `await` usage for all I/O.
- `using` declarations and statements for disposables.
- `sealed` classes by default unless inheritance is required.

---

## 7. Error Handling & Resilience

- Never swallow exceptions silently.
- Use structured logging via `ILogger<T>`.
- Prefer clear, specific exceptions (`ArgumentNullException`, `InvalidOperationException`, etc.).
- Use `ArgumentNullException.ThrowIfNull()` and similar helpers.
- Log at the appropriate level with sufficient context.
- Implement retry or resilience logic manually when needed (do not introduce external resilience libraries unless explicitly approved).

---

## 8. Asynchronous Programming

- All I/O-bound work **must** be asynchronous.
- Always accept and propagate `CancellationToken`.
- Prefer `ValueTask<T>` when the operation frequently completes synchronously.
- Never use `.Result` or `.Wait()`.
- Use `ConfigureAwait(false)` appropriately in library code.

---

## 9. Performance & Memory

- Be mindful of allocations in hot paths.
- Use `Span<T>`, `ReadOnlySpan<T>`, and `stackalloc` where appropriate and safe.
- Use `ArrayPool<T>` and `MemoryPool<T>` for high-frequency buffer scenarios.
- Avoid multiple enumerations of `IEnumerable<T>` in performance-critical code.
- Measure before optimizing. Readability and correctness take priority unless profiling demonstrates a clear need.

---

## 10. Collections

- Prefer `IReadOnlyList<T>`, `IReadOnlyCollection<T>`, or `IEnumerable<T>` in public APIs when mutation is not required.
- Use immutable collections from the BCL when they are already available in the project.
- Be intentional when calling `ToList()` or `ToArray()`.

---

## 11. Dependency Injection

- Use constructor injection only.
- Keep constructors simple.
- Prefer small, focused interfaces (Interface Segregation).
- Register services with appropriate lifetimes (`Scoped` is typical for web applications).

---

## 12. Security

- Validate all external input.
- Never concatenate SQL or use string interpolation for queries.
- Never store secrets in source code or committed configuration files.
- Follow secure coding practices for authentication, authorization, and data protection.

---

## 13. Testing

- Write meaningful tests for public behavior.
- Use clear test naming: `MethodName_StateUnderTest_ExpectedBehavior`.
- Follow Arrange-Act-Assert pattern.
- Mock only what is necessary.

---

## 14. Documentation & Comments

- Write self-documenting code as the primary goal.
- Use XML documentation (`///`) on all public APIs.
- Add comments only to explain *why*, not *what*.
- Keep comments current — remove outdated ones.

---

## 15. Conflict Resolution

When rules appear to conflict, apply this priority:

1. **Correctness & Robustness**
2. **Readability & Maintainability**
3. **Performance** (only when justified by measurement)

---

**Last Updated**: July 2026  
**Review Cadence**: Update this document when new language features or team decisions require it.
**Version**: 1.0