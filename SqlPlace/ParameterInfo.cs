namespace SqlPlace
{
    public struct ParameterInfo
    {
        public ParameterInfo(object value, System.Data.DbType? dbType = null, int? size = null)
        {
            _globalName = null;
            _parameterName = null;
            Value = value;
            DbType = dbType;
            SpecificDbType = null;
            Size = size;
            Direction = null;
        }

        internal string _globalName;

        internal string _parameterName;
        public string ParameterName => _parameterName;

        public object Value;

        public System.Data.DbType? DbType;

        /// <summary>
        /// To specify enum value of certain DB type such e.g. SqlDbType, OleDbType
        /// </summary>
        public int? SpecificDbType;

        public int? Size;

        public System.Data.ParameterDirection? Direction;
    }
}
