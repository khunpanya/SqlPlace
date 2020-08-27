using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Reflection;

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
            if (obj is ParameterInfo) return true;
            if (obj.GetType().IsValueType) return true;
            if (obj is string) return true;
            if (obj is byte[]) return true;
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
            else if(value is object[])
            {
                // Change values array to SqlList
                PlaceParameter(localIndex, SqlList.CommaValues(value as object[]));
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

        public SqlStatement PlaceStatement(string globalName, string sql, object parameterObject)
        {
            if (parameterObject != null)
            {
                if (IsPlacable(parameterObject))
                {
                    return PlaceStatement(globalName, sql, new object[] { parameterObject });
                }
                else
                {
                    return PlaceStatement(globalName, sql, Extensions.ExtensionMethods.ExtractProperties(parameterObject));
                }
            } else
            {
                return PlaceStatement(globalName, sql);
            }
        }

        #endregion

        #region "DbCommand"
        public static Factories.ICommandFactory DefaultCommandFactory = new Factories.SqlCommandFactory();
        
        protected Factories.ICommandFactory _commandFactory;
        public Factories.ICommandFactory CommandFactory
        {
            get
            {
                if (_commandFactory == null)
                    _commandFactory = DefaultCommandFactory;
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

        public virtual DbCommand MakeCommand(DbConnection connection = null, DbTransaction transaction = null)
        {
            if(_commandFactory == null && connection != null)
            {
                CommandFactory = Factories.GenericCommandFactory.Resolve(connection);
            }
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
                var parameter = CommandFactory.CreateParameter();
                parameter.ParameterName = param.ParameterName;
                parameter.Value = pValue;
                if (param.DbType.HasValue) 
                    parameter.DbType = param.DbType.Value;
                if (param.SpecificDbType.HasValue)
                    parameter.GetType().GetProperty(CommandFactory.SpecificDbTypePropertyName(), BindingFlags.Instance | BindingFlags.Public)
                        .SetValue(parameter, param.SpecificDbType.Value, null);
                if (param.Size.HasValue) 
                    parameter.Size = param.Size.Value;
                if (param.Direction.HasValue) 
                    parameter.Direction = param.Direction.Value;
                param.OnParameterCreated?.Invoke(parameter);
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
            foreach (var statement in AllNamedStatements().Values) 
                statement.CommandFactory = this.CommandFactory;
            int paramOffset = 0;
            var cmdInfo = MakeRecursive(ref paramOffset);
            if(!CommandFactory.IsSupportNamedParameter())
            {
                // Redetermine parameter order from CommandText
                var parameterInOrder = new List<ParameterInfo>();
                var paramPattern = new System.Text.RegularExpressions.Regex(@"\{([\d\w]+)\}");
                int placeHolderPosition = 0;
                cmdInfo.CommandText = paramPattern.Replace(cmdInfo.CommandText, 
                    delegate (System.Text.RegularExpressions.Match match) {
                        var token = match.Groups[0].Value;
                        var param = cmdInfo.Parameters.Where(p => p.ParameterName == token).FirstOrDefault();
                        if(param._parameterName != null)
                        {
                            if (CommandType == CommandType.StoredProcedure)
                            {
                                // Retain name for StoredProcedure
                                var name = match.Groups[1].Value;
                                if (name.All(Char.IsNumber))
                                    param._parameterName = CommandFactory.GetParameterName(int.Parse(name));
                                else
                                    param._parameterName = CommandFactory.GetParameterName(param._globalName);
                            }
                            else
                            {
                                param._globalName = null;
                                param._parameterName = CommandFactory.GetParameterName(placeHolderPosition);
                            }
                            parameterInOrder.Add(param);
                        }
                        placeHolderPosition += 1;
                        return CommandFactory.GetParameterPlaceholder(placeHolderPosition);
                    });
                if (CommandType == CommandType.StoredProcedure && parameterInOrder.Count == 0)
                {
                    for (int i = 0; i < cmdInfo.Parameters.Length; i++)
                    {
                        var name = cmdInfo.Parameters[i]._parameterName.Trim('{', '}');
                        if (name.All(Char.IsNumber))
                            cmdInfo.Parameters[i]._parameterName = CommandFactory.GetParameterName(int.Parse(name));
                        else
                            cmdInfo.Parameters[i]._parameterName = CommandFactory.GetParameterName(cmdInfo.Parameters[i]._globalName);
                    }
                }
                else
                {
                    cmdInfo.Parameters = parameterInOrder.ToArray();
                }
            }
            return cmdInfo;
        }

        private string MakeParameterName(int index)
        {
            string paramName;
            if (CommandFactory.IsSupportNamedParameter())
                paramName = CommandFactory.GetParameterName(index); // Final parameter name
            else
                paramName = "{" + (index).ToString() + "}"; // Intermediate parameter index
            return paramName;
        }
        private string MakeParameterName(string name)
        {
            string paramName;
            if (CommandFactory.IsSupportNamedParameter())
                paramName = CommandFactory.GetParameterName(name); // Final parameter name
            else
                paramName = "{" + name + "}"; // Intermediate parameter name
            return paramName;
        }
        protected virtual CommandInfo MakeRecursive(ref int paramOffset)
        {
            string commandText = _sql;
            List<ParameterInfo> parameters = new List<ParameterInfo>();

            commandText = commandText.Replace("{{", "︷").Replace("}}", "︸");

            // Process indexed parameters
            var indexedParameters = IndexedParameters();
            var numericPattern = new System.Text.RegularExpressions.Regex(@"\{(\d+)\}");
            var matches = numericPattern.Matches(commandText);
            // ... CommantText
            var maxLocalIndex = -1;
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                var token = match.Groups[0].Value;
                int localIndex = int.Parse(match.Groups[1].Value);
                if (localIndex > maxLocalIndex) maxLocalIndex = localIndex;
                string paramName = MakeParameterName(paramOffset + localIndex);
                commandText = commandText.Replace(token, paramName);
            }
            // ... Parameters
            for (int i = 0; i < indexedParameters.Length; i++)
            {
                var param = indexedParameters[i];
                string paramName = MakeParameterName(paramOffset + i);
                param._parameterName = paramName;
                parameters.Add(param);
            }
            if (parameters.Count-1 > maxLocalIndex) maxLocalIndex = parameters.Count-1;
            paramOffset += maxLocalIndex+1;

            // Process named token
            var allNamedParameters = Root.AllNamedParameters();
            var allNamedStatements = Root.AllNamedStatements();
            var stringPattern = new System.Text.RegularExpressions.Regex(@"\{(\w+?)\}");
            matches = stringPattern.Matches(commandText);
            // ... CommandText
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
                        var childCmdInfo = child.MakeRecursive(ref paramOffset);
                        commandText = commandText.Replace(token, childCmdInfo.CommandText);
                        parameters.AddRange(childCmdInfo.Parameters);
                    }
                }
                else
                {
                    // parameter
                    string paramName = MakeParameterName(globalName);
                    commandText = commandText.Replace(token, paramName);
                }
            }
            // ... Parameters
            if (this == Root)
            {
                foreach (string globalName in allNamedParameters.Keys)
                {
                    var param = allNamedParameters[globalName];
                    string paramName = MakeParameterName(globalName);
                    param._parameterName = paramName;
                    parameters.Add(param);
                }
            }

            commandText = commandText.Replace("︷", "{").Replace("︸", "}");

            var result = new CommandInfo() { CommandText = commandText };
            result.Parameters = parameters.ToArray();
            return result;
        }

        public string MakeText()
        {
            foreach (var statement in AllNamedStatements().Values)
                statement.CommandFactory = this.CommandFactory;
            int paramOffset = 0;
            var cmdInfo = MakeRecursive(ref paramOffset);
            if (!CommandFactory.IsSupportNamedParameter())
            {
                // Redetermine parameter order
                var paramPattern = new System.Text.RegularExpressions.Regex(@"\{([\d\w]+)\}");
                cmdInfo.CommandText = paramPattern.Replace(cmdInfo.CommandText,
                    delegate (System.Text.RegularExpressions.Match match) {
                        var token = match.Groups[0].Value;
                        var param = cmdInfo.Parameters.Where(p => p.ParameterName == token).FirstOrDefault();
                        return QuoteValue(param.Value);
                    });
            }
            else
            {
                for (int i = 0; i < cmdInfo.Parameters.Count(); i++)
                {
                    cmdInfo.CommandText = cmdInfo.CommandText.Replace(cmdInfo.Parameters[i].ParameterName, QuoteValue(cmdInfo.Parameters[i].Value));
                }
            }
            return cmdInfo.CommandText;
        }
        private static string QuoteValue(object value)
        {
            if (value == null || value == DBNull.Value)
                return "null";
            else if (value is string)
                return "'" + ((string)value).Replace("'", "''") + "'";
            else if (value is DateTime)
                return "'" + ((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.GetCultureInfo("en-GB")) + "'";
            else if (value is bool)
                return ((bool)value) ? "1" : "0";
            else
                return value.ToString();
        }
        #endregion

    }

}
