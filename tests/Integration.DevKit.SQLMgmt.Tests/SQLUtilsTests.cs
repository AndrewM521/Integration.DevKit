using System.Data;
using System.Data.Common;

namespace Integration.DevKit.SQLMgmt.Tests;

public class SQLUtilsTests
{
    private static DataTable BuildTable()
    {
        var table = new DataTable();
        table.Columns.Add("Id", typeof(int));
        table.Columns.Add("Name", typeof(string));
        table.Columns.Add("Notes", typeof(string));

        table.Rows.Add(1, "Widget", DBNull.Value);
        table.Rows.Add(2, "Gadget", "has notes");

        return table;
    }

    private static DbDataReader Reader() => BuildTable().CreateDataReader();

    [Fact]
    public void GetDBNullIfNull_NullValue_ReturnsDBNull()
    {
        Assert.Equal(DBNull.Value, SQLUtils.GetDBNullIfNull(null));
    }

    [Fact]
    public void GetDBNullIfNull_NonNullValue_ReturnsValue()
    {
        Assert.Equal("hello", SQLUtils.GetDBNullIfNull("hello"));
    }

    [Fact]
    public void GetDBNullIfNull_MatchesNullEquivalent_ReturnsDBNull()
    {
        Assert.Equal(DBNull.Value, SQLUtils.GetDBNullIfNull(-1, -1));
    }

    [Fact]
    public void GetValueOrDefault_NonNullColumn_ReturnsTypedValue()
    {
        using var reader = Reader();
        reader.Read();

        var name = reader.GetValueOrDefault("Name", "fallback");

        Assert.Equal("Widget", name);
    }

    [Fact]
    public void GetValueOrDefault_DBNullColumn_ReturnsDefault()
    {
        using var reader = Reader();
        reader.Read();

        var notes = reader.GetValueOrDefault("Notes", "no-notes");

        Assert.Equal("no-notes", notes);
    }

    [Fact]
    public void GetValueOrDefault_UnknownColumn_ReturnsDefault()
    {
        using var reader = Reader();
        reader.Read();

        var value = reader.GetValueOrDefault("DoesNotExist", 42);

        Assert.Equal(42, value);
    }

    [Fact]
    public void GetValueOrDefault_ConvertibleType_ConvertsValue()
    {
        using var reader = Reader();
        reader.Read();

        var idAsLong = reader.GetValueOrDefault<long>("Id", -1);

        Assert.Equal(1L, idAsLong);
    }

    [Fact]
    public void RowToDictionary_ReturnsAllColumnsCaseInsensitive()
    {
        using var reader = Reader();
        reader.Read();

        var row = reader.RowToDictionary();

        Assert.Equal(1, row["id"]);
        Assert.Equal("Widget", row["NAME"]);
        Assert.Null(row["Notes"]);
    }

    [Fact]
    public async Task RowToDictionaryAsync_ReturnsAllColumns()
    {
        using var reader = Reader();
        await reader.ReadAsync();

        var row = await reader.RowToDictionaryAsync();

        Assert.Equal(1, row["Id"]);
        Assert.Equal("Widget", row["Name"]);
    }

    [Fact]
    public void ToListDictionary_ConsumesAllRemainingRows()
    {
        using var reader = Reader();

        var rows = reader.ToListDictionary();

        Assert.Equal(2, rows.Count);
        Assert.Equal("Widget", rows[0]["Name"]);
        Assert.Equal("Gadget", rows[1]["Name"]);
    }

    [Fact]
    public async Task ToListDictionaryAsync_ConsumesAllRemainingRows()
    {
        using var reader = Reader();

        var rows = await reader.ToListDictionaryAsync();

        Assert.Equal(2, rows.Count);
    }

    [Fact]
    public void ToCsvContent_ProducesHeaderAndDataRows()
    {
        using var reader = Reader();

        var csv = reader.ToCsvContent();

        Assert.Equal(3, csv.Count); // header + 2 rows
        Assert.Contains("Id", csv[0]);
        Assert.Contains("Name", csv[0]);
        Assert.Equal("1", csv[1][Array.IndexOf(csv[0], "Id")]);
        Assert.Equal("", csv[1][Array.IndexOf(csv[0], "Notes")]);
    }

    [Fact]
    public void ToCsvContent_WithColumnFilter_OnlyIncludesRequestedColumns()
    {
        using var reader = Reader();

        var csv = reader.ToCsvContent(new List<string> { "Name" });

        Assert.Equal(new[] { "Name" }, csv[0]);
        Assert.Equal("Widget", csv[1][0]);
    }

    [Fact]
    public void ToCsvContent_EscapesCommasAndQuotes()
    {
        var table = new DataTable();
        table.Columns.Add("Value", typeof(string));
        table.Rows.Add("has, a comma and \"quotes\"");
        using var reader = table.CreateDataReader();

        var csv = reader.ToCsvContent();

        Assert.DoesNotContain(",", csv[1][0]);
        Assert.DoesNotContain("\"", csv[1][0]);
    }

    [Fact]
    public void ToCsvContent_NoRows_ReturnsEmptyList()
    {
        var table = new DataTable();
        table.Columns.Add("Id", typeof(int));
        using var reader = table.CreateDataReader();

        var csv = reader.ToCsvContent();

        Assert.Empty(csv);
    }

    [Fact]
    public async Task ToCsvContentAsync_ProducesHeaderAndDataRows()
    {
        using var reader = Reader();

        var csv = await reader.ToCsvContentAsync();

        Assert.Equal(3, csv.Count);
    }
}
