# Async/Await Best Practices in C#

## Always Use Async All the Way
Don't mix synchronous and asynchronous code. Go async from top to bottom.

**Bad Example:**
```csharp
public void ProcessOrder() {
    var result = GetDataAsync().Result; // Blocks thread!
}
```

**Good Example:**
```csharp
public async Task ProcessOrderAsync() {
    var result = await GetDataAsync();
}
```

## Use ConfigureAwait(false) in Libraries
In library code, use ConfigureAwait(false) to avoid deadlocks.

```csharp
public async Task<string> GetDataAsync() {
    var data = await _httpClient.GetAsync(url).ConfigureAwait(false);
    return await data.Content.ReadAsStringAsync().ConfigureAwait(false);
}
```

## Avoid async void
Only use async void for event handlers. Always return Task or Task<T>.

**Bad Example:**
```csharp
public async void SaveDataAsync() { } // Can't be awaited, exceptions lost
```

**Good Example:**
```csharp
public async Task SaveDataAsync() { }
```

## Use ValueTask for Hot Paths
Use ValueTask when the result is often available synchronously.

```csharp
public ValueTask<int> GetCachedValueAsync(string key) {
    if (_cache.TryGetValue(key, out int value))
        return new ValueTask<int>(value);
    return new ValueTask<int>(LoadFromDatabaseAsync(key));
}
```

## Cancellation Tokens
Always accept and use CancellationToken for long-running operations.

```csharp
public async Task ProcessAsync(CancellationToken cancellationToken = default) {
    await foreach (var item in GetItemsAsync(cancellationToken)) {
        cancellationToken.ThrowIfCancellationRequested();
        await ProcessItemAsync(item, cancellationToken);
    }
}
```

## Parallel Operations
Use Task.WhenAll for independent parallel operations.

```csharp
var tasks = items.Select(item => ProcessItemAsync(item));
await Task.WhenAll(tasks);
```
