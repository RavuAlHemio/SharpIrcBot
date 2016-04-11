using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace SharpIrcBot.Collections
{
    public class DrillDownTree<TKey, TValue>
    {
        protected abstract class Node
        {
        }

        protected sealed class Branch : Node
        {
            public Dictionary<TKey, Node> Children { get; set; }
            public Branch() { Children = new Dictionary<TKey, Node>(); }
        }

        protected sealed class Leaf : Node
        {
            public TValue Value { get; set; }
            public Leaf(TValue value) { Value = value; }
        }

        protected enum ItemMatch
        {
			NoneFound = 0,
            FullMatch = 1,
            PrefixMatch = 2
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
                ImmutableList<TValue> ret;
                int depth;

                if (TryRecursiveGet(Root, keyList, 0, out ret, out depth) == ItemMatch.FullMatch)
                {
                    return ret.First();
                }
                throw new IndexOutOfRangeException("item not found");
            }
            set
            {
                RecursiveSet(Root, keyList, value);
            }
        }

        public int GetBestMatches(ImmutableList<TKey> keyList, out ImmutableList<TValue> matches)
        {
            int finalDepth;

            ItemMatch match = TryRecursiveGet(Root, keyList, 0, out matches, out finalDepth);
            switch (match)
            {
                case ItemMatch.NoneFound:
                    matches = ImmutableList<TValue>.Empty;
                    return -1;
                default:
                    return finalDepth;
            }
        }

        public bool ContainsKey(ImmutableList<TKey> keyList)
        {
            ImmutableList<TValue> matches;
            int finalDepth;
            return (TryRecursiveGet(Root, keyList, 0, out matches, out finalDepth) == ItemMatch.FullMatch);
        }

        protected static ItemMatch TryRecursiveGet(Branch root, ImmutableList<TKey> keyList, int currentDepth, out ImmutableList<TValue> values, out int finalDepth)
        {
            finalDepth = currentDepth;

            Debug($"root: {root.GetType()}, keyList: [{string.Join(", ", keyList)}], currentDepth: {currentDepth}");

            if (keyList.Count == 0)
            {
                Debug("keyList is empty");

                values = ImmutableList<TValue>.Empty;
                return ItemMatch.NoneFound;
            }

            var thisKey = keyList[0];
            var keyRest = keyList.RemoveAt(0);

            if (!root.Children.ContainsKey(thisKey))
            {
                Debug("collecting children");

                // collect the children and return them
                values = CollectChildren(root);
                return ItemMatch.PrefixMatch;
            }

            ++currentDepth;
            finalDepth = currentDepth;

            var child = root.Children[thisKey];
            if (child is Branch)
            {
                if (keyRest.Count == 0)
                {
                    Debug("child is branch but key ended");

                    // key ends here
                    values = ImmutableList<TValue>.Empty;
                    return ItemMatch.NoneFound;
                }

                Debug("child is branch, descending");
                return TryRecursiveGet((Branch) child, keyRest, currentDepth, out values, out finalDepth);
            }
            else // if (child is Leaf)
            {
                if (keyRest.Count > 0)
                {
                    Debug("child is leaf but key continues");

                    // key does not end here
                    values = ImmutableList<TValue>.Empty;
                    return ItemMatch.NoneFound;
                }

                Debug("child is leaf, returning");
                values = ImmutableList.Create(((Leaf) child).Value);
                return ItemMatch.FullMatch;
            }
        }

        protected static void RecursiveSet(Branch root, ImmutableList<TKey> keyList, TValue value)
        {
            if (keyList.Count == 0)
            {
                return;
            }

            var thisKey = keyList[0];
            var keyRest = keyList.RemoveAt(0);

            if (keyRest.Count > 0)
            {
                if (!root.Children.ContainsKey(thisKey) || !(root.Children[thisKey] is Branch))
                {
                    // FIXME: exception instead of lumberjacking?
                    root.Children[thisKey] = new Branch();
                }

                var subBranch = ((Branch) root.Children[thisKey]);
                RecursiveSet(subBranch, keyRest, value);
            }
            else // if keyRest is empty
            {
                // FIXME: exception if child is branch?
                root.Children[thisKey] = new Leaf(value);
            }
        }

        protected static ImmutableList<TValue> CollectChildren(Node root)
        {
            var rootBranch = root as Branch;
            var ret = ImmutableList.CreateBuilder<TValue>();

            if (rootBranch != null)
            {
                foreach (var child in rootBranch.Children.Values)
                {
                    ret.AddRange(CollectChildren(child));
                }
            }
            else
            {
                ret.Add(((Leaf) root).Value);
            }

            return ret.ToImmutable();
        }

        private static void Debug(string message)
        {
#if DRILL_DOWN_TREE_LOGGING
			Console.Error.WriteLine(message);
#endif
        }
    }
}
