using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace SqlPlace.Extensions
{
    public static class ExtensionMethods
    {
        [DebuggerStepThrough()]
        public static int ExecuteNonQuery(this DbConnection conn, SqlStatement q, DbTransaction tx = null)
        {
            using (var command = q.ToCommand(conn))
            {
                if (tx != null) command.Transaction = tx;
                if (conn.State != ConnectionState.Open) conn.Open();
                return command.ExecuteNonQuery();
            }
        }

        [DebuggerStepThrough()]
        public static object ExecuteScalar(this DbConnection conn, SqlStatement q, DbTransaction tx = null)
        {
            using (var command = q.ToCommand(conn))
            {
                if (tx != null) command.Transaction = tx;
                if (conn.State != ConnectionState.Open) conn.Open();
                return command.ExecuteScalar();
            }
        }

        [DebuggerStepThrough()]
        public static IDataReader ExecuteReader(this DbConnection conn, SqlStatement q, DbTransaction tx = null)
        {
            using (var command = q.ToCommand(conn))
            {
                if (tx != null) command.Transaction = tx;
                if (conn.State != ConnectionState.Open) conn.Open();
                return command.ExecuteReader();
            }
        }

        [DebuggerStepThrough()]
        public static void ExecuteFill(this DbConnection conn, ref DataTable dt, SqlStatement q, DbTransaction tx = null)
        {
            using (var command = q.ToCommand(conn))
            {
                if (tx != null) command.Transaction = tx;
                var da = q.CommandFactory.CreateDataAdapter();
                da.SelectCommand = command;
                da.Fill(dt);
            }
        }

        [DebuggerStepThrough()]
        public static T ExecuteToDataTable<T>(this DbConnection conn, SqlStatement q, DbTransaction tx = null) where T : DataTable, new()
        {
            DataTable dt = new T();
            ExecuteFill(conn, ref dt, q, tx);
            return (T)dt;
        }

        [DebuggerStepThrough()]
        public static DataTable ExecuteToDataTable(this DbConnection conn, SqlStatement q, DbTransaction tx = null)
        {
            return ExecuteToDataTable<DataTable>(conn, q, tx);
        }

        [DebuggerStepThrough()]
        public static IEnumerable<T> ExecuteToValues<T>(this DbConnection conn, SqlStatement q, DbTransaction transaction = null)
        {
            IDataReader rdr = ExecuteReader(conn, q, transaction);
            try
            {
                while (rdr.Read())
                {
                    T o = default;
                    if(!rdr.IsDBNull(0)) 
                        o = (T)rdr.GetValue(0);
                    yield return o;
                }
            }
            finally
            {
                rdr.Close();
                rdr.Dispose();
            }
        }

        [DebuggerStepThrough()]
        public static IEnumerable<T> ExecuteToObjects<T>(this DbConnection conn, SqlStatement q, DbTransaction transaction = null) where T: new()
        {
            string[] propNames = typeof(T).GetProperties().Select(p => p.Name).ToArray();
            IDataReader rdr = ExecuteReader(conn, q, transaction);
            try
            {
                while (rdr.Read())
                {
                    T o = new T();
                    for (var i = 0; i <= rdr.FieldCount - 1; i++)
                    {
                        string fName = rdr.GetName(i);
                        if (propNames.Contains(fName))
                        {
                            PropertyInfo p = o.GetType().GetProperty(fName);
                            Type pType = p.PropertyType;
                            object fValue = pType.IsValueType ? Activator.CreateInstance(pType) : null;
                            if (!rdr.IsDBNull(i))
                                fValue = rdr.GetValue(i);
                            p.SetValue(o, fValue, null);
                        }
                    }
                    yield return o;
                }
            }
            finally
            {
                rdr.Close();
                rdr.Dispose();
            }
        }

        private static T DefaultValue<T>(T type)
        {
            var v = default(T);
            return v;
        }

        //[DebuggerStepThrough()]
        public static IEnumerable<IDictionary<string, object>> ExecuteToDictionaries(this DbConnection conn, SqlStatement q, DbTransaction transaction = null)
        {
            IDataReader rdr = ExecuteReader(conn, q, transaction);
            try
            {
                while (rdr.Read())
                {
                    var o = new Dictionary<string, object>();
                    for (var i = 0; i <= rdr.FieldCount - 1; i++)
                    {
                        string fName = rdr.GetName(i);
                        var fType = rdr.GetFieldType(i);
                        object fValue = DefaultValue(fType);
                        if (!rdr.IsDBNull(i))
                            fValue = rdr.GetValue(i);
                        o.Add(fName, fValue);
                    }
                    yield return o;
                }
            }
            finally
            {
                rdr.Close();
                rdr.Dispose();
            }
        }


        [DebuggerStepThrough()]
        public static IDictionary<string, object> ExtractProperties<T>(this T obj, params string[] propertyNames)
        {
            var result = new Dictionary<string, object>();
            if (propertyNames == null) propertyNames = new string[] { };
            if(obj is IDictionary<string, object>)
            {
                var dict = obj as IDictionary<string, object>;
                // Dictionary
                if(propertyNames.Count() == 0)
                {
                    // Get all
                    foreach(var key in dict.Keys)
                    {
                        result.Add(key, dict[key]);
                    }
                }else
                {
                    // Get some
                    foreach (string propName in propertyNames)
                    {
                        if (!dict.ContainsKey(propName))
                            throw new Exception($"Property name \"{propName}\" not found");
                        result.Add(propName, dict[propName]);
                    }
                }
            }
            else
            {
                // POCO
                if (propertyNames.Count() == 0)
                {
                    // Get all
                    var pis = obj.GetType().GetProperties();
                    foreach(var pi in pis)
                    {
                        result.Add(pi.Name, pi.GetValue(obj, null));
                    }
                }
                else
                {
                    // Get some
                    foreach (string propName in propertyNames)
                    {
                        var pi = obj.GetType().GetProperty(propName);
                        if (pi == null)
                            throw new Exception($"Property name \"{propName}\" not found");
                        result.Add(propName, pi.GetValue(obj, null));
                    }
                }
            }
            return result;
        }
    }
}
