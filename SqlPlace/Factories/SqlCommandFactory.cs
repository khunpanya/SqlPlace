using System;
using System.Data.Common;
using System.Data.SqlClient;

namespace SqlPlace.Factories
{
    public class SqlCommandFactory : GenericCommandFactory
    {
        private static readonly bool reg = Register<SqlConnection, SqlCommandFactory>();

        private static bool regDialect = false;

        public SqlCommandFactory(): base(SqlClientFactory.Instance)
        {
            if(!regDialect)
            {
                SqlDialect.RegisterSyntax(FactoryDialectName, nameof(SqlDialect.CurrentDate), arguments =>
                {
                    return new SqlStatement("CONVERT(DATE, GETDATE())");
                });
                regDialect = true;
            }
        }

        public override string SpecificDbTypePropertyName => "SqlDbType";

        public override bool IsSupportNamedParameter => true;

        public override string FactoryDialectName => "MSSQL";

    }
}
