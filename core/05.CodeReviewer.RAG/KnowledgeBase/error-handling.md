# Error Handling Best Practices in C#

## Exception Handling Guidelines

### Catch Specific Exceptions
Always catch specific exceptions instead of generic Exception when possible.

**Bad Example:**
```csharp
try {
    // some operation
} catch (Exception ex) {
    // handles everything
}
```

**Good Example:**
```csharp
try {
    await httpClient.GetAsync(url);
} catch (HttpRequestException ex) {
    _logger.LogError(ex, "HTTP request failed");
} catch (TaskCanceledException ex) {
    _logger.LogWarning("Request was cancelled");
}
```

### Don't Swallow Exceptions
Never catch an exception and do nothing with it. At minimum, log it.

### Use Finally for Cleanup
Use finally blocks or `using` statements for resource cleanup.

```csharp
using var connection = new SqlConnection(connectionString);
// connection will be disposed automatically
```

### Custom Exceptions
Create custom exceptions for domain-specific errors.

```csharp
public class OrderNotFoundException : Exception {
    public int OrderId { get; }
    public OrderNotFoundException(int orderId)
        : base($"Order {orderId} was not found") {
        OrderId = orderId;
    }
}
```

### Validation vs Exceptions
Use validation for expected errors, exceptions for unexpected situations.

```csharp
// Validation - expected
if (string.IsNullOrEmpty(email))
    return Result.Fail("Email is required");

// Exception - unexpected
throw new InvalidOperationException("Database connection lost");
```

### Global Exception Handling
In ASP.NET Core, use middleware for global exception handling.

```csharp
app.UseExceptionHandler("/error");
```
