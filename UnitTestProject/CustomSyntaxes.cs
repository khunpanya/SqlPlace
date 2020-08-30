using SqlPlace;
using SqlPlace.Dialects;

namespace UnitTestProject
{
    public class CustomSyntaxes : IDialect
    {
        public void RegisterDialect()
        {
            var dialectName = "MSSQL";
            SqlDialect.RegisterSyntax(dialectName, nameof(YourOwnSyntax), arguments =>
            {
                var expression = arguments[0] as SqlStatement;
                int number = (int)arguments[1];
                SqlStatement result = $"YOUR_OWN({expression}, {number})";
                return result;
            });
        }

        public static SqlStatement YourOwnSyntax(SqlStatement expression, int number)
        {
            return SqlDialect.Syntax(nameof(YourOwnSyntax), new object[] { expression, number });
        }

    }
}
