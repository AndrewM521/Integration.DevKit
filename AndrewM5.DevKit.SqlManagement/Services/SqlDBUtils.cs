using System.Data;

namespace AndrewM5.DevKit.SqlManagement.Services
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
    }
}
