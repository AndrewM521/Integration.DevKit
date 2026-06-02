/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using Microsoft.Data.SqlClient;
using System.Data;
using System.Data.Common;
using System.Diagnostics;

namespace AndrewM5.DevKit.SQLMgmt;

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
    /// <param name="nullEquivalentValue">Optional value to say an object is null</param>
    /// <returns>
    /// The original <paramref name="value"/> if not null; otherwise, returns <see cref="DBNull.Value"/>.
    /// </returns>
    public static object GetDBNullIfNull(object? value, object? nullEquivalentValue = null)
    {
        // Check if the primary value is null
        if (value == null)
        {
            return DBNull.Value;
        }

        // Check if the value matches your custom null equivalent (e.g., -1, "N/A", or an empty string)
        if (nullEquivalentValue != null && object.Equals(value, nullEquivalentValue))
        {
            return DBNull.Value;
        }

        return value;
    }

    /// <summary>
    /// Attempts to retrieve a value from a data reader by column name, returning a default value if the column is null or an error occurs.
    /// </summary>
    /// <typeparam name="T">The desired return type for the value.</typeparam>
    /// <param name="reader">The active <see cref="DbDataReader"/> containing the data.</param>
    /// <param name="columnName">The name of the column to retrieve.</param>
    /// <param name="defaultValue">The value to return if the database value is <see cref="DBNull"/> or retrieval fails.</param>
    /// <returns>
    /// The value cast or converted to <typeparamref name="T"/>, or <paramref name="defaultValue"/> if the operation fails.
    /// </returns>
    /// <remarks>
    /// This method performs an ordinal lookup and handles type conversion using <see cref="Convert.ChangeType(object, Type)"/> 
    /// if a direct cast is not possible.
    /// </remarks>
    public static T GetValueOrDefault<T>(this DbDataReader reader, string columnName, T defaultValue)
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
    /// Converts the current row of a <see cref="DbDataReader"/> into a dictionary where keys are column names.
    /// </summary>
    /// <param name="reader">The reader at its current row state to convert.</param>
    /// <returns>
    /// A <see cref="Dictionary{TKey, TValue}"/> containing column names as keys and their corresponding cell values.
    /// </returns>
    /// <remarks>
    /// The resulting dictionary uses <see cref="StringComparer.OrdinalIgnoreCase"/> for keys to ensure 
    /// case-insensitive column lookups.
    /// </remarks>
    public static Dictionary<string, object?> RowToDictionary(this DbDataReader reader)
    {
        var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < reader.FieldCount; i++)
        {
            var columnName = reader.GetName(i);
            object? rowVal = null;

            if (!reader.IsDBNull(i))
            {
                rowVal = reader.GetValue(i);
            }

            row[columnName] = rowVal;
        }

        return row;
    }

    /// <summary>
    /// Asynchronously converts the current row of a <see cref="DbDataReader"/> into a dictionary where keys are column names.
    /// </summary>
    /// <param name="reader">The reader at its current row state to convert.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A <see cref="Dictionary{TKey, TValue}"/> containing column names as keys and their corresponding cell values.
    /// </returns>
    /// <remarks>
    /// The resulting dictionary uses <see cref="StringComparer.OrdinalIgnoreCase"/> for keys to ensure 
    /// case-insensitive column lookups.
    /// </remarks>
    public static async Task<Dictionary<string, object?>> RowToDictionaryAsync(this DbDataReader reader, CancellationToken cancellationToken = default)
    {
        var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < reader.FieldCount; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var columnName = reader.GetName(i);
            object? rowVal = null;

            if (!await reader.IsDBNullAsync(i, cancellationToken))
            {
                rowVal = reader.GetValue(i);
            }

            row[columnName] = rowVal;
        }

        return row;
    }

    /// <summary>
    /// Iterates through an <see cref="DbDataReader"/> and converts all remaining rows into a list of dictionaries.
    /// </summary>
    /// <param name="reader">The active <see cref="IDataReader"/> instance.</param>
    /// <returns>
    /// A <see cref="List{T}"/> of dictionaries, where each dictionary represents a single row from the result set.
    /// </returns>
    /// <remarks>
    /// This method consumes the remaining results in the reader by calling <see cref="IDataReader.Read()"/> 
    /// until no more rows are available.
    /// </remarks>
    public static List<Dictionary<string, object?>> ToListDictionary(this DbDataReader reader)
    {
        var list = new List<Dictionary<string, object?>>();

        while (reader.Read())
        {
            list.Add(reader.RowToDictionary());
        }

        return list;
    }

    /// <summary>
    /// Asynchronously iterates through a <see cref="DbDataReader"/> and converts all remaining rows into a list of dictionaries.
    /// </summary>
    /// <param name="reader">The active <see cref="DbDataReader"/> instance.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task representing the async operation, containing a list of dictionaries representing the row values.
    /// </returns>
    public static async Task<List<Dictionary<string, object?>>> ToListDictionaryAsync(this DbDataReader reader, CancellationToken cancellationToken = default)
    {
        var list = new List<Dictionary<string, object?>>();

        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(await reader.RowToDictionaryAsync(cancellationToken));
        }

        return list;
    }

    /// <summary>
    /// Converts the data from a <see cref="DbDataReader"/> into a list of string arrays, 
    /// formatted and sanitized for CSV output.
    /// </summary>
    /// <param name="reader">The active database data reader to extract data from.</param>
    /// <param name="columnsToInclude">
    /// Optional. A list of specific column names to include in the output. 
    /// If null or empty, all columns from the data reader are exported in their default database order.
    /// </param>
    /// <returns>
    /// A result containing a list of string arrays, where the first element is the header row, followed by the data rows.
    /// </returns>
    public static List<string[]> ToCsvContent(this DbDataReader reader, List<string>? columnsToInclude = null)
    {
        var csvContent = new List<string[]>();

        if (reader == null || !reader.HasRows)
        {
            return csvContent;
        }

        // Map out the column names and their database indices
        var schemaMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < reader.FieldCount; i++)
        {
            schemaMap[reader.GetName(i)] = i;
        }

        // Determine target column indices based on user selection or full schema
        List<int> targetIndices = new List<int>();
        string[] headers;

        if (columnsToInclude != null && columnsToInclude.Count > 0)
        {
            var headersList = new List<string>();
            // Filter and order based on the user's provided list
            foreach (var colName in columnsToInclude)
            {
                if (schemaMap.TryGetValue(colName, out int index))
                {
                    targetIndices.Add(index);
                    headersList.Add(colName);
                }
                else
                {
                    Debug.WriteLine($"Column '{colName}' requested for CSV export was not found in the data reader source.");
                }
            }
            headers = headersList.ToArray();
        }
        else
        {
            // Default behavior: Include all columns in their natural DB order
            for (int i = 0; i < reader.FieldCount; i++)
            {
                targetIndices.Add(i);
            }
            headers = schemaMap.Keys.ToArray();
        }

        // Add the headers to the CSV output
        csvContent.Add(headers);
        int targetFieldCount = targetIndices.Count;

        // Extract Rows dynamically
        while (reader.Read())
        {
            string[] row = new string[targetFieldCount];

            for (int i = 0; i < targetFieldCount; i++)
            {
                int dbIndex = targetIndices[i];

                if (reader.IsDBNull(dbIndex))
                {
                    row[i] = "";
                }
                else
                {
                    string val = reader.GetValue(dbIndex)?.ToString() ?? "";

                    // Standard CSV escaping
                    if (val.Contains(",") || val.Contains("\r") || val.Contains("\n") || val.Contains("\""))
                    {
                        val = val.Replace("\"", "")
                                 .Replace(",", " ")
                                 .Replace("\r", "")
                                 .Replace("\n", "");
                    }

                    row[i] = val;
                }
            }

            csvContent.Add(row);
        }

        return csvContent;
    }

    /// <summary>
    /// Asynchronously converts the data from a <see cref="DbDataReader"/> into a list of string arrays, 
    /// formatted and sanitized for CSV output.
    /// </summary>
    /// <param name="reader">The active database data reader to extract data from.</param>
    /// <param name="columnsToInclude">
    /// Optional. A list of specific column names to include in the output. 
    /// If null or empty, all columns from the data reader are exported in their default database order.
    /// </param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a list of string arrays, 
    /// where the first element is the header row, followed by the data rows.
    /// </returns>
    /// <exception cref="OperationCanceledException">Thrown when the <paramref name="cancellationToken"/> is canceled.</exception>
    public static async Task<List<string[]>> ToCsvContentAsync(this DbDataReader reader, List<string>? columnsToInclude = null, CancellationToken cancellationToken = default)
    {
        var csvContent = new List<string[]>();

        if (reader == null || !reader.HasRows)
        {
            return csvContent;
        }

        // Map out the column names and their database indices
        var schemaMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < reader.FieldCount; i++)
        {
            schemaMap[reader.GetName(i)] = i;
        }

        // Determine target column indices based on user selection or full schema
        List<int> targetIndices = new List<int>();
        string[] headers;

        if (columnsToInclude != null && columnsToInclude.Count > 0)
        {
            var headersList = new List<string>();
            // Filter and order based on the user's provided list
            foreach (var colName in columnsToInclude)
            {
                if (schemaMap.TryGetValue(colName, out int index))
                {
                    targetIndices.Add(index);
                    headersList.Add(colName);
                }
                else
                {
                    Debug.WriteLine($"Column '{colName}' requested for CSV export was not found in the data reader source.");
                }
            }
            headers = headersList.ToArray();
        }
        else
        {
            // Default behavior: Include all columns in their natural DB order
            for (int i = 0; i < reader.FieldCount; i++)
            {
                targetIndices.Add(i);
            }
            headers = schemaMap.Keys.ToArray();
        }

        // Add the headers to the CSV output
        csvContent.Add(headers);
        int targetFieldCount = targetIndices.Count;

        // Extract Rows dynamically
        while (await reader.ReadAsync(cancellationToken))
        {
            if (cancellationToken.IsCancellationRequested) break;

            string[] row = new string[targetFieldCount];

            for (int i = 0; i < targetFieldCount; i++)
            {
                int dbIndex = targetIndices[i];

                if (await reader.IsDBNullAsync(dbIndex, cancellationToken))
                {
                    row[i] = "";
                }
                else
                {
                    string val = reader.GetValue(dbIndex)?.ToString() ?? "";

                    // Standard CSV escaping
                    if (val.Contains(",") || val.Contains("\r") || val.Contains("\n") || val.Contains("\""))
                    {
                        val = val.Replace("\"", "")
                                 .Replace(",", " ")
                                 .Replace("\r", "")
                                 .Replace("\n", "");
                    }

                    row[i] = val;
                }
            }

            csvContent.Add(row);
        }

        return csvContent;
    }
}
