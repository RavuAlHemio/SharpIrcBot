using System;
using System.Collections.Generic;

namespace SharpIrcBot.Plugins.Stats
{
    public class DrillDownValue<K, V>
    {
        public List<K> Keys { get; set; }
        public Dictionary<string, V> Values { get; set; }
        public List<DrillDownValue<K, V>> Children { get; set; }

        public DrillDownValue()
        {
            Keys = new List<K>();
            Values = new Dictionary<string, V>();
            Children = new List<DrillDownValue<K, V>>();
        }

        public IEnumerable<DrillDownValue<K, V>> FlattenedDescendants()
        {
            var queue = new Queue<DrillDownValue<K, V>>();
            queue.Enqueue(this);

            while (queue.Count > 0)
            {
                var val = queue.Dequeue();
                yield return val;

                foreach (var child in val.Children)
                {
                    queue.Enqueue(child);
                }
            }
        }

        public V TotalValueForMetric(string metricName, V initialValue, V missingValue, Func<V, V, V> meldValueFunc)
        {
            V total = initialValue;

            foreach (var value in FlattenedDescendants())
            {
                V val;
                if (!value.Values.TryGetValue(metricName, out val))
                {
                    val = missingValue;
                }
                total = meldValueFunc(total, val);
            }

            return total;
        }
    }
}
