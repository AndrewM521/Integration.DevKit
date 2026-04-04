using System.Data;

namespace AndrewM5.DevKit.SqlManagement;

/// <summary>
/// Provides a set of static helper methods and extension methods for common SQL and DataReader operations.
/// </summary>
public static class SqlDBUtils
{
    /// <summary>
    /// Converts a null object reference to <see cref="DBNull.Value"/> for database insertion.
    /// </summary>
    /// <param name="value">The value to check for null.</param>
    /// <returns>The original value if not null; otherwise, <see cref="DBNull.Value"/>.</returns>
    public static object GetDBNullIfNull(object? value)
    {
        if (value == null)
        {
            return DBNull.Value;
        }

        return value;
    }

    /// <summary>
    /// Attempts to retrieve a value from a data record by column name, returning a default value if the column is null or an error occurs.
    /// </summary>
    /// <typeparam name="T">The desired return type.</typeparam>
    /// <param name="reader">The data record instance.</param>
    /// <param name="columnName">The name of the column to retrieve.</param>
    /// <param name="defaultValue">The value to return if the database value is null or retrieval fails.</param>
    /// <returns>The value cast or converted to <typeparamref name="T"/>, or <paramref name="defaultValue"/>.</returns>
    public static T GetValueOrDefaultFromReader<T>(this IDataRecord reader, string columnName, T defaultValue)
    {
        try
        {
            int ordinal = reader.GetOrdinal(columnName);

            if (reader.IsDBNull(ordinal))
            {
                return defaultValue;
            }

            object val = reader.GetValue(ordinal);

            if (val is T typedVal)
            {
                return typedVal;
            }

            return (T)Convert.ChangeType(val, typeof(T));
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// Converts the current row of an <see cref="IDataRecord"/> into a dictionary where keys are column names.
    /// </summary>
    /// <param name="record">The record (typically the current state of a reader) to convert.</param>
    /// <returns>A dictionary containing column names as keys and their corresponding values.</returns>
    /// <remarks>The resulting dictionary uses a case-insensitive ordinal comparer for keys.</remarks>
    public static Dictionary<string, object?> ReaderRowToDictionary(this IDataRecord record)
    {
        var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < record.FieldCount; i++)
        {
            var columnName = record.GetName(i);
            object? rowVal = null;

            if (!record.IsDBNull(i))
            {
                rowVal = record.GetValue(i);
            }

            row[columnName] = rowVal;
        }

        return row;
    }

    /// <summary>
    /// Iterates through an <see cref="IDataReader"/> and converts all remaining rows into a list of dictionaries.
    /// </summary>
    /// <param name="reader">The active data reader.</param>
    /// <returns>A list of dictionaries, where each dictionary represents a single row from the result set.</returns>
    public static List<Dictionary<string, object?>> ReaderResultToList(this IDataReader reader)
    {
        var list = new List<Dictionary<string, object?>>();

        while (reader.Read())
        {
            list.Add(reader.ReaderRowToDictionary());
        }

        return list;
    }
}
