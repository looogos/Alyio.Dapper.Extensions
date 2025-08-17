// MIT License

using System.Data;
using System.Globalization;
using Alyio.Dapper.Extensions;
using Moq;

namespace Alyio.Dapper.Extension.Tests;

public sealed class DataReaderExtensionsTests
{
    #region Test Data

    public static TheoryData<string, Type, object, Type, object> BasicTypeConversionTestData => new()
    {
        // SQL Server
        { "datetimeoffset", typeof(DateTimeOffset), new DateTimeOffset(2023, 8, 16, 12, 34, 56, TimeSpan.FromHours(5)), typeof(DateTimeOffset), new DateTimeOffset(2023, 8, 16, 12, 34, 56, TimeSpan.FromHours(5)) },
        { "datetime2", typeof(DateTime), new DateTime(2023, 8, 16, 12, 34, 56), typeof(DateTime), new DateTime(2023, 8, 16, 12, 34, 56) },
        { "datetime", typeof(DateTime), new DateTime(2023, 8, 16, 12, 34, 56), typeof(DateTime), new DateTime(2023, 8, 16, 12, 34, 56) },
        { "bit", typeof(bool), true, typeof(bool), true },
        { "uniqueidentifier", typeof(Guid), Guid.Parse("01234567-89ab-cdef-0123-456789abcdef"), typeof(Guid), Guid.Parse("01234567-89ab-cdef-0123-456789abcdef") },
        { "money", typeof(decimal), 123.45m, typeof(decimal), 123.45m },
        { "int", typeof(int), 42, typeof(int), 42 },
        { "float", typeof(double), 3.14, typeof(double), 3.14 },
        { "binary", typeof(byte[]), new byte[] { 1, 2, 3 }, typeof(byte[]), new byte[] { 1, 2, 3 } },
        { "nvarchar", typeof(string), "hello", typeof(string), "hello" },

        // MySQL
        { "datetime", typeof(DateTime), new DateTime(2023, 8, 16, 12, 34, 56), typeof(DateTime), new DateTime(2023, 8, 16, 12, 34, 56) },
        { "timestamp", typeof(DateTime), new DateTime(2023, 8, 16, 12, 34, 56), typeof(DateTime), new DateTime(2023, 8, 16, 12, 34, 56) },
        { "tinyint", typeof(sbyte), (sbyte)1, typeof(sbyte), (sbyte)1 },
        { "decimal", typeof(decimal), 123.45m, typeof(decimal), 123.45m },
        { "int", typeof(int), 42, typeof(int), 42 },
        { "float", typeof(float), 3.14f, typeof(float), 3.14f },
        { "blob", typeof(byte[]), new byte[] { 1, 2, 3 }, typeof(byte[]), new byte[] { 1, 2, 3 } },
        { "varchar", typeof(string), "hello", typeof(string), "hello" },

        // PostgreSQL
        { "timestamp", typeof(DateTime), new DateTime(2023, 8, 16, 12, 34, 56), typeof(DateTime), new DateTime(2023, 8, 16, 12, 34, 56) },
        { "bool", typeof(bool), true, typeof(bool), true },
        { "uuid", typeof(Guid), Guid.Parse("01234567-89ab-cdef-0123-456789abcdef"), typeof(Guid), Guid.Parse("01234567-89ab-cdef-0123-456789abcdef") },
        { "numeric", typeof(decimal), 123.45m, typeof(decimal), 123.45m },
        { "bytea", typeof(byte[]), new byte[] { 1, 2, 3 }, typeof(byte[]), new byte[] { 1, 2, 3 } },
        { "integer", typeof(int), 42, typeof(int), 42 },
        { "real", typeof(float), 3.14f, typeof(float), 3.14f },
        { "text", typeof(string), "hello", typeof(string), "hello" },

        // SQLite
        { "DATE", typeof(DateTime), new DateTime(2023, 8, 16), typeof(DateTime), new DateTime(2023, 8, 16) },
        { "DATETIME", typeof(string), "2023-08-16 12:34:56", typeof(DateTime), new DateTime(2023, 8, 16, 12, 34, 56) },
        { "BOOLEAN", typeof(long), 1L, typeof(bool), true },
        { "REAL", typeof(double), 3.14d, typeof(double), 3.14d },
        { "NUMERIC", typeof(double), 123.456, typeof(decimal), 123.456m },
        { "BLOB", typeof(byte[]), new byte[] { 1, 2, 3 }, typeof(byte[]), new byte[] { 1, 2, 3 } },
        { "TEXT", typeof(string), "hello", typeof(string), "hello" },
        { "INTEGER", typeof(long), 42L, typeof(long), 42L },

        // ClickHouse
        { "DateTime", typeof(DateTime), new DateTime(2023, 8, 16, 12, 34, 56), typeof(DateTime), new DateTime(2023, 8, 16, 12, 34, 56) },
        { "UInt8", typeof(byte), (byte)1, typeof(byte), (byte)1 },
        { "Int32", typeof(int), 42, typeof(int), 42 },
        { "Float64", typeof(double), 3.14d, typeof(double), 3.14d },
        { "Decimal(18,2)", typeof(decimal), 123.45m, typeof(decimal), 123.45m },
        { "String", typeof(string), "hello", typeof(string), "hello" },
        { "UUID", typeof(Guid), Guid.Parse("01234567-89ab-cdef-0123-456789abcdef"), typeof(Guid), Guid.Parse("01234567-89ab-cdef-0123-456789abcdef") },
        { "Array(UInt8)", typeof(byte[]), new byte[] { 1, 2, 3 }, typeof(byte[]), new byte[] { 1, 2, 3 } },
    };

    public static TheoryData<string, Type, object> EdgeCaseTestData => new()
    {
        // Empty values
        { "varchar", typeof(string), "" },
        { "nvarchar", typeof(string), "" },
        { "text", typeof(string), "" },
        
        // Zero values
        { "int", typeof(int), 0 },
        { "bigint", typeof(long), 0L },
        { "decimal", typeof(decimal), 0m },
        { "float", typeof(double), 0.0 },
        
        // Boundary values
        { "int", typeof(int), int.MaxValue },
        { "int", typeof(int), int.MinValue },
        { "bigint", typeof(long), long.MaxValue },
        { "bigint", typeof(long), long.MinValue },
        { "decimal", typeof(decimal), decimal.MaxValue },
        { "decimal", typeof(decimal), decimal.MinValue },
        
        // Special string values
        { "varchar", typeof(string), "NULL" },
        { "varchar", typeof(string), "null" },
        { "varchar", typeof(string), " " },
        { "varchar", typeof(string), "\t\n\r" },
    };

    public static TheoryData<string, Type, object> TimeSpanTestData => new()
    {
        { "TIME", typeof(TimeSpan), TimeSpan.FromHours(12) },
        { "TIME", typeof(TimeSpan), TimeSpan.FromMinutes(30) },
        { "TIME", typeof(TimeSpan), TimeSpan.FromSeconds(45) },
        { "TIME", typeof(TimeSpan), TimeSpan.Zero },
    };

    #endregion

    #region Basic Type Conversion Tests

    [Theory]
    [MemberData(nameof(BasicTypeConversionTestData))]
    public void GetClrValue_MapsProviderTypesCorrectly(
        string dataTypeName,
        Type fieldType,
        object dbValue,
        Type expectedType,
        object expectedValue)
    {
        // Arrange
        var mockReader = new Mock<IDataReader>();
        mockReader.Setup(r => r.GetOrdinal("Col")).Returns(0);
        mockReader.Setup(r => r.GetValue(0)).Returns(dbValue);
        mockReader.Setup(r => r.GetDataTypeName(0)).Returns(dataTypeName);
        mockReader.Setup(r => r.GetFieldType(0)).Returns(fieldType);

        // Act
        object? value = mockReader.Object.GetClrValue("Col");

        // Assert
        Assert.IsType(expectedType, value);
        AssertValueEquality(expectedType, expectedValue, value);
    }

    [Theory]
    [MemberData(nameof(EdgeCaseTestData))]
    public void GetClrValue_HandlesEdgeCasesCorrectly(
        string dataTypeName,
        Type fieldType,
        object dbValue)
    {
        // Arrange
        var mockReader = new Mock<IDataReader>();
        mockReader.Setup(r => r.GetOrdinal("Col")).Returns(0);
        mockReader.Setup(r => r.GetValue(0)).Returns(dbValue);
        mockReader.Setup(r => r.GetDataTypeName(0)).Returns(dataTypeName);
        mockReader.Setup(r => r.GetFieldType(0)).Returns(fieldType);

        // Act
        object? value = mockReader.Object.GetClrValue("Col");

        // Assert
        Assert.NotNull(value);
        Assert.IsType(fieldType, value);
        Assert.Equal(dbValue, value);
    }

    [Theory]
    [MemberData(nameof(TimeSpanTestData))]
    public void GetClrValue_HandlesTimeSpanCorrectly(
        string dataTypeName,
        Type fieldType,
        object dbValue)
    {
        // Arrange
        var mockReader = new Mock<IDataReader>();
        mockReader.Setup(r => r.GetOrdinal("Col")).Returns(0);
        mockReader.Setup(r => r.GetValue(0)).Returns(dbValue);
        mockReader.Setup(r => r.GetDataTypeName(0)).Returns(dataTypeName);
        mockReader.Setup(r => r.GetFieldType(0)).Returns(fieldType);

        // Act
        object? value = mockReader.Object.GetClrValue("Col");

        // Assert
        Assert.IsType<TimeSpan>(value);
        Assert.Equal(dbValue, value);
    }

    #endregion

    #region Null and DBNull Tests

    [Fact]
    public void GetClrValue_WithDBNull_ReturnsNull()
    {
        // Arrange
        var mockReader = new Mock<IDataReader>();
        mockReader.Setup(r => r.GetOrdinal("Col")).Returns(0);
        mockReader.Setup(r => r.GetValue(0)).Returns(DBNull.Value);
        mockReader.Setup(r => r.GetDataTypeName(0)).Returns("varchar");
        mockReader.Setup(r => r.GetFieldType(0)).Returns(typeof(string));

        // Act
        object? value = mockReader.Object.GetClrValue("Col");

        // Assert
        Assert.Null(value);
    }

    [Fact]
    public void GetClrValue_WithDBNullAndValueType_ReturnsDefault()
    {
        // Arrange
        var mockReader = new Mock<IDataReader>();
        mockReader.Setup(r => r.GetOrdinal("Col")).Returns(0);
        mockReader.Setup(r => r.GetValue(0)).Returns(DBNull.Value);
        mockReader.Setup(r => r.GetDataTypeName(0)).Returns("int");
        mockReader.Setup(r => r.GetFieldType(0)).Returns(typeof(int));

        // Act
        object? value = mockReader.Object.GetClrValue("Col", typeof(int));

        // Assert
        Assert.NotNull(value);
        Assert.IsType<int>(value);
        Assert.Equal(0, value);
    }

    [Fact]
    public void GetClrValue_WithDBNullAndNullableType_ReturnsNull()
    {
        // Arrange
        var mockReader = new Mock<IDataReader>();
        mockReader.Setup(r => r.GetOrdinal("Col")).Returns(0);
        mockReader.Setup(r => r.GetValue(0)).Returns(DBNull.Value);
        mockReader.Setup(r => r.GetDataTypeName(0)).Returns("int");
        mockReader.Setup(r => r.GetFieldType(0)).Returns(typeof(int));

        // Act
        object? value = mockReader.Object.GetClrValue("Col", typeof(int?));

        // Assert
        Assert.Null(value);
    }

    #endregion

    #region Target Type Conversion Tests

    [Theory]
    [InlineData("42", typeof(int), 42)]
    [InlineData("3.14", typeof(double), 3.14)]
    [InlineData("123.45", typeof(decimal), 123.45)]
    [InlineData("true", typeof(bool), true)]
    [InlineData("2023-08-16", typeof(DateTime), "2023-08-16")]
    public void GetClrValue_WithTargetType_ConvertsCorrectly(
        object dbValue,
        Type targetType,
        object expectedValue)
    {
        // Arrange
        var mockReader = new Mock<IDataReader>();
        mockReader.Setup(r => r.GetOrdinal("Col")).Returns(0);
        mockReader.Setup(r => r.GetValue(0)).Returns(dbValue);
        mockReader.Setup(r => r.GetDataTypeName(0)).Returns("varchar");
        mockReader.Setup(r => r.GetFieldType(0)).Returns(typeof(string));

        // Act
        object? value = mockReader.Object.GetClrValue("Col", targetType);

        // Assert
        Assert.NotNull(value);
        Assert.IsType(targetType, value);

        if (targetType == typeof(DateTime) && dbValue is string dateTimeString)
        {
            Assert.Equal(DateTime.Parse(dateTimeString, CultureInfo.InvariantCulture), value);
        }
        else if (targetType == typeof(decimal))
        {
            Assert.Equal(Convert.ToDecimal(expectedValue, CultureInfo.InvariantCulture), Convert.ToDecimal(value, CultureInfo.InvariantCulture), 6);
        }
        else
        {
            Assert.Equal(expectedValue, value);
        }
    }

    [Fact]
    public void GetClrValue_WithNullableTargetType_HandlesCorrectly()
    {
        // Arrange
        var mockReader = new Mock<IDataReader>();
        mockReader.Setup(r => r.GetOrdinal("Col")).Returns(0);
        mockReader.Setup(r => r.GetValue(0)).Returns(42);
        mockReader.Setup(r => r.GetDataTypeName(0)).Returns("int");
        mockReader.Setup(r => r.GetFieldType(0)).Returns(typeof(int));

        // Act
        object? value = mockReader.Object.GetClrValue("Col", typeof(int?));

        // Assert
        Assert.NotNull(value);
        Assert.IsType<int>(value);
        Assert.Equal(42, value);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public void GetClrValue_WithInvalidColumnName_ThrowsException()
    {
        // Arrange
        var mockReader = new Mock<IDataReader>();
        mockReader.Setup(r => r.GetOrdinal("NonExistentColumn")).Throws(new IndexOutOfRangeException("Column not found"));

        // Act & Assert
        IndexOutOfRangeException exception = Assert.Throws<IndexOutOfRangeException>(() =>
            mockReader.Object.GetClrValue("NonExistentColumn"));
        Assert.Equal("Column not found", exception.Message);
    }

    [Fact]
    public void GetClrValue_WithConversionFailure_ThrowsInvalidCastException()
    {
        // Arrange
        var mockReader = new Mock<IDataReader>();
        mockReader.Setup(r => r.GetOrdinal("Col")).Returns(0);
        mockReader.Setup(r => r.GetValue(0)).Returns("invalid_number");
        mockReader.Setup(r => r.GetDataTypeName(0)).Returns("varchar");
        mockReader.Setup(r => r.GetFieldType(0)).Returns(typeof(string));

        // Act & Assert
        InvalidCastException exception = Assert.Throws<InvalidCastException>(() =>
            mockReader.Object.GetClrValue("Col", typeof(int)));

        Assert.Contains("Failed to convert database value", exception.Message);
        Assert.Contains("System.String", exception.Message);
        Assert.Contains("System.Int32", exception.Message);
    }

    [Fact]
    public void GetClrValue_WithUnsupportedConversion_ThrowsInvalidCastException()
    {
        // Arrange
        var mockReader = new Mock<IDataReader>();
        mockReader.Setup(r => r.GetOrdinal("Col")).Returns(0);
        mockReader.Setup(r => r.GetValue(0)).Returns(new object()); // Unsupported type
        mockReader.Setup(r => r.GetDataTypeName(0)).Returns("unknown");
        mockReader.Setup(r => r.GetFieldType(0)).Returns(typeof(object));

        // Act & Assert
        InvalidCastException exception = Assert.Throws<InvalidCastException>(() =>
            mockReader.Object.GetClrValue("Col", typeof(int)));

        Assert.Contains("Failed to convert database value", exception.Message);
    }

    #endregion

    #region Helper Methods

    private static void AssertValueEquality(Type expectedType, object expected, object? actual)
    {
        Assert.NotNull(actual);

        if (expectedType == typeof(byte[]))
        {
            Assert.Equal((byte[])expected, (byte[])actual);
        }
        else if (expectedType == typeof(float))
        {
            Assert.Equal((float)expected, (float)actual, 5); // Higher precision for float
        }
        else if (expectedType == typeof(double))
        {
            Assert.Equal((double)expected, (double)actual, 10); // Higher precision for double
        }
        else if (expectedType == typeof(decimal))
        {
            Assert.Equal((decimal)expected, Convert.ToDecimal(actual, CultureInfo.InvariantCulture), 6); // Higher precision for decimal
        }
        else if (expectedType == typeof(DateTime) && actual is DateTime actualDateTime)
        {
            // For DateTime comparisons, we need to handle potential precision differences
            var expectedDateTime = (DateTime)expected;
            Assert.Equal(expectedDateTime.Year, actualDateTime.Year);
            Assert.Equal(expectedDateTime.Month, actualDateTime.Month);
            Assert.Equal(expectedDateTime.Day, actualDateTime.Day);
            Assert.Equal(expectedDateTime.Hour, actualDateTime.Hour);
            Assert.Equal(expectedDateTime.Minute, actualDateTime.Minute);
            Assert.Equal(expectedDateTime.Second, actualDateTime.Second);
        }
        else
        {
            Assert.Equal(expected, actual);
        }
    }

    #endregion
}

