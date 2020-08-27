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

        public override string SpecificDbTypePropertyName()
        {
            return "OdbcType";
        }

        public override bool IsSupportNamedParameter()
        {
            return false;
        }
    }
}
