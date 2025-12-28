# C# Naming Conventions

## Classes and Structs
- Use PascalCase for class and struct names
- Names should be nouns or noun phrases
- Avoid prefixes like "C" or "cls"
- Examples: `CustomerService`, `OrderProcessor`, `PaymentGateway`

## Interfaces
- Use PascalCase with "I" prefix
- Names should describe behavior or capability
- Examples: `IDisposable`, `IComparable`, `IOrderRepository`

## Methods
- Use PascalCase for method names
- Names should be verbs or verb phrases
- Be descriptive about what the method does
- Examples: `CalculateTotal()`, `SendEmail()`, `ValidateInput()`

## Variables and Parameters
- Use camelCase for local variables and parameters
- Be descriptive, avoid single letters except for loops
- Examples: `customerName`, `orderTotal`, `isValid`

## Private Fields
- Use camelCase with underscore prefix
- Examples: `_customerRepository`, `_logger`, `_connectionString`

## Constants
- Use PascalCase for constants
- Consider using static readonly for complex types
- Examples: `MaxRetryCount`, `DefaultTimeout`, `ApiVersion`

## Async Methods
- Always suffix async methods with "Async"
- Examples: `GetCustomerAsync()`, `SaveOrderAsync()`, `SendEmailAsync()`
