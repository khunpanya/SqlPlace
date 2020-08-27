using System;
using System.Data.Common;
using System.Data.SqlClient;

namespace SqlPlace.Factories
{
    public class SqlCommandFactory : GenericCommandFactory
    {
        static int _ = Register<SqlConnection, SqlCommandFactory>();

        public SqlCommandFactory(): base(SqlClientFactory.Instance)
        {

        }

        public override string SpecificDbTypePropertyName()
        {
            return "SqlDbType";
        }

        public override bool IsSupportNamedParameter()
        {
            return true;
        }
    }
}
