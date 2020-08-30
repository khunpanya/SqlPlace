using System;
using System.Data.Odbc;

namespace SqlPlace.Factories
{
    public class OdbcCommandFactory : GenericCommandFactory
    {
        public OdbcCommandFactory() : base(OdbcFactory.Instance)
        {

        }

        public override Type DbConnectionType => typeof(OdbcConnection);

        public override string SpecificDbTypePropertyName => "OdbcType";

        public override bool IsSupportNamedParameter => false;

    }
}
