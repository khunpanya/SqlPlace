namespace SqlPlace.Dialects
{
    class MSSQL: IDialect
    {
        public void RegisterDialect()
        {
            var dialectName = nameof(MSSQL);
            SqlDialect.RegisterSyntax(dialectName, nameof(SqlDialect.CurrentDate), arguments =>
            {
                return new SqlStatement("CONVERT(DATE, GETDATE())");
            });
        }
    }
}
