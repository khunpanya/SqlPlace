using System.Data.Common;
using System.Data.SqlClient;

namespace SqlPlace.Factories
{
    public class SqlCommandFactory : GenericCommandFactory<SqlClientFactory>
    {
        public override void SetSpecificDbType(DbParameter parameter, int specificDbType)
        {
            (parameter as SqlParameter).SqlDbType = (System.Data.SqlDbType)specificDbType;
        }
    }
}
