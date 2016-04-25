using System.Collections.Generic;


namespace ImprovedAssetsPanel
{
    public class MultiMap<K, V>
    {
        private readonly Dictionary<K, List<V>> _dictionary = new Dictionary<K, List<V>>();

        public void Add(K key, V value)
        {
            List<V> list;
            if (this._dictionary.TryGetValue(key, out list))
            {
                list.Add(value);
            }
            else
            {
                list = new List<V> {value};
                this._dictionary[key] = list;
            }
        }

        public IEnumerable<K> Keys => this._dictionary.Keys;

        public List<V> this[K key]
        {
            get
            {
                List<V> list;
                if (this._dictionary.TryGetValue(key, out list))
                {
                    return list;
                }
                list = new List<V>();
                this._dictionary[key] = list;
                return list;
            }
        }

        public void Clear()
        {
            _dictionary.Clear();
        }
    }
}
