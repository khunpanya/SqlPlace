using System.Data.Common;
using System.Data.OleDb;

namespace SqlPlace.Factories
{
    public class OleDbCommandFactory : GenericCommandFactory
    {
        static int _ = Register<OleDbConnection, OleDbCommandFactory>();

        public OleDbCommandFactory() : base(OleDbFactory.Instance)
        {

        }

        public override string SpecificDbTypePropertyName()
        {
            return "OleDbType";
        }

        public override bool IsSupportNamedParameter()
        {
            return false;
        }
    }
}
