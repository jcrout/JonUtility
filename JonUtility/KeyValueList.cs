using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JonUtility
{
    public class KeyValueList<TKey, TValue> : List<KeyValuePair<TKey, TValue>>
    {
        public void Add(TKey key, TValue value)
        {            
            Add(new KeyValuePair<TKey, TValue>(key, value));
        }
    }

    public class KeyValueStringList : List<KeyValuePair<string, string>>
    {
        public void Add(string key, string value)
        {
            Add(new KeyValuePair<string, string>(key, value));
        }
    }
}
