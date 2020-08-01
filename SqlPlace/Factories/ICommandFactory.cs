using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace SqlPlace.Factories
{
    public interface ICommandFactory
    {
        DbCommand CreateCommand();

        DbParameter CreateParameter(string name, object value, SqlDbType? sqlDbType, int? size, ParameterDirection? direction);

        DbDataAdapter CreateDataAdapter();

        string GetParameterName(int paramIndex);

        string GetParameterName(string paramName);

    }
}
