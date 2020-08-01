using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;

namespace SqlPlace.Factories
{
    public class SqlCommandFactory : ICommandFactory
    {

        public DbCommand CreateCommand()
        {
            return new SqlCommand();
        }

        public DbParameter CreateParameter(string name, object value, SqlDbType? sqlDbType, int? size, ParameterDirection? direction)
        {
            var param = new SqlParameter(name, value);
            if (sqlDbType.HasValue) param.SqlDbType = sqlDbType.Value;
            if (size.HasValue) param.Size = size.Value;
            if (direction.HasValue) param.Direction = direction.Value;
            return param;
        }

        public DbDataAdapter CreateDataAdapter()
        {
            return new SqlDataAdapter();
        }

        public string GetParameterName(int paramIndex)
        {
            return $"@p{paramIndex}";
        }

        public string GetParameterName(string paramName)
        {
            return $"@{paramName}";
        }
    }
}
