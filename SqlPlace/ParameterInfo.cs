using System.Data;
using System.Data.Common;

namespace SqlPlace
{
    public struct ParameterInfo
    {
        public ParameterInfo(object value, DbType? dbType = null, int? size = null)
        {
            _globalName = null;
            _parameterName = null;
            Value = value;
            DbType = dbType;
            SpecificDbType = null;
            Size = size;
            Direction = null;
            OnParameterCreated = null;
        }

        public delegate void ParameterCreatedCallback(DbParameter parameter);

        internal string _globalName;

        internal string _parameterName;
        public string ParameterName => _parameterName;

        public object Value;

        public DbType? DbType;

        /// <summary>
        /// To specify enum value of certain DB type such e.g. SqlDbType, OleDbType
        /// </summary>
        public int? SpecificDbType;

        public int? Size;

        public ParameterDirection? Direction;

        public ParameterCreatedCallback OnParameterCreated;

        public override string ToString()
        {
            return $"{ParameterName}, {Value}";
        }
    }
}
