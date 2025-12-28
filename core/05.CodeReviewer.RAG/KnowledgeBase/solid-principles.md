# SOLID Principles

## Single Responsibility Principle (SRP)
A class should have only one reason to change. Each class should focus on a single task or responsibility.

**Bad Example:**
```csharp
public class User {
    public void SaveToDatabase() { }
    public void SendEmail() { }
    public void GenerateReport() { }
}
```

**Good Example:**
```csharp
public class User { /* user data only */ }
public class UserRepository { public void Save(User user) { } }
public class EmailService { public void Send(string to, string message) { } }
```

## Open/Closed Principle (OCP)
Classes should be open for extension but closed for modification. Use inheritance and interfaces.

**Good Example:**
```csharp
public interface IPaymentProcessor { void Process(decimal amount); }
public class CreditCardProcessor : IPaymentProcessor { }
public class PayPalProcessor : IPaymentProcessor { }
```

## Liskov Substitution Principle (LSP)
Derived classes must be substitutable for their base classes without breaking the application.

## Interface Segregation Principle (ISP)
Clients should not be forced to depend on interfaces they don't use. Prefer small, focused interfaces.

**Bad Example:**
```csharp
public interface IWorker {
    void Work();
    void Eat();
    void Sleep();
}
```

**Good Example:**
```csharp
public interface IWorkable { void Work(); }
public interface IEatable { void Eat(); }
```

## Dependency Inversion Principle (DIP)
High-level modules should not depend on low-level modules. Both should depend on abstractions.

**Good Example:**
```csharp
public class OrderService {
    private readonly IOrderRepository _repository;
    public OrderService(IOrderRepository repository) {
        _repository = repository;
    }
}
```
