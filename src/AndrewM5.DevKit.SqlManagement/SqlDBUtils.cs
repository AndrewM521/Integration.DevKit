using System.Data;

namespace AndrewM5.DevKit.SqlManagement
{
    public static class SqlDBUtils
    {
        public static object GetDBNullIfNull(object? value)
        {
            if (value == null)
            {
                return DBNull.Value;
            }

            return value;
        }

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
}
