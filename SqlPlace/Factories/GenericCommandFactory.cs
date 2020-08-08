using System.Data;
using System.Data.Common;

namespace SqlPlace.Factories
{
    public class GenericCommandFactory<TCommand, TParameter, TDataAdapter> : ICommandFactory 
        where TCommand: DbCommand, new() 
        where TParameter: DbParameter, new()
        where TDataAdapter : DbDataAdapter, new()
    {

        public DbCommand CreateCommand()
        {
            return new TCommand();
        }

        public DbParameter CreateParameter(string name, object value, DbType? dbType, int? size, ParameterDirection? direction)
        {
            var param = new TParameter();
            param.ParameterName = name;
            param.Value = value;
            if (dbType.HasValue) param.DbType = dbType.Value;
            if (size.HasValue) param.Size = size.Value;
            if (direction.HasValue) param.Direction = direction.Value;
            return param;
        }

        public DbDataAdapter CreateDataAdapter()
        {
            return new TDataAdapter();
        }

        public string ParameterSymbol { get; set; } = "@";

        public string GetParameterName(int paramIndex)
        {
            return $"{ParameterSymbol}p{paramIndex}";
        }

        public string GetParameterName(string paramName)
        {
            return $"{ParameterSymbol}{paramName}";
        }
    }
}
