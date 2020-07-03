using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;

namespace AssemblyNameSpace
{
    /// <summary>
    /// Contains the logic to filter all the identifiers for all input reads to enforce unique names.
    /// </summary>
    public class NameFilter
    {
        // SideNote: If the lookup in the BST at one point becomes a bottleneck please consider implementing a way of reorganising the BST (splay tree?)

        /// <summary>
        /// The Binary Search Tree containing the names of each identifier.
        /// </summary>
        BST Names;

        /// <summary>
        /// The invalid chars in a file path
        /// </summary>
        readonly HashSet<char> invalidchars;

        /// <summary>
        /// Create a new NameFilter
        /// </summary>
        public NameFilter()
        {
            invalidchars = new HashSet<char>(Path.GetInvalidFileNameChars());
            invalidchars.Add('*');
            invalidchars.Add('<');
            invalidchars.Add('>');
            invalidchars.Add('!');
            invalidchars.Add('%');
            invalidchars.Add('.');
            invalidchars.Add(' ');
        }

        /// <summary>
        /// Escapes the given identifier and returns the amount of read with exactly the same identifer
        /// that were already Escaped by this namefilter plus one. So it can be seen as the index 
        /// (1-based) of this identifier in the list of identical identifiers. The total number of 
        /// duplicates can be found by using the 'Count' member of the IdenticalIdentifiersNode (BST).
        /// </summary>
        /// <param name="identifier">The identifier to escape.</param>
        public (string EscapedIdentifier, BST IdenticalIdentifiersNode, int Index) EscapeIdentifier(string identifier)
        {
            var chars = identifier.ToCharArray();

            for (int i = 0; i < chars.Length; i++)
            {
                if (invalidchars.Contains(chars[i])) chars[i] = '_';
            }

            var name = new string(chars);

            BST bst;
            int count = 1;

            if (Names == null)
            {
                Names = new BST(name);
                bst = Names;
            }
            else
            {
                (bst, count) = Names.Append(name);
            }

            return (name, bst, count);
        }


    }

    /// <summary>
    /// Binary Search Tree a single node
    /// </summary>
    public class BST
    {
        /// <summary> The identifier of this node. </summary>
        public readonly string Name;

        /// <summary> The total number of duplicates found. </summary>
        public int Count;
        public BST Left;
        public BST Right;

        /// <summary>
        /// Creates a new BST node
        /// </summary>
        /// <param name="name"> The identifier to be the name of this node. </param>
        public BST(string name)
        {
            Name = name;
            Count = 1;
            Left = null;
            Right = null;
        }

        /// <summary>
        /// Add an extra identifier to the tree. Append if it does not exist yet. 
        /// Increment the Count if this identifier was already found.
        /// </summary>
        /// <param name="name"> The identifier to add. </param>
        public (BST IdenticalIdentifiersNode, int Index) Append(string name)
        {
            var sort = name.CompareTo(Name);

            if (sort == 0)
            {
                Count++;
                return (this, Count);
            }
            else if (sort < 0)
            {
                if (Left == null)
                {
                    Left = new BST(name);
                    return (Left, Left.Count);
                }
                else
                {
                    return Left.Append(name);
                }
            }
            else
            {
                if (Right == null)
                {
                    Right = new BST(name);
                    return (Right, Right.Count);
                }
                else
                {
                    return Right.Append(name);
                }
            }
        }
    }
}