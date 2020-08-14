using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;

namespace SqlPlace.Factories
{
    public class GenericCommandFactory : ICommandFactory
    {
        #region "Registry"
        internal static Dictionary<Type, ICommandFactory> _registry = new Dictionary<Type, ICommandFactory>();
        protected static int Register<TDbConnection, TCommandFactory>()
            where TDbConnection : DbConnection
            where TCommandFactory : ICommandFactory, new()
        {
            _registry.Add(typeof(TDbConnection), new TCommandFactory());
            return _registry.Count;
        }
        public static ICommandFactory Resolve<TDbConnection>(TDbConnection connection) where TDbConnection : DbConnection
        {
            if(_registry.ContainsKey(connection.GetType()))
            {
                return _registry[connection.GetType()];
            } else
            {
                // Try best to determine DbProviderFactory from DbConnection 
                // (Due to .net3.5 cannot simply determine DbProviderFactory from DbConnection)

                Type TConn = connection.GetType();
                DbProviderFactory providerFactory = null;
                PropertyInfo prop1 = TConn.GetProperty("DbProviderFactory", BindingFlags.Instance | BindingFlags.NonPublic);
                if (prop1 != null) providerFactory = prop1.GetValue(connection, null) as DbProviderFactory;
                if (providerFactory != null)
                {
                    return new GenericCommandFactory(providerFactory);
                }

                object connFactory = null;
                PropertyInfo prop2 = TConn.GetProperty("ConnectionFactory", BindingFlags.Instance | BindingFlags.NonPublic);
                if (prop2 != null) connFactory = prop2.GetValue(connection, null);
                if (connFactory != null)
                {
                    PropertyInfo prop3 = connFactory.GetType().GetProperty("ProviderFactory", BindingFlags.Instance | BindingFlags.Public);
                    if(prop3 != null) providerFactory = prop3.GetValue(connFactory, null) as DbProviderFactory;
                    if (providerFactory != null)
                    {
                        return new GenericCommandFactory(providerFactory);
                    }
                }

                var segments = connection.GetType().FullName.Split('.');
                var providerName = string.Join(".", segments.Take(segments.Length - 1).ToArray());
                try
                {
                    providerFactory = DbProviderFactories.GetFactory(providerName);
                    return new GenericCommandFactory(providerFactory);
                }
                catch (Exception ex)
                {
                    throw new Exception("Unable to determine CommandFactory from DbConnection.", ex);
                }
            }
        }
        #endregion

        /// <param name="providerName">E.g. "System.Data.SqlClient"</param>
        public GenericCommandFactory(string providerName): this(DbProviderFactories.GetFactory(providerName))
        {
        }

        public GenericCommandFactory(DbProviderFactory providerFactory)
        {
            FactoryInstance = providerFactory;
        }

        protected DbProviderFactory FactoryInstance { get; }

        private DbCommandBuilder _builderInstance;
        protected DbCommandBuilder BuilderInstance
        {
            get
            {
                if(_builderInstance == null)
                {
                    _builderInstance = FactoryInstance.CreateCommandBuilder();
                }
                return _builderInstance;
            }
        }

        public virtual DbCommand CreateCommand()
        {
            return FactoryInstance.CreateCommand();
        }

        public virtual DbParameter CreateParameter()
        {
            return FactoryInstance.CreateParameter();
        }

        public virtual DbDataAdapter CreateDataAdapter()
        {
            return FactoryInstance.CreateDataAdapter();
        }

        public virtual string GetParameterName(int paramIndex)
        {
            var methodInfo = typeof(DbCommandBuilder).GetMethod("GetParameterName",
                BindingFlags.Instance | BindingFlags.NonPublic, Type.DefaultBinder, new[] { typeof(int) }, null);
            var result = methodInfo.Invoke(BuilderInstance, new object[] { paramIndex }) as string;
            return result;
        }

        public virtual string GetParameterName(string paramName)
        {
            var methodInfo = typeof(DbCommandBuilder).GetMethod("GetParameterName",
                BindingFlags.Instance | BindingFlags.NonPublic, Type.DefaultBinder, new[] { typeof(string) }, null);
            var result = methodInfo.Invoke(BuilderInstance, new object[] { paramName }) as string;
            return result;
        }

        public virtual string GetParameterPlaceholder(int paramIndex)
        {
            var methodInfo = typeof(DbCommandBuilder).GetMethod("GetParameterPlaceholder",
                BindingFlags.Instance | BindingFlags.NonPublic, Type.DefaultBinder, new[] { typeof(int) }, null);
            var result = methodInfo.Invoke(BuilderInstance, new object[] { paramIndex }) as string;
            return result;
        }

        private string specificPropName = null;
        public virtual void SetSpecificDbType(DbParameter parameter, int specificDbType)
        {
            // Assume that the specific DbType propertie's name is the same as one in constructors
            if(specificPropName == null)
            {
                System.Reflection.ParameterInfo[] ctorParams;
                foreach (var ctor in parameter.GetType().GetConstructors())
                {
                    if ((ctorParams = ctor.GetParameters()).Length == 2 && ctorParams[1].ParameterType.Name.EndsWith("DbType"))
                    {
                        specificPropName = ctorParams[1].ParameterType.Name;
                        break;
                    }
                }
            }
            if(!string.IsNullOrEmpty(specificPropName))
            {
                parameter.GetType().GetProperty(specificPropName,
                    BindingFlags.Instance | BindingFlags.Public).SetValue(parameter, specificDbType, null);
            }
            else
            {
                throw new Exception("Unable to determine specific DbType propertie's name. A concrete implementation for this DB provider is required.");
            }
        }

        private bool? _supportNamedParameter;
        public virtual bool IsSupportNamedParameter()
        {
            if (!_supportNamedParameter.HasValue)
                _supportNamedParameter = (GetParameterPlaceholder(42).Contains("42"));
            return _supportNamedParameter.Value;
        }

    }

    public class GenericCommandFactory<TDbProviderFactory> : GenericCommandFactory where TDbProviderFactory : DbProviderFactory
    {
        public GenericCommandFactory(): base(typeof(TDbProviderFactory).GetField("Instance",
                BindingFlags.Public | BindingFlags.Static | BindingFlags.GetField).GetValue(null) as DbProviderFactory)
        {

        }
    }
}
