# Data Access Layer - Design Decisions

## Entity vs DTO Pattern

This project uses a clear separation between **Domain Entities** and **Data Transfer Objects (DTOs)**:

### Domain Entities (ExpenseTracker.Core)
Located in `ExpenseTracker.Core/Domain/Entities/`, these classes represent the business domain with proper .NET types:

```csharp
public class Category
{
    public bool IsDefault { get; set; }  // Business type: bool
}

public class Expense
{
    public DateTime Date { get; set; }    // Business type: DateTime
    public ExpenseSource Source { get; set; }  // Business type: enum
}
```

### Repository DTOs (ExpenseTracker.Infrastructure)
Each repository defines a private DTO record that matches SQLite's type system:

```csharp
private record ExpenseDto(
    string Id,
    string UserId,
    double Amount,      // SQLite REAL → C# double (not decimal!)
    string Description,
    string CategoryId,
    string Date,        // SQLite TEXT → C# string (ISO 8601)
    string? Location,
    string? Notes,
    long Source,        // SQLite INTEGER → C# long
    string CreatedAt,
    string? ModifiedAt
);
```

### Why This Separation?

**SQLite Type Affinity:**
- SQLite stores all integers as `Int64` (C# `long`)
- SQLite stores REAL as `Double` (C# `double`), not `decimal`
- SQLite has no native boolean type (uses 0/1 integers)
- SQLite has no native DateTime type (we store as ISO 8601 strings)

**Dapper Materialization:**
Dapper requires exact type matching for positional records. When SQLite returns an `INTEGER`, Dapper expects `long`, not `int` or `bool`.

**Mapping Functions:**
Each repository includes a `MapToEntity` function that converts database types to business types:

```csharp
private static Expense MapToEntity(ExpenseDto dto) => new()
{
    Id = dto.Id,
    UserId = dto.UserId,
    Amount = (decimal)dto.Amount,           // double → decimal
    Date = DateTime.Parse(dto.Date),        // string → DateTime
    Source = (ExpenseSource)(int)dto.Source // long → int → enum
};
```

## Design Benefits

1. **Clean Domain Model**: Business entities use intuitive types (`bool`, `DateTime`, enums)
2. **Database Abstraction**: DTOs handle database-specific quirks
3. **Type Safety**: Compile-time checking for both layers
4. **Testability**: Domain entities can be tested without database concerns

## Alternative Approaches

For production applications, consider:

1. **Dapper Type Handlers**: Register custom handlers for bool/DateTime mapping
2. **Source Generators**: Auto-generate DTOs from entities
3. **Entity Framework Core**: Handles type conversions automatically

This project uses explicit DTOs for educational purposes, making the mapping visible and understandable.

---
*This is a design decision documented for the book chapter on AI Agents with C#.*
