# Alyio.Dapper.Extensions

[![NuGet](https://img.shields.io/nuget/v/Alyio.Dapper.Extensions.svg)](https://www.nuget.org/packages/Alyio.Dapper.Extensions)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

A .NET library that extends Dapper's query capabilities with support for collecting unmapped columns into a dictionary property. Useful for dynamic schemas or capturing additional columns without corresponding entity properties.

## Features

-   **Extra Column Collection**: Automatically collect unmapped database columns into a dictionary property
-   **Robust Type Conversion**: Intelligent type conversion with support for various database types
-   **Async Support**: Full async/await support for all query operations
-   **Type Safety**: Compile-time validation of extra data property types
-   **Performance Optimized**: Efficient data reader implementation with minimal overhead
-   **Cross-Platform**: Supports .NET 6.0, 8.0, 9.0, and 10.0

## Installation

### NuGet Package

```bash
dotnet add package Alyio.Dapper.Extensions
```

### Package Manager Console

```powershell
Install-Package Alyio.Dapper.Extensions
```

## Quick Start

### 1. Define the Entity

Create a class with a property marked with the `[ExtraData]` attribute to capture unmapped columns:

```csharp
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }

    [ExtraData]
    public IDictionary<string, object?>? ExtraData { get; set; }
}
```

### 2. Use the Extension Methods

Use `QueryWithExtra<T>()` or `QueryWithExtraAsync<T>()` instead of Dapper's standard `Query<T>()` or `QueryAsync<T>()`:

```csharp
using Alyio.Dapper.Extensions;
using Dapper;

// Synchronous query
var users = connection.QueryWithExtra<User>("SELECT * FROM Users");

// Asynchronous query
var users = await connection.QueryWithExtraAsync<User>("SELECT * FROM Users");
```

### 3. Access Extra Data

Unmapped columns are automatically collected in the `ExtraData` dictionary:

```csharp
foreach (var user in users)
{
    Console.WriteLine($"User: {user.Name}");

    if (user.ExtraData != null)
    {
        foreach (var extra in user.ExtraData)
        {
            Console.WriteLine($"  {extra.Key}: {extra.Value}");
        }
    }
}
```

## Detailed Examples

### Basic Usage

```csharp
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }

    [ExtraData]
    public IDictionary<string, object?>? ExtraData { get; set; }
}

// Database table has additional columns: CreatedDate, Category, Tags
var products = await connection.QueryWithExtraAsync<Product>("SELECT * FROM Products");

foreach (var product in products)
{
    Console.WriteLine($"Product: {product.Name}");

    // Access extra columns
    if (product.ExtraData != null)
    {
        var createdDate = (DateTime)product.ExtraData["CreatedDate"];
        var category = (string)product.ExtraData["Category"];
        var tags = (string)product.ExtraData["Tags"];

        Console.WriteLine($"  Created: {createdDate}");
        Console.WriteLine($"  Category: {category}");
        Console.WriteLine($"  Tags: {tags}");
    }
}
```

### Entity Without Extra Data

If an entity doesn't have an `[ExtraData]` property, the library behaves like standard Dapper:

```csharp
public class SimpleUser
{
    public int Id { get; set; }
    public string Name { get; set; }
}

// Extra columns are ignored, only mapped properties are populated
var users = await connection.QueryWithExtraAsync<SimpleUser>("SELECT * FROM Users");
```

## Error Handling

The library throws specific exceptions for common configuration errors:

### Multiple ExtraData Properties

```csharp
public class InvalidEntity
{
    [ExtraData]
    public IDictionary<string, object?>? Extra1 { get; set; }

    [ExtraData] // ❌ Throws InvalidOperationException
    public IDictionary<string, object?>? Extra2 { get; set; }
}
```

### Invalid Property Type

```csharp
public class InvalidEntity
{
    [ExtraData]
    public List<string>? ExtraData { get; set; } // ❌ Must be IDictionary<string, object?>
}
```

## Contributing

Contributions are welcome. Submit a Pull Request for minor changes. For major changes, open an issue first to discuss the proposed changes.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Dependencies

-   **Dapper**: 2.1.66 or higher
-   **.NET**: 6.0, 8.0, 9.0, or 10.0

## Related Projects

-   [Dapper](https://github.com/DapperLib/Dapper) - The micro ORM this library extends
-   [Microsoft.Data.Sqlite](https://github.com/dotnet/efcore) - SQLite provider for .NET

## Support

For issues or questions:

1. Check the [existing issues](https://github.com/looogos/Alyio.Dapper.Extensions/issues)
2. Create a new issue with a detailed description
3. Include a minimal reproduction example if possible
