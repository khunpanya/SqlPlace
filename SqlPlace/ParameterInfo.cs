namespace SqlPlace
{
    public struct ParameterInfo
    {
        public ParameterInfo(object value, System.Data.SqlDbType? sqlDbType = null, int? size = null)
        {
            _globalName = null;
            _parameterName = null;
            Value = value;
            SqlDbType = sqlDbType;
            Size = size;
            Direction = null;
        }

        internal string _globalName;

        internal string _parameterName;
        public string ParameterName => _parameterName;

        public object Value;

        public System.Data.SqlDbType? SqlDbType;

        public int? Size;

        public System.Data.ParameterDirection? Direction;
    }
}
