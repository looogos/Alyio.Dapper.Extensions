// MIT License

using Alyio.Dapper.Extensions;
using Dapper;
using Microsoft.Data.Sqlite;

namespace Alyio.Dapper.Extension.Tests;

public sealed class DbConnectionExtensionsTests
{
    private sealed class TestEntity
    {
        public int Id { get; set; }
        public string? Name { get; set; }

        [ExtraData]
        public IDictionary<string, object?>? ExtraData { get; set; }
    }

    private sealed class NoExtraDataEntity
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    private sealed class MultipleExtraDataEntity
    {
        [ExtraData]
        public IDictionary<string, object?>? Extra1 { get; set; }

        [ExtraData]
        public IDictionary<string, object?>? Extra2 { get; set; }
    }

    private sealed class InvalidExtraDataTypeEntity
    {
        public int Id { get; set; }
        public string? Name { get; set; }

        [ExtraData]
        public List<string>? ExtraData { get; set; }
    }

    [Fact]
    public async Task QueryWithExtraAsync_MapsExtraColumnsToDictionary()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        await connection.ExecuteAsync(@"
            CREATE TABLE TestEntities (
                Id INTEGER PRIMARY KEY,
                Name TEXT,
                Extra1 TEXT,
                Extra2 INTEGER
            );
            INSERT INTO TestEntities (Id, Name, Extra1, Extra2) VALUES (1, 'Alice', 'foo', 42);
            INSERT INTO TestEntities (Id, Name, Extra1, Extra2) VALUES (2, 'Bob', 'bar', 99);
        ");

        IEnumerable<TestEntity> result = await connection.QueryWithExtraAsync<TestEntity>("SELECT * FROM TestEntities");
        var list = new List<TestEntity>(result);

        Assert.Equal(2, list.Count);
        Assert.Equal("foo", list[0].ExtraData!["Extra1"]);
        Assert.Equal(42L, list[0].ExtraData!["Extra2"]);
        Assert.Equal("bar", list[1].ExtraData!["Extra1"]);
        Assert.Equal(99L, list[1].ExtraData!["Extra2"]);
    }

    [Fact]
    public async Task QueryWithExtraAsync_NoExtraColumns_DictionaryIsEmpty()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        await connection.ExecuteAsync(@"
            CREATE TABLE TestEntities (
                Id INTEGER PRIMARY KEY,
                Name TEXT
            );
            INSERT INTO TestEntities (Id, Name) VALUES (1, 'Alice');
        ");

        IEnumerable<TestEntity> result = await connection.QueryWithExtraAsync<TestEntity>("SELECT * FROM TestEntities");
        TestEntity entity = Assert.Single(result);
        Assert.Null(entity.ExtraData);
    }

    [Fact]
    public async Task QueryWithExtraAsync_EntityWithoutExtraDataProperty_IgnoresExtraColumns()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        await connection.ExecuteAsync(@"
            CREATE TABLE NoExtraDataEntities (
                Id INTEGER PRIMARY KEY,
                Name TEXT,
                Extra1 TEXT
            );
            INSERT INTO NoExtraDataEntities (Id, Name, Extra1) VALUES (1, 'Alice', 'foo');
        ");

        IEnumerable<NoExtraDataEntity> result = await connection.QueryWithExtraAsync<NoExtraDataEntity>("SELECT * FROM NoExtraDataEntities");
        NoExtraDataEntity entity = Assert.Single(result);
        Assert.Equal(1, entity.Id);
        Assert.Equal("Alice", entity.Name);
    }

    [Fact]
    public async Task QueryWithExtraAsync_MultipleExtraDataProperties_ThrowsException()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        await connection.ExecuteAsync(@"
            CREATE TABLE MultipleExtraDataEntities (
                Id INTEGER PRIMARY KEY,
                Name TEXT,
                Extra1 TEXT
            );
            INSERT INTO MultipleExtraDataEntities (Id, Name, Extra1) VALUES (1, 'Alice', 'foo');
        ");

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await connection.QueryWithExtraAsync<MultipleExtraDataEntity>("SELECT * FROM MultipleExtraDataEntities");
        });
    }

    [Fact]
    public async Task QueryWithExtraAsync_InvalidExtraDataType_ThrowsException()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        await connection.ExecuteAsync(@"
            CREATE TABLE InvalidExtraDataTypeEntities (
                Id INTEGER PRIMARY KEY,
                Name TEXT,
                Extra1 TEXT
            );
            INSERT INTO InvalidExtraDataTypeEntities (Id, Name, Extra1) VALUES (1, 'Alice', 'foo');
        ");

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await connection.QueryWithExtraAsync<InvalidExtraDataTypeEntity>("SELECT * FROM InvalidExtraDataTypeEntities");
        });
    }

    [Fact]
    public async Task QueryWithExtraAsync_HandlesNullValuesInExtraColumns()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        await connection.ExecuteAsync(@"
            CREATE TABLE TestEntities (
                Id INTEGER PRIMARY KEY,
                Name TEXT,
                Extra1 TEXT,
                Extra2 INTEGER
            );
            INSERT INTO TestEntities (Id, Name, Extra1, Extra2) VALUES (1, 'Alice', NULL, NULL);
        ");

        IEnumerable<TestEntity> result = await connection.QueryWithExtraAsync<TestEntity>("SELECT * FROM TestEntities");
        TestEntity entity = Assert.Single(result);
        Assert.NotNull(entity.ExtraData);
        Assert.Null(entity.ExtraData!["Extra1"]);
        Assert.Null(entity.ExtraData!["Extra2"]);
    }

    [Fact]
    public async Task QueryWithExtraAsync_MapsVariousDataTypesToDictionary()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        await connection.ExecuteAsync(@"
            CREATE TABLE DataTypeEntities (
                Id INTEGER PRIMARY KEY,
                Name TEXT,
                ExtraDate DATETIME,
                ExtraBool BOOLEAN,
                ExtraReal REAL,
                ExtraNumeric NUMERIC,
                ExtraBlob BLOB
            );
            INSERT INTO DataTypeEntities (Id, Name, ExtraDate, ExtraBool, ExtraReal, ExtraNumeric, ExtraBlob)
            VALUES (
                1,
                'Test',
                '2023-08-16 12:34:56',
                1,
                3.14159,
                123.456,
                x'01020304'
            );
        ");

        IEnumerable<TestEntity> result = await connection.QueryWithExtraAsync<TestEntity>(@"
            SELECT Id, Name, ExtraDate, ExtraBool, ExtraReal, ExtraNumeric, ExtraBlob FROM DataTypeEntities
        ");
        TestEntity entity = Assert.Single(result);
        IDictionary<string, object?> extra = entity.ExtraData!;

        // DATETIME: Should be mapped to DateTime
        Assert.True(extra.ContainsKey("ExtraDate"));
        Assert.IsType<DateTime>(extra["ExtraDate"]);
        Assert.Equal(new DateTime(2023, 8, 16, 12, 34, 56), extra["ExtraDate"]);

        // BOOLEAN: Should be mapped to bool
        Assert.True(extra.ContainsKey("ExtraBool"));
        Assert.IsType<bool>(extra["ExtraBool"]);
        Assert.True((bool)extra["ExtraBool"]!);

        // REAL: Should be mapped to double
        Assert.True(extra.ContainsKey("ExtraReal"));
        Assert.IsType<double>(extra["ExtraReal"]);
        Assert.Equal(3.14159f, (double)extra["ExtraReal"]!, 5);

        // NUMERIC: Should be mapped to decimal
        Assert.True(extra.ContainsKey("ExtraNumeric"));
        Assert.IsType<decimal>(extra["ExtraNumeric"]);
        Assert.Equal(123.456m, (decimal)extra["ExtraNumeric"]!, 3);

        // BLOB: Should be mapped to byte[]
        Assert.True(extra.ContainsKey("ExtraBlob"));
        Assert.IsType<byte[]>(extra["ExtraBlob"]);
        Assert.Equal([1, 2, 3, 4], (byte[])extra["ExtraBlob"]!);
    }

    [Fact]
    public void QueryWithExtra_MapsExtraColumnsToDictionary()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        connection.Execute(@"
            CREATE TABLE TestEntities (
                Id INTEGER PRIMARY KEY,
                Name TEXT,
                Extra1 TEXT,
                Extra2 INTEGER
            );
            INSERT INTO TestEntities (Id, Name, Extra1, Extra2) VALUES (1, 'Alice', 'foo', 42);
            INSERT INTO TestEntities (Id, Name, Extra1, Extra2) VALUES (2, 'Bob', 'bar', 99);
        ");

        IEnumerable<TestEntity> result = connection.QueryWithExtra<TestEntity>("SELECT * FROM TestEntities");
        var list = new List<TestEntity>(result);

        Assert.Equal(2, list.Count);
        Assert.Equal("foo", list[0].ExtraData!["Extra1"]);
        Assert.Equal(42L, list[0].ExtraData!["Extra2"]);
        Assert.Equal("bar", list[1].ExtraData!["Extra1"]);
        Assert.Equal(99L, list[1].ExtraData!["Extra2"]);
    }

    [Fact]
    public void QueryWithExtra_NoExtraColumns_DictionaryIsEmpty()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        connection.Execute(@"
            CREATE TABLE TestEntities (
                Id INTEGER PRIMARY KEY,
                Name TEXT
            );
            INSERT INTO TestEntities (Id, Name) VALUES (1, 'Alice');
        ");

        IEnumerable<TestEntity> result = connection.QueryWithExtra<TestEntity>("SELECT * FROM TestEntities");
        TestEntity entity = Assert.Single(result);
        Assert.Null(entity.ExtraData);
    }

    [Fact]
    public void QueryWithExtra_EntityWithoutExtraDataProperty_IgnoresExtraColumns()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        connection.Execute(@"
            CREATE TABLE NoExtraDataEntities (
                Id INTEGER PRIMARY KEY,
                Name TEXT,
                Extra1 TEXT
            );
            INSERT INTO NoExtraDataEntities (Id, Name, Extra1) VALUES (1, 'Alice', 'foo');
        ");

        IEnumerable<NoExtraDataEntity> result = connection.QueryWithExtra<NoExtraDataEntity>("SELECT * FROM NoExtraDataEntities");
        NoExtraDataEntity entity = Assert.Single(result);
        Assert.Equal(1, entity.Id);
        Assert.Equal("Alice", entity.Name);
    }

    [Fact]
    public void QueryWithExtra_MultipleExtraDataProperties_ThrowsException()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        connection.Execute(@"
            CREATE TABLE MultipleExtraDataEntities (
                Id INTEGER PRIMARY KEY,
                Name TEXT,
                Extra1 TEXT
            );
            INSERT INTO MultipleExtraDataEntities (Id, Name, Extra1) VALUES (1, 'Alice', 'foo');
        ");

        Assert.Throws<InvalidOperationException>(() =>
        {
            connection.QueryWithExtra<MultipleExtraDataEntity>("SELECT * FROM MultipleExtraDataEntities");
        });
    }

    [Fact]
    public void QueryWithExtra_InvalidExtraDataType_ThrowsException()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        connection.Execute(@"
            CREATE TABLE InvalidExtraDataTypeEntities (
                Id INTEGER PRIMARY KEY,
                Name TEXT,
                Extra1 TEXT
            );
            INSERT INTO InvalidExtraDataTypeEntities (Id, Name, Extra1) VALUES (1, 'Alice', 'foo');
        ");

        Assert.Throws<InvalidOperationException>(() =>
        {
            connection.QueryWithExtra<InvalidExtraDataTypeEntity>("SELECT * FROM InvalidExtraDataTypeEntities");
        });
    }

    [Fact]
    public void QueryWithExtra_HandlesNullValuesInExtraColumns()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        connection.Execute(@"
            CREATE TABLE TestEntities (
                Id INTEGER PRIMARY KEY,
                Name TEXT,
                Extra1 TEXT,
                Extra2 INTEGER
            );
            INSERT INTO TestEntities (Id, Name, Extra1, Extra2) VALUES (1, 'Alice', NULL, NULL);
        ");

        IEnumerable<TestEntity> result = connection.QueryWithExtra<TestEntity>("SELECT * FROM TestEntities");
        TestEntity entity = Assert.Single(result);
        Assert.NotNull(entity.ExtraData);
        Assert.Null(entity.ExtraData!["Extra1"]);
        Assert.Null(entity.ExtraData!["Extra2"]);
    }

    [Fact]
    public void QueryWithExtra_MapsVariousDataTypesToDictionary()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        connection.Execute(@"
            CREATE TABLE DataTypeEntities (
                Id INTEGER PRIMARY KEY,
                Name TEXT,
                ExtraDate DATE,
                ExtraDateTime DATETIME,
                ExtraBool BOOLEAN,
                ExtraReal REAL,
                ExtraNumeric NUMERIC,
                ExtraBlob BLOB,
                ExtraGuid GUID,
                ExtraUniqueIdentifier UNIQUEIDENTIFIER
            );
            INSERT INTO DataTypeEntities (Id, Name, ExtraDate, ExtraDateTime, ExtraBool, ExtraReal, ExtraNumeric, ExtraBlob, ExtraGuid, ExtraUniqueIdentifier)
            VALUES (
                1,
                'Test',
                '2025-08-16',
                '2025-08-16 12:34:56',
                1,
                3.14159,
                123.456,
                x'01020304',
                '8ba97c9b-8844-4db5-8807-fa1d33900c55',
                '8ba97c9b-8844-4db5-8807-fa1d33900c55'
            );
        ");

        IEnumerable<TestEntity> result = connection.QueryWithExtra<TestEntity>(@"
            SELECT Id, Name, ExtraDate, ExtraDateTime, ExtraBool, ExtraReal, ExtraNumeric, ExtraBlob, ExtraGuid, ExtraUniqueIdentifier FROM DataTypeEntities
        ");
        TestEntity entity = Assert.Single(result);
        IDictionary<string, object?> extra = entity.ExtraData!;

        // DATE: Should be mapped to DateTime
        Assert.True(extra.ContainsKey("ExtraDate"));
        Assert.IsType<DateTime>(extra["ExtraDate"]);
        Assert.Equal(new DateTime(2025, 8, 16), extra["ExtraDate"]!);

        // DATETIME: Should be mapped to DateTime
        Assert.True(extra.ContainsKey("ExtraDateTime"));
        Assert.IsType<DateTime>(extra["ExtraDateTime"]);
        Assert.Equal(new DateTime(2025, 8, 16, 12, 34, 56), extra["ExtraDateTime"]!);

        // BOOLEAN: Should be mapped to bool
        Assert.True(extra.ContainsKey("ExtraBool"));
        Assert.IsType<bool>(extra["ExtraBool"]);
        Assert.True((bool)extra["ExtraBool"]!);

        // REAL: Should be mapped to double
        Assert.True(extra.ContainsKey("ExtraReal"));
        Assert.IsType<double>(extra["ExtraReal"]);
        Assert.Equal(3.14159f, (double)extra["ExtraReal"]!, 5);

        // NUMERIC: Should be mapped to decimal
        Assert.True(extra.ContainsKey("ExtraNumeric"));
        Assert.IsType<decimal>(extra["ExtraNumeric"]);
        Assert.Equal(123.456m, (decimal)extra["ExtraNumeric"]!, 3);

        // BLOB: Should be mapped to byte[]
        Assert.True(extra.ContainsKey("ExtraBlob"));
        Assert.IsType<byte[]>(extra["ExtraBlob"]);
        Assert.Equal([1, 2, 3, 4], (byte[])extra["ExtraBlob"]!);

        // GUID: Should be mapped to Guid
        Assert.True(extra.ContainsKey("ExtraGuid"));
        Assert.IsType<Guid>(extra["ExtraGuid"]);
        Assert.Equal(Guid.Parse("8ba97c9b-8844-4db5-8807-fa1d33900c55"), (Guid)extra["ExtraGuid"]!);

        // UNIQUEIDENTIFIER: Should be mapped to Guid
        Assert.True(extra.ContainsKey("ExtraUniqueIdentifier"));
        Assert.IsType<Guid>(extra["ExtraUniqueIdentifier"]);
        Assert.Equal(Guid.Parse("8ba97c9b-8844-4db5-8807-fa1d33900c55"), (Guid)extra["ExtraUniqueIdentifier"]!);
    }
}
