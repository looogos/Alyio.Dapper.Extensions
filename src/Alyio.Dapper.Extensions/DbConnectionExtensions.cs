// MIT License

using System.Data;
using System.Reflection;
using Dapper;

namespace Alyio.Dapper.Extensions;

/// <summary>
/// Provides extension methods for database connections that enhance Dapper's query capabilities
/// with support for collecting unmapped columns into a dictionary property.
/// </summary>
public static class DbConnectionExtensions
{
    /// <summary>
    /// Executes a query and maps the result to a sequence of <typeparamref name="T"/> objects.
    /// Unmapped columns are collected into a property marked with <see cref="ExtraDataAttribute"/> as a dictionary.
    /// </summary>
    /// <typeparam name="T">The type to map the result to.</typeparam>
    /// <param name="cnn">The database connection.</param>
    /// <param name="sql">The SQL query to execute.</param>
    /// <param name="param">The parameters for the SQL query.</param>
    /// <param name="transaction">The transaction to use.</param>
    /// <param name="commandTimeout">The command timeout in seconds.</param>
    /// <param name="commandType">The type of the command.</param>
    /// <returns>A sequence of mapped objects with extra data collected.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if more than one property is marked with <see cref="ExtraDataAttribute"/>,
    /// or if the marked property is not of type <c>IDictionary&lt;string, object?&gt;</c>.
    /// </exception>
    public static IEnumerable<T> QueryWithExtra<T>(
        this IDbConnection cnn,
        string sql,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null) => QueryWithExtra<T>(cnn, new CommandDefinition(sql, param, transaction, commandTimeout, commandType, CommandFlags.Buffered, default));

    /// <summary>
    /// Executes a query and maps the result to a sequence of <typeparamref name="T"/> objects.
    /// Unmapped columns are collected into a property marked with <see cref="ExtraDataAttribute"/> as a dictionary.
    /// </summary>
    /// <typeparam name="T">The type to map the result to.</typeparam>
    /// <param name="cnn">The database connection.</param>
    /// <param name="command">The command definition for the query.</param>
    /// <returns>A sequence of mapped objects with extra data collected.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if more than one property is marked with <see cref="ExtraDataAttribute"/>,
    /// or if the marked property is not of type <c>IDictionary&lt;string, object?&gt;</c>.
    /// </exception>
    public static IEnumerable<T> QueryWithExtra<T>(this IDbConnection cnn, CommandDefinition command)
    {
        PropertyInfo? extraDataProperty = GetExtraDataProperty<T>();

        if (extraDataProperty == null)
        {
            return cnn.Query<T>(command);
        }

        using IDataReader reader = cnn.ExecuteReader(command);

        return QueryWithExtraCore<T>(extraDataProperty, reader);
    }

    /// <summary>
    /// Executes a query asynchronously and maps the result to a sequence of <typeparamref name="T"/> objects.
    /// Unmapped columns are collected into a property marked with <see cref="ExtraDataAttribute"/> as a dictionary.
    /// </summary>
    /// <typeparam name="T">The type to map the result to.</typeparam>
    /// <param name="cnn">The database connection.</param>
    /// <param name="sql">The SQL query to execute.</param>
    /// <param name="param">The parameters for the SQL query.</param>
    /// <param name="transaction">The transaction to use.</param>
    /// <param name="commandTimeout">The command timeout in seconds.</param>
    /// <param name="commandType">The type of the command.</param>
    /// <returns>An asynchronous sequence of mapped objects with extra data collected.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if more than one property is marked with <see cref="ExtraDataAttribute"/>,
    /// or if the marked property is not of type <c>IDictionary&lt;string, object?&gt;</c>.
    /// </exception>
    public static Task<IEnumerable<T>> QueryWithExtraAsync<T>(
        this IDbConnection cnn,
        string sql,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null) => QueryWithExtraAsync<T>(cnn, new CommandDefinition(sql, param, transaction, commandTimeout, commandType, CommandFlags.Buffered, default));

    /// <summary>
    /// Executes a query asynchronously and maps the result to a sequence of <typeparamref name="T"/> objects.
    /// Unmapped columns are collected into a property marked with <see cref="ExtraDataAttribute"/> as a dictionary.
    /// </summary>
    /// <typeparam name="T">The type to map the result to.</typeparam>
    /// <param name="cnn">The database connection.</param>
    /// <param name="command">The command definition for the query.</param>
    /// <returns>An asynchronous sequence of mapped objects with extra data collected.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if more than one property is marked with <see cref="ExtraDataAttribute"/>,
    /// or if the marked property is not of type <c>IDictionary&lt;string, object?&gt;</c>.
    /// </exception>
    public static async Task<IEnumerable<T>> QueryWithExtraAsync<T>(this IDbConnection cnn, CommandDefinition command)
    {
        PropertyInfo? extraDataProperty = GetExtraDataProperty<T>();

        if (extraDataProperty == null)
        {
            return await cnn.QueryAsync<T>(command);
        }

        using IDataReader reader = await cnn.ExecuteReaderAsync(command);

        return QueryWithExtraCore<T>(extraDataProperty, reader);
    }

    private static List<T> QueryWithExtraCore<T>(PropertyInfo extraDataProperty, IDataReader reader)
    {
        var columnNames = new List<string>();
        for (int i = 0; i < reader.FieldCount; i++)
        {
            columnNames.Add(reader.GetName(i));
        }

        var defaultTypeMap = new DefaultTypeMap(typeof(T));
        string[] extraColumnNames = columnNames.Where(fn => defaultTypeMap.GetMember(fn) == null).ToArray();
        string[] directColumnNames = columnNames.Where(fn => defaultTypeMap.GetMember(fn) != null).ToArray();

        var result = new List<T>();
        while (reader.Read())
        {
            T? instance = Activator.CreateInstance<T>();
            foreach (string columnName in directColumnNames)
            {
                SqlMapper.IMemberMap? memberMap = defaultTypeMap.GetMember(columnName);
                if (memberMap != null)
                {
                    object? value = reader.GetClrValue(memberMap.ColumnName, memberMap.MemberType);
                    memberMap.Property?.SetValue(instance, value);
                }
            }

            if (extraColumnNames.Length > 0)
            {
                var extraData = new Dictionary<string, object?>();
                foreach (string columnName in extraColumnNames)
                {
                    extraData[columnName] = reader.GetClrValue(columnName);
                }

                extraDataProperty.SetValue(instance, extraData);
            }

            result.Add(instance);
        }

        return result;
    }

    private static PropertyInfo? GetExtraDataProperty<T>()
    {
        PropertyInfo[] extraDataProperties = typeof(T).GetProperties().Where(p => p.GetCustomAttribute<ExtraDataAttribute>() != null).ToArray();

        if (extraDataProperties.Length > 1)
        {
            throw new InvalidOperationException("Only one property can be marked with [ExtraData]");
        }

        PropertyInfo? extraDataProperty = extraDataProperties.FirstOrDefault();
        if (extraDataProperty != null && !typeof(IDictionary<string, object?>).IsAssignableFrom(extraDataProperty.PropertyType))
        {
            throw new InvalidOperationException("ExtraData property must be a dictionary of type IDictionary<string, object>");
        }

        return extraDataProperty;
    }
}
