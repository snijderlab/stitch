using System.Collections.Generic;
using System.Text;
using System.Linq;
using System;

namespace AssemblyNameSpace
{
    /// <summary> Functions to handle and create phylogenetic trees. </summary>
    public class PhylogeneticTree
    {
        /// <summary>
        /// Use the Neighbour Joining algorithm to construct a phylogenetic tree from the given sequences.
        /// The runtime is O(n^3).
        /// </summary>
        /// <param name="Sequences"> The sequences to join in a tree. </param>
        /// <param name="alphabet"> The alphabet to use. </param>
        public static Tree<string> CreateTree(List<(string Name, AminoAcid[] Sequence)> Sequences, Alphabet alphabet)
        {
            var length = Sequences.Count;
            var distance = new double[length, length];

            // Get all the scores in the matrix
            distance.IndexMap((i, j) =>
            {
                var scores = HelperFunctionality.SmithWaterman(Sequences[i].Sequence, Sequences[j].Sequence, alphabet).GetDetailedScores();
                return scores.MisMatches + (scores.GapInQuery + scores.GapInTemplate) * 12;
            });

            // Find the max value
            //var max = double.MinValue;
            //foreach (var item in distance) if (item > max) max = item;

            // Invert all score to be valid as distances between the sequences
            //distance.IndexMap((i, j) => i == j ? 0 : max - distance[i, j]);

            // Set up the tree list, start off with only leaf nodes, one for each sequence
            var leaves = new Tree<string>[length];
            leaves.IndexMap(i => new Tree<string>(i, Sequences[i].Name));

            // Repeat until only two trees remain
            while (length > 2)
            {
                // Calculate the Q matrix 
                var q = new double[length, length];
                var rows = new double[length];
                for (int i = 0; i < length; i++)
                    for (int j = 0; j < length; j++)
                        rows[i] += distance[i, j];

                q.IndexMap((i, j) => (length - 2) * distance[i, j] - rows[i] - rows[j]);

                // Get the pair of trees to join in this cycle
                var min = (double.MaxValue, 0, 0);
                for (int i = 0; i < length; i++)
                    for (int j = 0; j < length; j++)
                        if (i != j && q[i, j] < min.Item1) min = (q[i, j], Math.Min(i, j), Math.Max(i, j));

                // Create the new tree
                var d = distance[min.Item2, min.Item3] / 2 + (rows[min.Item2] - rows[min.Item3]) / (2 * (length - 2));
                var node = new Tree<string>((d, leaves[min.Item2]), (distance[min.Item2, min.Item3] - d, leaves[min.Item3]));

                // Create the distance and leave matrices for the next cycle by removing the joined trees and adding the new tree it their place
                length -= 1;
                var new_distance = new double[length, length];
                var new_leaves = new Tree<string>[length];
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

            // Join the last two trees
            var output = new Tree<string>((distance[0, 1] / 2, leaves[0]), (distance[0, 1] / 2, leaves[1]));
            output.RemoveNegativeDistances();
            return output;
        }

        public class ProteinHierarchyTree
        {
            public readonly Tree<string> OriginalTree;
            public readonly Tree<(int Score, int UniqueScore, int Matches, int UniqueMatches, double Area, double UniqueArea)> DataTree;

            public ProteinHierarchyTree(Tree<string> tree, List<SequenceMatch> matches)
            {
                OriginalTree = tree;
                var SetTree = tree.Remodel(branch => // Slightly inefficient as it recreates all sets from scratch every time, but I do not think that it takes much time
                    branch.Fold(
                        (left, right) => left.Union(right).ToList(),
                        (index, _) => new List<int> { index }));

                List<(string Key, HashSet<int> Set, bool Unique, int Score, int Matches, double Area)> MatchSets = matches.GroupBy(match => match.MetaData.Identifier).Select(group => (group.Key, group.Select(match => match.TemplateIndex).ToHashSet(), group.First().Unique, group.First().Score, group.First().TotalMatches, group.First().MetaData.TotalArea)).ToList();

                // Now remodel the tree again, into a version that contains the matching data that is needed.
                // Take the MatchSets and on each branch in the SetTree if any set is fully contained (all 
                // elements in the tree set are in the MatchSet) remove the matched indices. Now add whatever 
                // data is required to the node. 

                DataTree = SetTree.Remodel(branch =>
                {
                    var score = 0;
                    var unique_score = 0;
                    var matches = 0;
                    var unique_matches = 0;
                    var area = 0.0;
                    var unique_area = 0.0;
                    for (int i = 0; i < MatchSets.Count; i++)
                    {
                        var set = MatchSets[i].Set;
                        if (set.IsSupersetOf(branch.Value))
                        {
                            if (MatchSets[i].Unique)
                            {
                                unique_score += MatchSets[i].Score;
                                unique_matches += MatchSets[i].Matches;
                                unique_area += MatchSets[i].Area;
                            }
                            else
                            {
                                score += MatchSets[i].Score;
                                matches += MatchSets[i].Matches;
                                area += MatchSets[i].Area;
                            }

                            branch.Value.ForEach(i => set.Remove(i));
                            if (set.Count == 0)
                            {
                                MatchSets.RemoveAt(i);
                                i--;
                            }
                        }
                    }
                    return (score, unique_score, matches, unique_matches, area, unique_area);
                }
                );
            }

        }

        public class Tree<TValue>
        {
            /// <summary> The index of this leaf in the list as presented to the CreateTree function.</summary>
            public readonly int Index = 0;
            /// <summary> The name of this leaf. </summary>
            public readonly TValue Value;
            /// <summary> The left tree. Including the distance to this node. </summary>
            public (double, Tree<TValue>)? Left { get; private set; }
            /// <summary> The right tree. Including the distance to this node. </summary>
            public (double, Tree<TValue>)? Right { get; private set; }

            /// <summary> Create a leaf node.</summary>
            /// <param name="index"> The index of this leaf in the list as presented to the CreateTree function. </param>
            /// <param name="value"> The value of this leaf. </param>
            public Tree(int index, TValue value)
            {
                Index = index;
                Value = value;
            }

            /// <summary> Create a treeing node. </summary>
            /// <param name="left"> The left tree. Consisting of the distance and node for this tree. </param>
            /// <param name="right"> The right tree. Consisting of the distance and node for this tree. </param>
            public Tree((double, Tree<TValue>) left, (double, Tree<TValue>) right)
            {
                Left = left;
                Right = right;
            }

            private Tree(int index, TValue value, (double, Tree<TValue>)? left, (double, Tree<TValue>)? right)
            {
                Index = index;
                Value = value;
                Left = left;
                Right = right;
            }

            /// <summary> Render this tree. </summary>
            /// <param name="own"> Any trees already present on the main stem of the tree (on this line). </param>
            /// <param name="other"> Any trees already present on the side tree(es) of the tree (on secondary lines). </param>
            /// <returns> A fully rendered tree, using UTF-8 and characters from the Box drawing set. </returns>
            string Render(string own, string other, bool showValue, bool showLength)
            {
                if (Left == null && Right == null)
                {
                    // Leaf just print the info
                    return $"{own}> {Value} ({Index})";
                }
                else
                {
                    // A split at the current depth
                    var value = showValue ? Value.ToString() : "";
                    var spacing = new string(' ', value.Length);

                    var lengthLeft = showLength ? Left.Value.Item1.ToString("G3") + '─' : "";
                    var lengthRight = showLength ? Right.Value.Item1.ToString("G3") + '─' : "";
                    var length = Math.Max(lengthLeft.Length, lengthRight.Length);
                    lengthLeft = lengthLeft.PadRight(length, '─');
                    lengthRight = lengthRight.PadRight(length, '─');
                    var lengthSpacing = new string(' ', length);

                    return Left.Value.Item2.Render(own + value + '┬' + lengthLeft, other + spacing + '│' + lengthSpacing, showValue, showLength) + '\n' +
                           Right.Value.Item2.Render(other + spacing + '└' + lengthRight, other + spacing + ' ' + lengthSpacing, showValue, showLength);
                }
            }

            /// <summary> Create a string representation of the tree. </summary>
            public override string ToString()
            {
                return Render("", "", false, false);
            }

            public string ToString(bool showValue, bool showLength)
            {
                return Render("", "", showValue, showLength);
            }

            /// <summary> Fold a function over the tree by applying it to every tree in a depth first way. </summary>
            /// <param name="seed"> The initial value for the accumulator structure. </param>
            /// <param name="f"> The function to apply to every node. </param>
            /// <typeparam name="TAcc"> The type of the accumulator structure. </typeparam>
            /// <returns> The accumulator. </returns>
            public TAcc Fold<TAcc>(TAcc seed, Func<TAcc, TValue, TAcc> f)
            {
                var output = f(seed, this.Value);
                if (Left != null) output = Left.Value.Item2.Fold(output, f);
                if (Right != null) output = Right.Value.Item2.Fold(output, f);
                return output;
            }

            /// <summary> Fold two functions over the tree by applying them to every tree and leaf in a depth first way. </summary>
            /// <param name="tree"> The function to apply to every branch (a node which has a Left and/or Right node). </param>
            /// <param name="leaf"> The function to apply to every leaf (a node which has no Left or Right node). </param>
            /// <typeparam name="TAcc"> The type of the accumulator structure. </typeparam>
            /// <returns> The accumulator. </returns>
            public TAcc Fold<TAcc>(Func<TAcc, TAcc, TAcc> tree, Func<int, TValue, TAcc> leaf)
            {
                if (Left == null && Right == null)
                    return leaf(this.Index, this.Value);
                else
                    return tree(Left.Value.Item2.Fold(tree, leaf), Right.Value.Item2.Fold(tree, leaf));
            }

            /// <summary> Apply a function to every branch in the tree. </summary>
            /// <param name="f"> The function to apply. </param>
            public void Apply(Action<Tree<TValue>> f)
            {
                f(this);
                if (Left != null) Left.Value.Item2.Apply(f);
                if (Right != null) Right.Value.Item2.Apply(f);
            }

            /// <summary> Apply a function to every branch in the tree. </summary>
            /// <param name="f"> The function to apply. </param>
            public void Apply(Action<Tree<TValue>> tree, Action<TValue> leaf)
            {
                if (Left == null && Right == null)
                {
                    leaf(this.Value);
                }
                else
                {
                    tree(this);
                    if (Left != null) Left.Value.Item2.Apply(tree, leaf);
                    if (Right != null) Right.Value.Item2.Apply(tree, leaf);
                }
            }


            public Tree<TOut> Remodel<TOut>(Func<Tree<TValue>, TOut> f)
            {
                return new Tree<TOut>(
                    this.Index,
                    f(this),
                    Left == null ? null : (Left.Value.Item1, Left.Value.Item2.Remodel(f)),
                    Right == null ? null : (Right.Value.Item1, Right.Value.Item2.Remodel(f)));
            }

            public void RemoveNegativeDistances()
            {
                static (double, Tree<TValue>) CheckSide((double, Tree<TValue>) node)
                {
                    var node_branch = node.Item2;
                    if (node_branch.Left != null && node_branch.Right != null)
                    {
                        if (node_branch.Left.Value.Item1 < 0)
                        {
                            node_branch.Right = (node_branch.Right.Value.Item1 - node_branch.Left.Value.Item1, node_branch.Right.Value.Item2);
                            node_branch.Left = (0, node_branch.Left.Value.Item2);
                            return (node.Item1 - node_branch.Left.Value.Item1, node_branch);
                        }
                        if (node_branch.Right.Value.Item1 < 0)
                        {
                            node_branch.Left = (node_branch.Left.Value.Item1 - node_branch.Right.Value.Item1, node_branch.Left.Value.Item2);
                            node_branch.Right = (0, node_branch.Right.Value.Item2);
                            return (node.Item1 - node_branch.Right.Value.Item1, node_branch);
                        }
                    }
                    return node;
                }
                if (Left != null)
                {
                    Left = CheckSide(Left.Value);
                    Left.Value.Item2.RemoveNegativeDistances();
                }
                if (Right != null)
                {
                    Right = CheckSide(Right.Value);
                    Right.Value.Item2.RemoveNegativeDistances();
                }
            }
        }
    }
}