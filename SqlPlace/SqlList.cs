using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace SqlPlace
{
    public class SqlList: SqlStatement, IEnumerable<SqlStatement>
    {
        public string Separator { get; }
        public string EmptyString { get; }

        List<SqlStatement> items = new List<SqlStatement>();

        public IEnumerator<SqlStatement> GetEnumerator()
        {
            return items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return items.GetEnumerator();
        }

        public SqlList(string separator, string emptyString = null): base(string.Empty)
        {
            Separator = separator;
            EmptyString = emptyString + string.Empty;
            _sql = EmptyString;
        }

        public void Add(SqlStatement statement)
        {
            items.Add(statement);
            Func<int, string> nameFromHash = i => "I" + GetHashCode().ToString() + "_" + i.ToString();
            var localIndex = items.Count - 1;
            if (localIndex == 0)
            {
                _sql = string.Empty;
            }else
            {
                _sql += Separator;
            }
            _sql += "{" + nameFromHash(localIndex) + "}";
            PlaceStatement(nameFromHash(localIndex), statement);
        }

        public void Add(string sql, params object[] indexedParameters)
        {
            var item = new SqlStatement(sql, indexedParameters);
            Add(item);
        }

        public void AddRange(IEnumerable<SqlStatement> statements)
        {
            foreach (var stmt in statements) 
                Add(stmt);
        }

        public void AddRange(IEnumerable<string> listOfSql)
        {
            AddRange(listOfSql.Select(sql => new SqlStatement(sql)));
        }

        public void AddRange(IEnumerable<KeyValuePair<string, object>> listOfSqlAndParameter)
        {
            AddRange(listOfSqlAndParameter.Select(kv => new SqlStatement(kv.Key, kv.Value)));
        }

        public void AddRange(IEnumerable<KeyValuePair<string, object[]>> listOfSqlAndParameters)
        {
            AddRange(listOfSqlAndParameters.Select(kv => new SqlStatement(kv.Key, kv.Value)));
        }

        #region "Additional Constructors"
        public static SqlList AndClauses(IEnumerable<SqlStatement> conditions = null, string emptyString = "(1=1)")
        {
            var list = new SqlList(" and ", emptyString);
            if(conditions != null) list.AddRange(conditions);
            return list;
        }

        public static SqlList CommaClauses(IEnumerable<string> listOfSql = null, string emptyString = null)
        {
            var list = new SqlList(", ", emptyString);
            if (listOfSql != null) list.AddRange(listOfSql);
            return list;
        }

        public static SqlList CommaValues(IEnumerable<object> listOfValue = null, string emptyString = null)
        {
            var list = new SqlList(", ", emptyString);
            if(listOfValue != null) list.AddRange(listOfValue.Select(v => new KeyValuePair<string, object>("{0}", v)));
            return list;
        }

        public static SqlList CommaAssignments(IDictionary<string, object> listOfNameAndValue = null, string emptyString = null)
        {
            var list = new SqlList(", ", emptyString);
            if (listOfNameAndValue != null) list.AddRange(listOfNameAndValue.Select(kv => new KeyValuePair<string, object>(kv.Key + "={0}", kv.Value)));
            return list;
        }

        public static SqlList CommaAssignments(object parameterObject, string emptyString = null)
        {
            var list = new SqlList(", ", emptyString);
            if (parameterObject != null) list.AddRange(Extensions.ExtensionMethods.ExtractProperties(parameterObject));
            return list;
        }

        #endregion

    }
}
