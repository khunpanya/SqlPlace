using System.Data.Common;
using System.Data.Odbc;

namespace SqlPlace.Factories
{
    public class OdbcCommandFactory : GenericCommandFactory
    {
        static int _ = Register<OdbcConnection, OdbcCommandFactory>();

        public OdbcCommandFactory() : base(OdbcFactory.Instance)
        {

        }
        
        public override void SetSpecificDbType(DbParameter parameter, int specificDbType)
        {
            (parameter as OdbcParameter).OdbcType = (OdbcType)specificDbType;
        }

        public override bool IsSupportNamedParameter()
        {
            return false;
        }
    }
}
