/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using System.Data;

namespace AndrewM5.DevKit.SQLManagement;

/// <summary>
/// Utility methods to simplify data extraction from database records, handling null conversions 
/// and transforming data sets into standard collection types.
/// </summary>
public static class SQLUtils
{
    /// <summary>
    /// Converts a null object reference to <see cref="DBNull.Value"/> for database insertion.
    /// </summary>
    /// <param name="value">The value to check for <see langword="null"/>.</param>
    /// <returns>
    /// The original <paramref name="value"/> if not null; otherwise, returns <see cref="DBNull.Value"/>.
    /// </returns>
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
    /// <typeparam name="T">The desired return type for the value.</typeparam>
    /// <param name="reader">The <see cref="IDataRecord"/> containing the data.</param>
    /// <param name="columnName">The name of the column to retrieve.</param>
    /// <param name="defaultValue">The value to return if the database value is <see cref="DBNull"/> or retrieval fails.</param>
    /// <returns>
    /// The value cast or converted to <typeparamref name="T"/>, or <paramref name="defaultValue"/> if the operation fails.
    /// </returns>
    /// <remarks>
    /// This method performs an ordinal lookup and handles type conversion using <see cref="Convert.ChangeType(object, Type)"/> 
    /// if a direct cast is not possible.
    /// </remarks>
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
    /// <param name="record">The record (typically the current state of an <see cref="IDataReader"/>) to convert.</param>
    /// <returns>
    /// A <see cref="Dictionary{TKey, TValue}"/> containing column names as keys and their corresponding cell values.
    /// </returns>
    /// <remarks>
    /// The resulting dictionary uses <see cref="StringComparer.OrdinalIgnoreCase"/> for keys to ensure 
    /// case-insensitive column lookups.
    /// </remarks>
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
    /// <param name="reader">The active <see cref="IDataReader"/> instance.</param>
    /// <returns>
    /// A <see cref="List{T}"/> of dictionaries, where each dictionary represents a single row from the result set.
    /// </returns>
    /// <remarks>
    /// This method consumes the remaining results in the reader by calling <see cref="IDataReader.Read()"/> 
    /// until no more rows are available.
    /// </remarks>
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
