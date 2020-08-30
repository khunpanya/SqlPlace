namespace SqlPlace.Dialects
{
    class Standard: IDialect
    {
        public void RegisterDialect()
        {
            SqlDialect.RegisterSyntax(string.Empty, nameof(SqlDialect.CurrentDate), arguments =>
            {
                return new SqlStatement("CURRENT_DATE");
            });
            SqlDialect.RegisterSyntax(string.Empty, nameof(SqlDialect.IsNull), arguments =>
            {
                var Expression = arguments[0] as SqlStatement;
                var Value = arguments[1] as SqlStatement;
                return new SqlStatement("ISNULL({0}, {1})", arguments[0], arguments[1]);
            });
            SqlDialect.RegisterSyntax(string.Empty, nameof(SqlDialect.Select), arguments =>
            {
                var Selection = arguments[0] as SqlStatement;
                var From = arguments[1] as SqlStatement;
                var Where = arguments[2] as SqlStatement;
                var result = new SqlStatement("SELECT {0}" +
                    System.Environment.NewLine + "FROM {1}" +
                    System.Environment.NewLine + "WHERE {2}");
                result.PlaceParameters(Selection, From, Where);
                return result;
            });
        }
    }
}
