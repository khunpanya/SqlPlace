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

        public override void SetSpecificDbType(DbParameter parameter, int specificDbType)
        {
            (parameter as SqlParameter).SqlDbType = (System.Data.SqlDbType)specificDbType;
        }

        public override bool IsSupportNamedParameter()
        {
            return true;
        }
    }
}
