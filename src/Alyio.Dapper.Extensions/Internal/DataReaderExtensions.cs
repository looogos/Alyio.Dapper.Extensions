// MIT License

using System.Data;
using System.Globalization;

namespace Alyio.Dapper.Extensions;

/// <summary>
/// Provides extension methods for <see cref="IDataReader"/> that enhance Dapper's query capabilities
/// with support for robust type conversion and column type inference.
/// </summary>
internal static class DataReaderExtensions
{
    /// <summary>
    /// Gets the value of the specified column and robustly converts it to the target .NET type.
    /// </summary>
    public static object? GetClrValue(this IDataReader reader, string columnName, Type? targetType = null)
    {
        int columnIndex = reader.GetOrdinal(columnName);
        object dbValue = reader.GetValue(columnIndex);

        if (dbValue == DBNull.Value)
        {
            if (targetType != null && targetType.IsValueType && Nullable.GetUnderlyingType(targetType) == null)
            {
                return Activator.CreateInstance(targetType);
            }

            return null;
        }

        Type effectiveTargetType = targetType ?? reader.GetEffectiveType(columnIndex);

        if (effectiveTargetType == null)
        {
            return dbValue;
        }

        Type conversionType = Nullable.GetUnderlyingType(effectiveTargetType) ?? effectiveTargetType;

        if (conversionType.IsInstanceOfType(dbValue))
        {
            return dbValue;
        }

        if (conversionType.IsAssignableTo(typeof(IConvertible)))
        {
            try
            {
                return Convert.ChangeType(dbValue, conversionType, CultureInfo.InvariantCulture);
            }
            catch (Exception ex)
            {
                throw new InvalidCastException($"Failed to convert database value of type '{dbValue.GetType().FullName}' to target type '{effectiveTargetType.FullName}'.", ex);
            }
        }

        if (dbValue is string dbValueString)
        {
            return conversionType.Name switch
            {
                "Guid" => Guid.Parse(dbValueString),
                _ => throw new NotSupportedException($"Failed to convert database value of type '{dbValue.GetType().FullName}' to target type '{effectiveTargetType.FullName}'."),
            };
        }

        throw new NotSupportedException($"Failed to convert database value of type '{dbValue.GetType().FullName}' to target type '{effectiveTargetType.FullName}'.");
    }

    /// <summary>
    /// Gets the effective type of the column at the specified index.
    /// </summary>
    /// <param name="reader">The data reader to get the column type from.</param>
    /// <param name="columnIndex">The index of the column to get the type for.</param>
    /// <returns>The effective type of the column in heuristic mapping.</returns>
    private static Type GetEffectiveType(this IDataReader reader, int columnIndex)
    {
        string dataTypeName = reader.GetDataTypeName(columnIndex).ToUpperInvariant();

        return dataTypeName switch
        {
            "TIME" => typeof(TimeSpan),
            "DATE" or "DATETIME" => typeof(DateTime),
            var s when s.Contains("BOOL") || s.Contains("BIT") => typeof(bool),
            var s when s.Contains("GUID") || s == "UNIQUEIDENTIFIER" => typeof(Guid),
            var s when s.Contains("DECIMAL") || s.Contains("NUMERIC") || s.Contains("MONEY") => typeof(decimal),
            _ => reader.GetFieldType(columnIndex),
        };
    }
}
