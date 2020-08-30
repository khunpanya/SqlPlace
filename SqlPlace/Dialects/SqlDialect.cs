using System;
using System.Collections.Generic;
using System.Linq;

namespace SqlPlace.Dialects
{
    public sealed class SqlDialect
    {
        #region "Registration"
        private static Dictionary<string, Func<object[], SqlStatement>> _dialectRegistry = new Dictionary<string, Func<object[], SqlStatement>>();

        public static void RegisterSyntax(string dialectName, string syntaxName, Func<object[], SqlStatement> implementation)
        {
            _dialectRegistry[$"{dialectName}:{syntaxName}"] = implementation;
        }

        static bool init = false;
        static object initLock = new object();
        static void InitializeRegistry()
        {
            lock (initLock)
            {
                if (!init)
                {
                    // Scan for all dialects
                    var dialectTypes = (
                        from domainAssembly in AppDomain.CurrentDomain.GetAssemblies()
                        from assemblyType in domainAssembly.GetTypes()
                        where assemblyType.GetInterfaces().Contains(typeof(IDialect))
                        select assemblyType).ToArray();
                    foreach (var dialectType in dialectTypes)
                    {
                        var dialectInstance = Activator.CreateInstance(dialectType) as IDialect;
                        dialectInstance.RegisterDialect();
                    }
                    init = true;
                }
            }
        }

        public static SqlStatement DialectSyntax(string dialectName, string syntaxName, object[] arguments)
        {
            InitializeRegistry();
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
        #endregion

        public static SqlStatement CurrentDate()
        {
            return Syntax(nameof(CurrentDate), null);
        }

        public static SqlStatement IsNull(SqlStatement Expression, SqlStatement Value)
        {
            return Syntax(nameof(IsNull), new object[] { Expression, Value });
        }

        public static SqlStatement Select(SqlStatement Selection, SqlStatement From, SqlStatement Where, SqlStatement OrderBy = null, int? Offset = null, int? Fetch = null)
        {
            return Syntax(nameof(Select), new object[] { Selection, From, Where });
        }

        //internal static string IndentedNewLine(int n)
        //{
        //    return Environment.NewLine + string.Concat(Enumerable.Repeat("  ", n).ToArray());
        //}

    }
}
