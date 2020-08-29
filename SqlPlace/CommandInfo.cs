using System.Collections.Generic;
using System.Linq;

namespace SqlPlace
{
    public struct CommandInfo
    {
        public string CommandText;

        public ParameterInfo[] Parameters;

        public IEnumerable<KeyValuePair<string, object>> GetParameterDictionary()
        {
            return Parameters.ToDictionary(p => p.ParameterName, p => p.Value);
        }

        public object[] GetParameterValues()
        {
            return Parameters.Select(p => p.Value).ToArray();
        }
    }
}
