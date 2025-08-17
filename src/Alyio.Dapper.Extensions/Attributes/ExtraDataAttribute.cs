// MIT License

namespace Alyio.Dapper.Extensions;

/// <summary>
/// Marks a property to capture extra, unmapped columns when mapping data.
/// The target property must be compatible with type <see cref="IDictionary{TKey, TValue}"/> while the key is <see cref="string"/> and the value is <see cref="object"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class ExtraDataAttribute : Attribute
{
}
