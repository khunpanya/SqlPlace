using System;
using System.Data;
using System.Data.Common;
using System.Reflection;

namespace SqlPlace.Factories
{
    public class GenericCommandFactory<TDbProviderFactory> : ICommandFactory where TDbProviderFactory : DbProviderFactory
    {
        private DbProviderFactory _factoryInstance;
        private DbProviderFactory FactoryInstance
        {
            get
            {
                if(_factoryInstance == null)
                {
                    var fieldInfo = typeof(TDbProviderFactory).GetField("Instance", BindingFlags.Public | BindingFlags.Static | BindingFlags.GetField);
                    _factoryInstance = fieldInfo.GetValue(null) as DbProviderFactory;
                }
                return _factoryInstance;
            }
        }

        public DbCommand CreateCommand()
        {
            return FactoryInstance.CreateCommand();
        }

        public DbParameter CreateParameter()
        {
            return FactoryInstance.CreateParameter();
        }

        public virtual void SetSpecificDbType(DbParameter parameter, int specificDbType)
        {

        }

        public DbDataAdapter CreateDataAdapter()
        {
            return FactoryInstance.CreateDataAdapter();
        }

        public virtual bool SupportNamedParameter()
        {
            return true;
        }

        public string ParameterSymbol { get; set; } = "@";

        public string GetParameterName(int paramIndex)
        {
            return $"{ParameterSymbol}p{paramIndex}";
        }

        public string GetParameterName(string paramName)
        {
            return $"{ParameterSymbol}{paramName}";
        }
    }
}
