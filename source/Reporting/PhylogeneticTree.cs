using System.Collections.Generic;
using System.Text;
using System.Linq;
using System;

namespace AssemblyNameSpace
{
    /// <summary>
    /// Stuff to handle phylogenetic trees.
    /// </summary>
    public class PhylogeneticTree
    {
        /// <summary>
        /// Use the Neighbour Joining algorithm to construct a phylogenetic tree from the given sequences.
        /// The runtime is O(n**3).
        /// </summary>
        /// <param name="Sequences">The sequences to join in a tree</param>
        /// <param name="alphabet">The alphabet to use</param>
        /// <returns>A tree</returns>
        public static Branch CreateTree(List<(string Name, AminoAcid[] Sequence)> Sequences, Alphabet alphabet)
        {
            var length = Sequences.Count();
            var distance = new double[length, length];

            // Get all the scores in the matrix
            for (int i = 0; i < length; i++)
                for (int j = 0; j < length; j++)
                    distance[i, j] = HelperFunctionality.SmithWaterman(Sequences[i].Sequence, Sequences[j].Sequence, alphabet).Score;

            // Find the max value
            var max = double.MinValue;
            foreach (var item in distance) if (item > max) max = item;

            // Invert all score to be valid as distances between the sequences
            for (int i = 0; i < length; i++)
                for (int j = 0; j < length; j++)
                    distance[i, j] = i == j ? 0 : max - distance[i, j];

            // Set up the branch list, start off with only leaf nodes, one for each sequence
            var leaves = new Branch[length];
            for (int i = 0; i < length; i++)
                leaves[i] = new Branch(i, Sequences[i].Name);

            // Repeat until only two branches remain
            while (length > 2)
            {
                // Calculate the Q matrix 
                var q = new double[length, length];
                var rows = new double[length];
                for (int i = 0; i < length; i++)
                    for (int j = 0; j < length; j++)
                        rows[i] += distance[i, j];

                for (int i = 0; i < length; i++)
                    for (int j = 0; j < length; j++)
                        q[i, j] = (length - 2) * distance[i, j] - rows[i] - rows[j];

                // Get the pair of branches to join in this cycle
                var min = (double.MaxValue, 0, 0);
                for (int i = 0; i < length; i++)
                    for (int j = 0; j < length; j++)
                        if (i != j && q[i, j] < min.Item1) min = (q[i, j], i, j);

                // Create the new branch
                var d = distance[min.Item2, min.Item3] / 2 + (rows[min.Item2] - rows[min.Item3]) / (2 * (length - 2));
                var node = new Branch((d, leaves[min.Item2]), (distance[min.Item2, min.Item3] - d, leaves[min.Item3]));

                // Create the distance and leave matrices for the next cycle by removing the joined branches and adding the new branch it their place
                length -= 1;
                var new_distance = new double[length, length];
                var new_leaves = new Branch[length];
                int skiprow = 0;
                for (int i = 0; i < length + 1; i++)
                {
                    if (i == min.Item3) { skiprow = 1; continue; }
                    else if (i == min.Item2) new_leaves[i - skiprow] = node;
                    else new_leaves[i - skiprow] = leaves[i];

                    int skipcolumn = 0;
                    for (int j = 0; j < length + 1; j++)
                    {
                        if (j == min.Item3)
                            skipcolumn = 1;
                        else if (i == min.Item2)
                            new_distance[i - skiprow, j - skipcolumn] = (distance[min.Item2, j] + distance[min.Item3, j] + distance[min.Item2, min.Item3]) / 2;
                        else if (j == min.Item2)
                            new_distance[i - skiprow, j - skipcolumn] = (distance[min.Item2, i] + distance[min.Item3, i] + distance[min.Item2, min.Item3]) / 2;
                        else
                            new_distance[i - skiprow, j - skipcolumn] = distance[i, j];
                    }
                }
                distance = new_distance;
                leaves = new_leaves;
            }

            // Join the last two branches
            return new Branch((distance[0, 1] / 2, leaves[0]), (distance[0, 1] / 2, leaves[1]));
        }

        public class Branch
        {
            /// <summary> The index of this leaf in the list as presented to the CreateTree function.</summary>
            public readonly int Index = 0;
            /// <summary>The name of this leaf. </summary>
            public readonly string Name = "";
            /// <summary>The left branch. Including the distance to this node.</summary>
            public readonly (double, Branch)? Left = null;
            /// <summary> The right branch. Including the distance to this node.</summary>
            public readonly (double, Branch)? Right = null;

            /// <summary> Create a leaf node.</summary>
            /// <param name="index">The index of this leaf in the list as presented to the CreateTree function.</param>
            /// <param name="name">The name of this leaf.</param>
            public Branch(int index, string name)
            {
                Index = index;
                Name = name;
            }

            /// <summary> Create a branching node. </summary>
            /// <param name="left">The left branch.</param>
            /// <param name="right">The right branch.</param>
            public Branch((double, Branch) left, (double, Branch) right)
            {
                Left = left;
                Right = right;
            }

            public void MidPointRoot()
            {
                // Find all the distances
                // Find the longest path
                // Place root
                // Resave tree in this new orientation
            }

            /// <summary> Render this tree. </summary>
            /// <param name="own">Any branches already present on the main stem of the tree (on this line).</param>
            /// <param name="other">Any branches already present on the side branch(es) of the tree (on secondary lines).</param>
            /// <returns>A fully rendered tree, using UTF-8 and characters from the Box drawing set.</returns>
            string Render(string own, string other)
            {
                if (Left == null && Right == null)
                {
                    // Leaf just print the info
                    return $"{own}> {Name} ({Index})";
                }
                else
                {
                    // A split at the current depth
                    var output = "";
                    output += Left.Value.Item2.Render(own + '┬', other + '│');
                    output += '\n';
                    output += Right.Value.Item2.Render(other + '└', other + ' ');
                    return output;
                }
            }

            /// <summary> Create a string representation of the tree. </summary>
            public override string ToString()
            {
                return Render("", "");
            }
        }
    }
}