using System.Data.Common;
using System.Data.Odbc;

namespace SqlPlace.Factories
{
    public class OdbcCommandFactory : GenericCommandFactory
    {
        private readonly static bool reg = Register<OdbcConnection, OdbcCommandFactory>();

        public OdbcCommandFactory() : base(OdbcFactory.Instance)
        {

        }

        public override string SpecificDbTypePropertyName => "OdbcType";

        public override bool IsSupportNamedParameter => false;

    }
}
