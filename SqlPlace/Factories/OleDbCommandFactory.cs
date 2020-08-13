﻿using System.Data.Common;
using System.Data.OleDb;

namespace SqlPlace.Factories
{
    public class OleDbCommandFactory : GenericCommandFactory
    {
        public OleDbCommandFactory() : base(OleDbFactory.Instance)
        {

        }

        public override void SetSpecificDbType(DbParameter parameter, int specificDbType)
        {
            (parameter as OleDbParameter).OleDbType = (OleDbType)specificDbType;
        }

        public override bool IsSupportNamedParameter()
        {
            return false;
        }
    }
}
