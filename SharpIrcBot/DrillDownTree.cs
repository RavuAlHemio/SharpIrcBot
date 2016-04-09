using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlsoKnownAs
{
    public class DrillDownTree<TKey, TValue>
    {
        protected class Node
        {
        }

        protected class Branch : Node
        {
            public Dictionary<TKey, Node> Children { get; set; }
        }

        protected class Leaf : Node
        {
            public TValue Value { get; set; }
        }

        protected Branch Root { get; set; }

        public DrillDownTree()
        {
            Root = new Branch();
        } 

        public TValue this[ImmutableList<TKey> keyList]
        {
            get
            {
                TValue ret;
                int depth;

                if (TryRecursiveGet(Root, keyList, 0, out ret, out depth))
                {
                    return ret;
                }
                throw new IndexOutOfRangeException("item not found");
            }
            set
            {
                RecursiveSet(Root, keyList, value);
            }
        }

        protected bool TryRecursiveGet(Branch root, ImmutableList<TKey> keyList, int currentDepth, out TValue value, out int finalDepth)
        {
            value = default(TValue);
            finalDepth = currentDepth;

            if (keyList.Count == 0)
            {
                return false;
            }

            var thisKey = keyList[0];
            var keyRest = keyList.RemoveAt(0);

            if (!root.Children.ContainsKey(thisKey))
            {
                return false;
            }

            var child = root.Children[thisKey];
            if (child is Branch)
            {
                if (keyRest.Count == 0)
                {
                    // key ends here
                    return false;
                }

                return TryRecursiveGet((Branch) child, keyRest, currentDepth + 1, out value, out finalDepth);
            }
            else // if (child is Leaf)
            {
                if (keyRest.Count > 0)
                {
                    // key does not end here
                    return false;
                }

                return ((Leaf) child).Value;
            }
        }

        protected void RecursiveSet(Branch root, ImmutableList<TKey> keyList, TValue value)
        {
            
        }
    }
}
