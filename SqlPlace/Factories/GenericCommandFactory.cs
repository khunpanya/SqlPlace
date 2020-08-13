using System;
using System.Data;
using System.Data.Common;
using System.Reflection;

namespace SqlPlace.Factories
{
    public class GenericCommandFactory : ICommandFactory
    {
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
