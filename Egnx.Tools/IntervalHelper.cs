using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Egnx.Tools
{
    public static class IntervalHelper
    {
        public static bool Find<K, V>(this KeyValuePair<Interval<K>, V>[] invect, K key, int leftidx, int rightidx, ref V value)
        {

            var mididx = (leftidx + rightidx) / 2;
            var item = invect[mididx];
            var cmpresult = item.Key.Position(key);
            var result = cmpresult == 0;
            if (!result && (leftidx != rightidx))
            {
                if (cmpresult < 0)
                {
                    result = Find(invect, key, leftidx, mididx - 1, ref value);
                }
                else
                {
                    result = Find(invect, key, mididx + 1, rightidx, ref value);
                }
            }
            else if (result)
            {
                value = item.Value;
            }

            return result;
        }

        public static bool Find<K, V>(this SortedList<Interval<K>, V> inlist, K key, out V value) where V : new()
        {
            value = default(V);

            var idxlist = inlist.ToArray();
            var leftidx = 0;
            var rightidx = idxlist.Length - 1;

            return Find<K, V>(idxlist, key, leftidx, rightidx, ref value);
        }
    }
}
