using System.Collections.Generic;

namespace SqlPlace
{
    public struct CommandInfo
    {
        public string CommandText;

        public ParameterInfo[] Parameters;

        //public IEnumerable<KeyValuePair<string, object>> GetParameterDictionary()
        //{
        //    var result = new List<KeyValuePair<string, object>>();
        //    foreach (var param in Parameters)
        //        result.Add(new KeyValuePair<string, object>(param.ParameterName, param.Value));
        //    return result;
        //}
    }
}
