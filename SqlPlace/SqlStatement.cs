using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;

namespace SqlPlace
{
    public class SqlStatement
    {
        protected string _sql;

        public SqlStatement(string sql, params object[] indexedParameters)
        {
            _sql = sql;
            PlaceParameters(indexedParameters);
        }

        public SqlStatement(string sql, IDictionary<string, object> namedParameters)
        {
            _sql = sql;
            PlaceParameters(namedParameters);
        }

        public SqlStatement(string sql, object parameterObject)
        {
            _sql = sql;
            if ( parameterObject != null )
            {
                if (IsPlacable(parameterObject))
                {
                    PlaceParameters(new object[] { parameterObject });
                }
                else
                {
                    PlaceParameters(Extensions.ExtensionMethods.ExtractProperties(parameterObject));
                }
            }
        }

        internal bool IsPlacable(object obj)
        {
            if (obj.GetType().IsValueType) return true;
            if (obj is string) return true;
            if (obj is byte[]) return true;
            if (obj is ParameterInfo) return true;
            if (obj is SqlStatement) return true;
            return false;
        }

        #region "Indexed Parameters"
        protected Dictionary<int, ParameterInfo> _indexedParameters = new Dictionary<int, ParameterInfo>();
        internal ParameterInfo[] IndexedParameters()
        {
            if (_indexedParameters.Count == 0) return new ParameterInfo[] { };
            return Enumerable.Range(0, _indexedParameters.Keys.Max() + 1)
                .Select(i => _indexedParameters[i])
                .ToArray();
        }

        public void PlaceParameter(int localIndex, object value)
        {
            if (value is SqlStatement)
            {
                // Create the nested statement instead
                Func<int, string> nameFromHash = i => "Q" + GetHashCode().ToString() + "_" + i.ToString();
                _sql = _sql.Replace("{" + localIndex.ToString() + "}", "{" + nameFromHash(localIndex) + "}");
                PlaceStatement(nameFromHash(localIndex), value as SqlStatement);
            }
            else
            {
                ParameterInfo param;
                if (value is ParameterInfo)
                {
                    param = (ParameterInfo)value;
                }
                else
                {
                    param = new ParameterInfo(value);
                }
                _indexedParameters[localIndex] = param;
            }
        }

        public void PlaceParameters(params object[] indexedParameters)
        {
            for (int i = 0; i < indexedParameters.Length; i++)
                PlaceParameter(i, indexedParameters[i]);
        }
        #endregion

        #region "Named Parameters"
        protected Dictionary<string, ParameterInfo> _namedParameters = new Dictionary<string, ParameterInfo>();

        internal IDictionary<string, ParameterInfo> AllNamedParameters()
        {
            var result = new Dictionary<string, ParameterInfo>();
            foreach (var key in _namedParameters.Keys)
            {
                result[key] = _namedParameters[key];
            }
            foreach (var stmt in AllNamedStatements().Values)
            {
                foreach(var key in stmt._namedParameters.Keys)
                {
                    result[key] = stmt._namedParameters[key];
                }
            }
            return result;
        }

        public void PlaceParameter(string globalName, object value)
        {
            if (value is SqlStatement)
            {
                // Create the nested statement instead
                PlaceStatement(globalName, value as SqlStatement);
            }
            else
            {
                ParameterInfo param;
                if (value is ParameterInfo)
                {
                    param = (ParameterInfo)value;
                }
                else
                {
                    param = new ParameterInfo(value);
                }
                param._globalName = globalName;
                _namedParameters[globalName] = param;
            }
        }

        public void PlaceParameters(IDictionary<string, object> namedParameters)
        {
            foreach (string globalName in namedParameters.Keys)
                PlaceParameter(globalName, namedParameters[globalName]);
        }

        public void PlaceParameters(object parameterObject)
        {
            if (parameterObject != null)
            {
                if (IsPlacable(parameterObject))
                {
                    PlaceParameters(new object[] { parameterObject });
                }
                else
                {
                    PlaceParameters(Extensions.ExtensionMethods.ExtractProperties(parameterObject));
                }
            }
        }
        #endregion

        #region "Nested Statements"
        protected SqlStatement _root;
        public SqlStatement Root { get => (_root == null) ? this : _root; }

        protected Dictionary<string, SqlStatement> _namedStatements = new Dictionary<string, SqlStatement>();
        
        internal IDictionary<string, SqlStatement> AllNamedStatements()
        {
            var result = new Dictionary<string, SqlStatement>();
            foreach(var key in _namedStatements.Keys)
            {
                result[key] = _namedStatements[key];
                var subs = _namedStatements[key].AllNamedStatements();
                foreach(var subkey in subs.Keys)
                {
                    result[subkey] = subs[subkey];
                }
            }
            return result;
        }

        private bool CircularReferenceFound(SqlStatement against)
        {
            var allNamedStatements = against.AllNamedStatements();
            return allNamedStatements.Values.Contains(this);
        }
        public SqlStatement PlaceStatement(string globalName, SqlStatement statement)
        {
            if (statement == this) throw new ArgumentException("Unable to place to itself");
            if (CircularReferenceFound(statement)) throw new ArgumentException("Circular referencing detected");

            statement._root = Root;
            statement.CommandFactory = CommandFactory;
            _namedStatements[globalName] = statement;

            return statement;
        }

        public SqlStatement PlaceStatement(string globalName, string sql, params object[] indexedParameters)
        {
            SqlStatement child = new SqlStatement(sql, indexedParameters);
            return PlaceStatement(globalName, child);
        }

        public SqlStatement PlaceStatement(string globalName, string sql, IDictionary<string, object> namedParameters)
        {
            SqlStatement child = new SqlStatement(sql, namedParameters);
            return PlaceStatement(globalName, child);
        }

        //public SqlStatement PlaceStatement<T>(string globalName, string sql, T paramObj) where T: class
        //{
        //    return PlaceStatement(globalName, sql, Extensions.ExtensionMethods.ExtractProperties(paramObj));
        //}


        #endregion

        #region "DbCommand"
        protected Factories.ICommandFactory _commandFactory;
        public Factories.ICommandFactory CommandFactory
        {
            get
            {
                if (_commandFactory == null)
                    _commandFactory = new Factories.SqlCommandFactory();
                return _commandFactory;
            }
            set
            {
                _commandFactory = value;
            }
        }

        /// <summary>
        /// CommandType of the command that built from this query
        /// </summary>
        public CommandType CommandType { get; set; } = CommandType.Text;

        /// <summary>
        /// The time in seconds to wait for the command to execute.
        /// </summary>
        public int Timeout { get; set; } = 30;

        public virtual DbCommand ToCommand(DbConnection connection = null, DbTransaction transaction = null)
        {
            var cmd = CommandFactory.CreateCommand();
            cmd.CommandType = CommandType;
            cmd.CommandTimeout = Timeout;
            cmd.Connection = connection;
            cmd.Transaction = transaction;
            var cmdInfo = Make();
            cmd.CommandText = cmdInfo.CommandText;
            _dbParameters.Clear();
            foreach (var param in cmdInfo.Parameters)
            {
                var pValue = param.Value;
                if (pValue == null) pValue = DBNull.Value;
                var parameter = CommandFactory.CreateParameter(param.ParameterName, pValue, param.DbType, param.Size, param.Direction);
                cmd.Parameters.Add(parameter);
                if (param._globalName != null)
                    _dbParameters.Add(param._globalName, parameter);
            }
            return cmd;
        }

        internal Dictionary<string, DbParameter> _dbParameters = new Dictionary<string, DbParameter>();

        public object ParameterValue(string globalName)
        {
            return _dbParameters[globalName].Value;
        }
        public CommandInfo Make()
        {
            int paramOffset = 0;
            var cmdInfo = Make(ref paramOffset);
            return cmdInfo;
        }

        protected virtual CommandInfo Make(ref int paramOffset)
        {
            string commandText = _sql;
            List<ParameterInfo> parameters = new List<ParameterInfo>();

            // Process indexed parameters
            var indexedParameters = IndexedParameters();
            var numericPattern = new System.Text.RegularExpressions.Regex(@"\{(\d+)\}");
            var matches = numericPattern.Matches(commandText);
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                var token = match.Groups[0].Value;
                int localIndex = int.Parse(match.Groups[1].Value);
                if (localIndex > indexedParameters.Length)
                    throw new System.Exception($"Parameter {token} has not been assigned.");
                var paramName = CommandFactory.GetParameterName(paramOffset + localIndex);
                commandText = commandText.Replace(token, paramName);
            }
            for (int i = 0; i < indexedParameters.Length; i++)
            {
                var param = indexedParameters[i];
                var paramName = CommandFactory.GetParameterName(paramOffset + i);
                param._parameterName = paramName;
                parameters.Add(param);
            }
            paramOffset += parameters.Count;

            // Process named token
            var allNamedParameters = Root.AllNamedParameters();
            var allNamedStatements = Root.AllNamedStatements();
            var stringPattern = new System.Text.RegularExpressions.Regex(@"\{(.+?)\}");
            matches = stringPattern.Matches(commandText);
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                var token = match.Groups[0].Value;
                string globalName = match.Groups[1].Value;
                if (allNamedStatements.ContainsKey(globalName))
                {
                    // child
                    var child = allNamedStatements[globalName];
                    if (child is SqlStatement)
                    {
                        var childCmdInfo = (child as SqlStatement).Make(ref paramOffset);
                        commandText = commandText.Replace(token, childCmdInfo.CommandText);
                        parameters.AddRange(childCmdInfo.Parameters);
                    }
                }
                else if (allNamedParameters.ContainsKey(globalName))
                {
                    // parameter
                    var paramName = CommandFactory.GetParameterName(globalName);
                    commandText = commandText.Replace(token, paramName);
                }
                else
                {
                    throw new System.Exception($"Parameter {token} has not been assigned.");
                }
            }
            if (this == Root)
            {
                foreach (string globalName in allNamedParameters.Keys)
                {
                    var param = allNamedParameters[globalName];
                    var paramName = CommandFactory.GetParameterName(globalName);
                    param._parameterName = paramName;
                    parameters.Add(param);
                }
            }

            var result = new CommandInfo() { CommandText = commandText };
            result.Parameters = parameters.ToArray();
            return result;
        }

        public string PlainText()
        {
            var cmdInfo = Make();
            var sql = cmdInfo.CommandText;
            for (int i = 0; i < cmdInfo.Parameters.Count(); i++)
            {
                sql = sql.Replace(CommandFactory.GetParameterName(i), Quote(cmdInfo.Parameters[i].Value));
            }
            return sql;
        }
        private static string Quote(object obj)
        {
            if (obj == null || obj == DBNull.Value)
                return "null";
            else if (obj is string)
                return "'" + ((string)obj).Replace("'", "''") + "'";
            else if (obj is DateTime)
                return "'" + ((DateTime)obj).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.GetCultureInfo("en-GB")) + "'";
            else if (obj is bool)
                return ((bool)obj) ? "1" : "0";
            else
                return obj.ToString();
        }
        #endregion

    }

    //public class SqlStatement<T> : SqlStatement where T: class
    //{
    //    public SqlStatement(string sql, params object[] indexedParameters): base(sql, indexedParameters)
    //    {
    //    }

    //    public SqlStatement(string sql, IDictionary<string, object> namedParameters): base(sql, namedParameters)
    //    {
    //    }

    //    public SqlStatement(string sql, T paramObj): base(sql, Extensions.ExtensionMethods.ExtractProperties(paramObj))
    //    {
    //    }

    //}
}
