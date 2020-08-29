using System;
using System.Collections.Generic;
using System.Linq;

namespace SqlPlace
{
    public class SqlDialect
    {
        private static Dictionary<string, Func<object[], SqlStatement>> _dialectRegistry = new Dictionary<string, Func<object[], SqlStatement>>();

        public static void RegisterSyntax(string dialectName, string syntaxName, Func<object[], SqlStatement> implementation)
        {
            _dialectRegistry[$"{dialectName}:{syntaxName}"] = implementation;
        }

        public static SqlStatement DialectSyntax(string dialectName, string syntaxName, object[] arguments)
        {
            var key = $"{dialectName}:{syntaxName}";
            if (!_dialectRegistry.ContainsKey(key))
            {
                key = $":{syntaxName}";
            }
            if(!_dialectRegistry.ContainsKey(key))
            {
                throw new Exception($"There is no syntax '{syntaxName}' registered for dialect '{dialectName}'");
            }
            return _dialectRegistry[key].Invoke(arguments);
        }

        private static string _defaultDialectName;
        public static string DefaultDialectName 
        { 
            get
            {
                if(_defaultDialectName == null)
                {
                    return SqlStatement.DefaultCommandFactory.FactoryDialectName;
                }
                return _defaultDialectName;
            }
            set
            {
                _defaultDialectName = value;
            }
        }

        public static SqlStatement Syntax(string syntaxName, object[] arguments)
        {
            return DialectSyntax(DefaultDialectName, syntaxName, arguments);
        }

        #region "Standard Syntax"

        public static SqlStatement CurrentDate()
        {
            return Syntax(nameof(CurrentDate), null);
        }

        public static SqlStatement IsNull(SqlStatement Expression, SqlStatement Value)
        {
            return Syntax(nameof(IsNull), new object[] { Expression, Value });
        }

        public static SqlStatement Select(SqlStatement Selection, SqlStatement From, SqlStatement Where)
        {
            return Syntax(nameof(Select), new object[] { Selection, From, Where });
        }

        private static readonly bool reg = RegisterStandardSyntax();

        private static bool RegisterStandardSyntax()
        {
            RegisterSyntax(string.Empty, nameof(SqlDialect.CurrentDate), arguments =>
            {
                return new SqlStatement("CURRENT_DATE");
            });
            RegisterSyntax(string.Empty, nameof(SqlDialect.IsNull), arguments =>
            {
                var Expression = arguments[0] as SqlStatement;
                var Value = arguments[1] as SqlStatement;
                return new SqlStatement("ISNULL({0}, {1})", arguments[0], arguments[1]);
            });
            RegisterSyntax(string.Empty, nameof(SqlDialect.Select), arguments =>
            {
                var Selection = arguments[0] as SqlStatement;
                var From = arguments[1] as SqlStatement;
                var Where = arguments[2] as SqlStatement;
                var result = new SqlStatement("SELECT {0}" +
                    "FROM {1}" +
                    "WHERE {2}");
                result.PlaceParameters(Selection, From, Where);
                return result;
            });
            return true;
        }

        #endregion

        //internal static string IndentedNewLine(int n)
        //{
        //    return Environment.NewLine + string.Concat(Enumerable.Repeat("  ", n).ToArray());
        //}

    }
}
