using System.Data.Common;
using System.Data.OleDb;

namespace SqlPlace.Factories
{
    public class OleDbCommandFactory : GenericCommandFactory
    {
        private readonly static bool reg = Register<OleDbConnection, OleDbCommandFactory>();

        public OleDbCommandFactory() : base(OleDbFactory.Instance)
        {

        }

        public override string SpecificDbTypePropertyName => "OleDbType";

        public override bool IsSupportNamedParameter => false;

    }
}
