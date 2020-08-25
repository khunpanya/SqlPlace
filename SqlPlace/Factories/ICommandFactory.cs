using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace SqlPlace.Factories
{
    public interface ICommandFactory
    {
        DbCommand CreateCommand();

        DbParameter CreateParameter();

        DbDataAdapter CreateDataAdapter();

        string GetParameterName(int paramIndex);

        string GetParameterName(string paramName);

        string GetParameterPlaceholder(int paramIndex);

        string SpecificDbTypePropertyName();

        bool IsSupportNamedParameter();

    }
}
