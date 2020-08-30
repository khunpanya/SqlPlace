using System;
using System.Data.SqlClient;

namespace SqlPlace.Factories
{
    public class SqlCommandFactory : GenericCommandFactory
    {
        public SqlCommandFactory(): base(SqlClientFactory.Instance)
        {

        }

        public override Type DbConnectionType => typeof(SqlConnection);

        public override string SpecificDbTypePropertyName => "SqlDbType";

        public override bool IsSupportNamedParameter => true;

        public override string FactoryDialectName => nameof(Dialects.MSSQL);

    }
}
