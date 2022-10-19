using System;
using System.Collections.Generic;
using System.Linq;


namespace Stitch
{
    public struct AmbiguityNode
    {
        /// <summary> The position in the consensus sequence for this ambiguity node. </summary>
        public int Position { get; private set; }

        /// <summary> All support to the next node. </summary>
        public Dictionary<(AminoAcid, AminoAcid), double> Support { get; private set; }

        /// <summary> Higher order support. </summary>
        public Dictionary<AminoAcid, (AmbiguityTreeNode Backward, AmbiguityTreeNode Forward)> SupportTrees;

        /// <summary> All Identifiers of reads that support this ambiguous node. </summary>
        public HashSet<string> SupportingReads { get; private set; }

        /// <summary> Create a new ambiguity node on the given position. </summary>
        /// <param name="position">The consensus sequence position for this node.</param>
        public AmbiguityNode(int position)
        {
            this.Position = position;
            this.Support = new();
            this.SupportTrees = new();
            this.SupportingReads = new();
        }

        /// <summary> Add new higher order support to this node. This will not overwrite any previously added support. </summary>
        /// <param name="here">The aminoacid at this location.</param>
        /// <param name="Intensity">The Intensity of the supporting read.</param>
        /// <param name="Identifier">The Identifier of the supporting read.</param>
        /// <param name="Path">The path flowing from this node.</param>
        public void UpdateHigherOrderSupportForward(AminoAcid here, double Intensity, string Identifier, AminoAcid[] Path)
        {
            if (this.SupportTrees.ContainsKey(here))
                this.SupportTrees[here].Forward.AddPath(Path, Intensity);
            else
            {
                var back = new AmbiguityTreeNode(here);
                var fore = new AmbiguityTreeNode(here);
                fore.AddPath(Path, Intensity);
                this.SupportTrees.Add(here, (back, fore));
            }
            this.SupportingReads.Add(Identifier);

            // Update single order support
            var key = (here, Path[0]);
            if (this.Support.ContainsKey(key))
                this.Support[key] += Intensity;
            else
                this.Support[key] = Intensity;
        }

        /// <summary> Add new higher order support to this node. This will not overwrite any previously added support. </summary>
        /// <param name="here">The aminoacid at this location.</param>
        /// <param name="Intensity">The Intensity of the supporting read.</param>
        /// <param name="Identifier">The Identifier of the supporting read.</param>
        /// <param name="Path">The path flowing from this node.</param>
        public void UpdateHigherOrderSupportBackward(AminoAcid here, double Intensity, string Identifier, AminoAcid[] Path)
        {
            if (this.SupportTrees.ContainsKey(here))
                this.SupportTrees[here].Backward.AddPath(Path, Intensity);
            else
            {
                var back = new AmbiguityTreeNode(here);
                var fore = new AmbiguityTreeNode(here);
                back.AddPath(Path, Intensity);
                this.SupportTrees.Add(here, (back, fore));
            }
            this.SupportingReads.Add(Identifier);
        }
    }
    public struct AmbiguityTreeNode
    {
        /// <summary> The Amino Acid variant of this node. </summary>
        public AminoAcid Variant;
        /// <summary> All connections from this node. </summary>
        public List<(double Intensity, AmbiguityTreeNode Next)> Connections;

        public AmbiguityTreeNode(AminoAcid variant)
        {
            this.Variant = variant;
            this.Connections = new();
        }

        /// <summary> Add a path to this tree, while squishing the tails to create tidy graphs. </summary>
        /// <param name="Path">The path still to be added.</param>
        /// <param name="Intensity">The intensity of the path.</param>
        public void AddPath(IEnumerable<AminoAcid> Path, double Intensity, bool perfect = true)
        {
            if (Path.Count() == 0) return;
            if (Connections.Count() == 0)
            {
                var next = new AmbiguityTreeNode(Path.First());
                next.AddPath(Path.Skip(1), Intensity, perfect);
                Connections.Add((Intensity, next));
                return;
            }

            var tail = this.Tail();
            if (tail != null)
            {
                if (Path.Zip(tail).All(a => a.First == a.Second))
                {
                    this.Connections[0].Next.AddPath(Path.Skip(1), Intensity, perfect);
                    this.Connections[0] = (this.Connections[0].Intensity + Intensity, this.Connections[0].Next);
                    return;
                }
            }
            if (perfect && this.Connections.Select(a => a.Next.Variant).Contains(Path.First()))
            {
                foreach (var connection in Connections)
                {
                    if (connection.Next.Variant == Path.First())
                    {
                        connection.Next.AddPath(Path.Skip(1), Intensity, true);
                        return;
                    }

                }
            }
            else
            {
                var next = new AmbiguityTreeNode(Path.First());
                next.AddPath(Path.Skip(1), Intensity, false);
                this.Connections.Add((Intensity, next));
            }
        }

        /// <summary> Simplify the tree by joining ends with this node as the root node. The function assumes the tree is not simplified yet. </summary>
        public void Simplify()
        {
            var levels = new List<List<(AmbiguityTreeNode Parent, AmbiguityTreeNode Node)>>() { new List<(AmbiguityTreeNode Parent, AmbiguityTreeNode Node)>() { (this, this) } };
            var to_scan = new Stack<(int Level, AmbiguityTreeNode Node)>();
            to_scan.Push((0, this));

            while (to_scan.Count > 0)
            {
                var element = to_scan.Pop();
                foreach (var child in element.Node.Connections)
                {
                    while (levels.Count() < element.Level + 2)
                    {
                        levels.Add(new List<(AmbiguityTreeNode Parent, AmbiguityTreeNode Node)>());
                    }
                    to_scan.Push((element.Level + 1, child.Next));
                    levels[element.Level + 1].Add((element.Node, child.Next));
                }
            }

            // Start from the end, go over all levels one by one and try to join the ends of paths.
            // Paths can be joined if the variant they are pointing at is the same, and either they
            // end at that position or they continue in a straight and equal tail.
            levels.Reverse();
            foreach (var level in levels.SkipLast(1))
            {
                foreach (var variant_group in level.GroupBy(l => l.Node.Variant))
                {
                    if (variant_group.Count() == 1) continue;
                    var seen_before = new List<(AmbiguityTreeNode Node, AminoAcid[] Tail)>();
                    var sorted_variant_group = variant_group.Select(n => (n.Parent, n.Node, n.Node.ReverseTail())).Select(i => (i.Parent, i.Node, i.Item3 == null ? 0 : i.Item3.Count())).ToList();
                    sorted_variant_group.Sort((a, b) => b.Item3.CompareTo(a.Item3));

                    // See if some of these can be joined, with the longest tail first to prevent throwing away tail information.
                    foreach (var item in sorted_variant_group)
                    {
                        var tail_list = item.Node.Tail();
                        if (tail_list == null) continue;
                        var tail = tail_list.ToArray();
                        var placed = false;

                        foreach (var other in seen_before)
                        {
                            // Checks if the full overlap of the tail is equal (ignoring any overhang)
                            if (tail.Zip(other.Tail).All(a => a.First == a.Second))
                            {
                                // Combine the intensity for the remaining children if applicable.
                                if (item.Node.Connections.Count() == 1 && other.Node.Connections.Count() == 1)
                                {
                                    other.Node.Connections[0] = (other.Node.Connections[0].Intensity + item.Node.Connections[0].Intensity, other.Node.Connections[0].Next);
                                }

                                // Set the correct connection for the parent
                                var index = item.Parent.Connections.FindIndex(n => n.Next.Variant == variant_group.Key);
                                item.Parent.Connections[index] = (item.Parent.Connections[index].Intensity, other.Node);
                                placed = true;

                                break;
                            }
                        }
                        if (!placed)
                        {
                            seen_before.Add((item.Node, tail));
                        }
                    }
                }
            }
        }

        /// <summary> The tail is the longest single connection path from this node. If any of the nodes 
        /// in the path has multiple connections the tail is null. </summary>
        /// <returns> null or the tail in the order. </returns>
        private List<AminoAcid> Tail()
        {
            var tail = this.ReverseTail();
            if (tail != null) tail.Reverse();
            return tail;
        }

        /// <returns> null or the tail in the reverse order.</returns>
        private List<AminoAcid> ReverseTail()
        {
            if (this.Connections.Count() > 1) return null;
            else if (this.Connections.Count() == 0) return new List<AminoAcid> { this.Variant };
            else
            {
                var tail = this.Connections[0].Next.ReverseTail();
                if (tail != null)
                {
                    tail.Add(this.Variant);
                    return tail;
                }
                else
                {
                    return null;
                }
            }

        }

        /// <summary> A method to ease testing of a whole network. </summary>
        /// <returns>An array with the number of arrows on each level joined with '-'.</returns>
        public string Topology()
        {
            var levels = new List<int>() { 0 };
            var to_scan = new Stack<(int Level, AmbiguityTreeNode Node)>();
            var already_scanned = new HashSet<(AmbiguityTreeNode, AmbiguityTreeNode)>();
            to_scan.Push((0, this));
            while (to_scan.Count > 0)
            {
                var element = to_scan.Pop();
                foreach (var child in element.Node.Connections)
                {
                    if (already_scanned.Contains((element.Node, child.Next))) continue;
                    already_scanned.Add((element.Node, child.Next));
                    while (levels.Count() < element.Level + 2)
                    {
                        levels.Add(0);
                    }
                    to_scan.Push((element.Level + 1, child.Next));
                    levels[element.Level + 1] += 1;
                }
            }
            return string.Join('-', levels.Skip(1));
        }

        /// <summary> The total intensity of all connections in the whole DAG. </summary>
        public double TotalIntensity()
        {
            double total = 0.0;
            var to_scan = new Stack<(int Level, AmbiguityTreeNode Node)>();
            var already_scanned = new HashSet<(AmbiguityTreeNode, AmbiguityTreeNode)>();
            to_scan.Push((0, this));
            while (to_scan.Count > 0)
            {
                var element = to_scan.Pop();
                foreach (var child in element.Node.Connections)
                {
                    if (already_scanned.Contains((element.Node, child.Next))) continue;
                    already_scanned.Add((element.Node, child.Next));
                    to_scan.Push((element.Level + 1, child.Next));
                    total += child.Intensity;
                }
            }
            return total;
        }
    }
}