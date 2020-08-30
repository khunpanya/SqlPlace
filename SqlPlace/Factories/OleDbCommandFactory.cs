using System;
using System.Data.OleDb;

namespace SqlPlace.Factories
{
    public class OleDbCommandFactory : GenericCommandFactory
    {
        public OleDbCommandFactory() : base(OleDbFactory.Instance)
        {

        }

        public override Type DbConnectionType => typeof(OleDbConnection);

        public override string SpecificDbTypePropertyName => "OleDbType";

        public override bool IsSupportNamedParameter => false;

    }
}
