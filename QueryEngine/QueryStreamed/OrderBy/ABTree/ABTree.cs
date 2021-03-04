using System;
using System.Collections;
using System.Collections.Generic;

namespace QueryEngine
{
    /// <summary>
    /// (A, B) Tree implementation that uses only insert and iteration.
    /// A >= 2, B = 2*A
    /// The keys on the nodes store the values directly.
    /// The used nodes contain parent pointers and indeces of their position in the parent's children array.
    /// This enables insertion without the need to remember the path, as well as the unumeration.
    /// It does not store duplicate values.
    /// 
    /// The implementation is based on the lecture on Data Structures 1 (NTIN066) taught in the 2020/2021 winter semester, at Charles University
    /// Mathematical and physical faculty.
    /// </summary>
    /// <typeparam name="T"> The type of keys stored in the nodes.</typeparam>
    internal class ABTree<T> : IEnumerable<T>
    {
        public int Count { get; private set; } = 0;

        private IComparer<T> comparer;
        private ABTreeNode<T> root = null;
        private int maxChildren;
        private int minChildren;

        public ABTree(int B, IComparer<T> comparer)
        {
            if (B % 2 == 1)
                throw new ArgumentException($"{this.GetType()}, passed non even B value as the max children count.");
            else if (comparer == null)
                throw new ArgumentException($"{this.GetType()}, passed comparer as null.");
            else if (B < 4)
                throw new ArgumentException($"{this.GetType()}, passed 4 > B value. Choose a higher B value.");

            this.maxChildren = B;
            this.minChildren = B / 2;
            this.comparer = comparer;
        }

        private void InitNewRoot(T key)
        {
            root = new ABTreeNode<T>(this.maxChildren);
            root.keys.Add(key);
            this.Count++;
        }

        /// <summary>
        /// Inserts key into the tree.
        /// If it already contains the key, the key is not inserted.
        /// </summary>
        /// <param name="key"> A key to insert into the tree.</param>
        public void Insert(T key)
        {
            ABTreeNode<T> node = null;
            if (root == null) InitNewRoot(key);
            // The key was found => no insertion.
            else if ((node = FindLeafNode(key, out int pos)) == null) return;
            else
            {
                node.keys.Insert(pos, key);
                this.Count++;
                SplitRoutine(node);
            }
        }

        /// <summary>
        /// Splits until it finds a node with enough space.
        /// Variable "node" is always the left node during spliting.
        /// </summary>
        /// <param name="node"></param>
        private void SplitRoutine(ABTreeNode<T> node)
        {
            // keys.count > B-1
            while (node.keys.Count >= this.maxChildren)
            {
                ABTreeNode<T> rightNode = new ABTreeNode<T>(this.maxChildren);
                int middleIndex = node.keys.Count / 2;
                T middleKey = node.keys[middleIndex];
                int rightStartIndex = middleIndex + 1;

                // Move children from rightStart of the node to the rightNode.
                node.MoveKeys(rightNode, rightStartIndex);
                // Remove the middle key, which must be at the end when the above finishes.
                node.keys.RemoveAt(node.keys.Count - 1);

                // If the node was internal node, move it's children as well.
                if (node.children != null)
                {
                    rightNode.InitChildren();
                    node.MoveChildren(rightNode, rightStartIndex);
                }

                if (node == root)
                {   // Create new root.
                    root = new ABTreeNode<T>(this.maxChildren);
                    root.InitChildren();
                    // Left
                    root.children.Add(node);
                    node.parent = root;
                    node.index = 0;
                    // Right
                    root.children.Add(rightNode);
                    rightNode.parent = root;
                    rightNode.index = 1;
                    // Key
                    root.keys.Add(middleKey);
                    return;
                }
                else
                {   // Insert the middle value into the parent.
                    node.parent.InsertBranch(node.index, middleKey, rightNode);
                    node = node.parent;
                }
            }
        }

        /// <summary>
        /// Starts at the root.
        /// If the key was found, return null.
        /// If the key was not found, go down if the next subtree is not null.
        /// If the next subtree is null, we reached the leaf node.
        /// </summary>
        /// <returns> Null if the key was found otherwise leaf node where the key should be inserted. </returns>
        private ABTreeNode<T> FindLeafNode(T key, out int pos)
        {
            ABTreeNode<T> node = root;
            while (true)
            {
                bool found = node.FindBranch(key, out pos, this.comparer);

                // Key was found, thus no insertion.
                if (found) return null;
                // There is another subtree.
                else if (!node.IsLeaf()) node = node.children[pos];
                // It reached the Leaf level.
                else return node;
            }
        }

        /// <summary>
        /// Prints keys in PreOrderTraversal.
        /// Recursive.
        /// </summary>
        public void Print()
        {
            PrintInternal(this.root);
        }

        private void PrintInternal(ABTreeNode<T> node)
        {
            if (node.children == null)
            {
                // Leaf node prints only keys.
                for (int i = 0; i < node.keys.Count; i++)
                    Console.WriteLine(node.keys[i]);
            }
            else
            {
                // Print each subtree and keys in between.
                for (int i = 0; i <= node.keys.Count; i++)
                {
                    PrintInternal(node.children[i]);
                    if (i != node.keys.Count) Console.WriteLine(node.keys[i]);
                }
            }
        }

        /// <summary>
        /// PreOrder traversal.
        /// Iterative iteration over the keys.
        /// No stack is used.
        /// The iteration uses parent pointers and children indeces in their parents children arrays.
        /// </summary>
        /// <returns> Keys of the tree in ascending order. </returns>
        public IEnumerator<T> GetEnumerator()
        {
            ABTreeNode<T> node = root;
            int branchIndex = -1;
            if (node != null)
            {
                while (true)
                {
                    // Leaf.
                    if (node.children == null)
                    {
                        // Return all keys.
                        for (int i = 0; i < node.keys.Count; i++)
                            yield return node.keys[i];

                        if (node.parent == null) break;
                        else {
                            // Return to the parent.
                            branchIndex = node.index;
                            node = node.parent;
                        }
                    }
                    else
                    // Internal Node -> must have children.
                    {
                        // If this is the first time in this node.
                        // Go to the left most subtree.
                        if (branchIndex == -1) node = node.children[0];
                        // If you returned from the right most subtree.
                        // Return to the parent node.
                        else if (branchIndex >= node.keys.Count)
                        {
                            if (node.parent == null) break;
                            else
                            {
                                branchIndex = node.index;
                                node = node.parent;
                            }
                        }
                        else
                        // It returned from the subtree and there are more subtrees in the node.
                        {
                            yield return node.keys[branchIndex];
                            // Go to the next subtree
                            node = node.children[branchIndex + 1];
                            branchIndex = -1;
                        }
                    }
                }
            } 
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        /// <summary>
        /// A node of an (A, B) tree.
        /// Uses parent pointers and index of it's position in the parent's children array.
        /// The childre array should be  inited only if the children are being inserted.
        /// </summary>
        /// <typeparam name="TT"> The type of keys stored in the node. </typeparam>
        private class ABTreeNode<TT>
        {
            /// <summary>
            /// Index of this node in the parent's children array.
            /// </summary>
            public int index;
            public List<TT> keys;
            /// <summary>
            /// Empty if the node is leaf.
            /// </summary>
            public List<ABTreeNode<TT>> children;
            public ABTreeNode<TT> parent;

            /// <summary>
            /// Plus one on B to be able to insert value before spliting the node.
            /// </summary>
            public ABTreeNode(int B)
            {
                //this.children = new List<ABTreeNode<TT>>(B + 1);
                this.keys = new List<TT>(B);
            }

            /// <summary>
            /// Finds a branch based on where the provided key belongs.
            /// The search is done as a binary search.
            /// </summary>
            /// <returns> 
            /// True on find, otherwise false. Returned position is either the key's position or the first index of a key
            /// that is larger than the searched key. If the key is the largest of the keys, the returned position is count of keys.
            /// </returns>
            public bool FindBranch(TT key, out int pos, IComparer<TT> comparer)
            {
                int branchIndex = this.keys.BinarySearch(key, comparer);
                if (branchIndex < 0)
                {
                    pos = ~branchIndex;
                    return false;
                }
                else
                {
                    pos = branchIndex;
                    return true;
                }
            }

            /// <summary>
            /// Expects that this function is called only when adding new child to a parent after split occurs.
            /// Otherwise, it will throw null ptr exception.
            /// </summary>
            public void InsertBranch(int i, TT value, ABTreeNode<TT> child)
            {
                child.parent = this;
                this.keys.Insert(i, value);

                this.children.Insert(i + 1, child);
                // Reset the values of indeces inside.
                for (int j = i + 1; j < this.children.Count; j++)
                    this.children[j].index = j;
            }

            /// <summary>
            /// Move keys to the empty right node when spliting.
            /// </summary>
            public void MoveKeys(ABTreeNode<TT> right, int startIndex)
            {
                for (int i = startIndex; i < this.keys.Count; i++)
                    right.keys.Add(this.keys[i]);
                this.keys.RemoveRange(startIndex, this.keys.Count - startIndex);
            }

            /// <summary>
            /// Move children to the empty right node when spliting.
            /// When moving children, it is important not to forget
            /// seting their new parent and theri new indeces as they occur in the parent's children array.
            /// </summary>
            public void MoveChildren(ABTreeNode<TT> right, int startIndex)
            {
                ABTreeNode<TT> child = null;
                int j = 0;
                for (int i = startIndex; i < this.children.Count; i++)
                {
                    child = this.children[i];
                    child.parent = right;
                    child.index = j;
                    right.children.Add(child);
                    j++;
                }
                this.children.RemoveRange(startIndex, this.children.Count - startIndex);
            }

            public bool IsLeaf()
            {
                return children == null;
            }

            public void InitChildren()
            {
                this.children = new List<ABTreeNode<TT>>(this.keys.Capacity + 1);
            }
        }
    }

}
