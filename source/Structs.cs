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
    /// <summary> A struct to function as a wrapper for AminoAcid information, so custom alphabets can 
    /// be used in an efficient way </summary>
    public struct AminoAcid
    {
        /// <summary> The code (index of the char in the alpabet array of the parent).
        /// The only way to change it is in the creator. </summary>
        /// <value> The code of this AminoAcid. </value>
        public int Code;
        /// <summary>
        /// The alphabet used
        /// </summary>
        public Alphabet alphabet;

        /// <summary> The creator of AminoAcids. </summary>
        /// <param name="alphabet_input"> The alphabet used. </param>
        /// <param name="input"> The character to store in this AminoAcid. </param>
        public AminoAcid(Alphabet alphabet_input, char input)
        {
            alphabet = alphabet_input;
            Code = alphabet.getIndexInAlphabet(input);
        }
        /// <summary> Will create a string of this AminoAcid. Consiting of the character used to 
        /// create this AminoAcid. </summary>
        /// <returns> Returns the character of this AminoAcid (based on the alphabet) as a string. </returns>
        public override string ToString()
        {
            return alphabet.alphabet[Code].ToString();
        }
        /// <summary> Will create a string of an array of AminoAcids. </summary>
        /// <param name="array"> The array to create a string from. </param>
        /// <returns> Returns the string of the array. </returns>
        public static string ArrayToString(AminoAcid[] array)
        {
            var builder = new StringBuilder();
            foreach (AminoAcid aa in array)
            {
                builder.Append(aa.ToString());
            }
            return builder.ToString();
        }
        /// <summary> To check for equality of the AminoAcids. Will return false if the object is not an AminoAcid. </summary>
        /// <remarks> Implemented as the equals operator (==). </remarks>
        /// <param name="obj"> The object to check equality with. </param>
        /// <returns> Returns true when the Amino Acids are equal. </returns>
        public override bool Equals(object obj)
        {
            return obj is AminoAcid && this == (AminoAcid)obj;
        }
        /// <summary> To check for equality of arrays of AminoAcids. </summary>
        /// <remarks> Implemented as a short circuiting loop with the equals operator (==). </remarks>
        /// <param name="left"> The first object to check equality with. </param>
        /// <param name="right"> The second object to check equality with. </param>
        /// <returns> Returns true when the aminoacid arrays are equal. </returns>
        public static bool ArrayEquals(AminoAcid[] left, AminoAcid[] right)
        {
            if (left.Length != right.Length)
                return false;
            for (int i = 0; i < left.Length; i++)
            {
                if (left[i] != right[i])
                {
                    return false;
                }
            }
            return true;
        }
        /// <summary> To check for equality of AminoAcids. </summary>
        /// <remarks> Implemented as a check for equality of the Code of both AminoAcids. </remarks>
        /// <param name="x"> The first AminoAcid to test. </param>
        /// <param name="y"> The second AminoAcid to test. </param>
        /// <returns> Returns true when the Amino Acids are equal. </returns>
        public static bool operator ==(AminoAcid x, AminoAcid y)
        {
            return x.Code == y.Code;
        }
        /// <summary> To check for inequality of AminoAcids. </summary>
        /// <remarks> Implemented as a a reverse of the equals operator. </remarks>
        /// <param name="x"> The first AminoAcid to test. </param>
        /// <param name="y"> The second AminoAcid to test. </param>
        /// <returns> Returns false when the Amino Acids are equal. </returns>
        public static bool operator !=(AminoAcid x, AminoAcid y)
        {
            return !(x == y);
        }
        /// <summary> To get a hashcode for this AminoAcid. </summary>
        /// <returns> Returns the hashcode of the AminoAcid. </returns>
        public override int GetHashCode()
        {
            return this.Code.GetHashCode();
        }
        /// <summary> Calculating homology, using the scoring matrix of the parent Assembler. 
        /// See <see cref="Alphabet.scoring_matrix"/> for the scoring matrix.
        /// See <see cref="Alphabet(string, Alphabet.AlphabetParamType)"/> on how to change the scoring matrix.</summary>
        /// <remarks> Depending on which rules are put into the scoring matrix the order in which this 
        /// function is evaluated could differ. <c>a.Homology(b)</c> does not have to be equal to 
        /// <c>b.Homology(a)</c>. </remarks>
        /// <param name="right"> The other AminoAcid to use. </param>
        /// <returns> Returns the homology score (based on the scoring matrix) of the two AminoAcids. </returns>
        public int Homology(AminoAcid right)
        {
            try
            {
                return alphabet.scoring_matrix[this.Code, right.Code];
            }
            catch (Exception e)
            {
                Console.WriteLine($"Got an error while looking up the homology for this code {this.Code} and that code {right.Code}, probably there is one (or more) character that is not valid");
                throw e;
            }
        }
        /// <summary> Calculating homology between two arrays of AminoAcids, using the scoring matrix 
        /// of the parent Assembler. </summary>
        /// <remarks> Two arrays of different length will result in a value of 0. This function loops
        /// over the AminoAcids and returns the sum of the homology value between those. </remarks>
        /// <param name="left"> The first object to calculate homology with. </param>
        /// <param name="right"> The second object to calculate homology with. </param>
        /// <returns> Returns the homology between the two aminoacid arrays. </returns>
        public static int ArrayHomology(AminoAcid[] left, AminoAcid[] right)
        {
            int score = 0;
            if (left.Length != right.Length)
                // Throw exception?
                return 0;
            for (int i = 0; i < left.Length; i++)
            {
                score += left[i].Homology(right[i]);
            }
            return score;
        }
    }
    /// <summary> Nodes in the graph with a sequence length of K-1. </summary>
    public class Node
    {
        /// <summary> The member to store the sequence information in. </summary>
        private AminoAcid[] sequence;

        /// <summary> The sequence of the Node. Only has a getter. </summary>
        /// <value> The sequence of this node. </value>
        public AminoAcid[] Sequence { get { return sequence; } }

        /// <summary> Where the (k-1)-mer sequence comes from. </summary>
        private List<int> origins;

        /// <summary> The indexes of the reads where this (k-1)-mere originated from. </summary>
        /// <value> A list of indexes of the list of reads. </value>
        public List<int> Origins { get { return origins; } }

        /// <summary> The list of edges from this Node. The tuples contain the index 
        /// of the Node where the edge goes to, the homology with the first Node 
        /// and the homology with the second Node in this order. The private 
        /// member to store the list. </summary>
        private List<ValueTuple<int, int, int>> forwardEdges;

        /// <summary> The list of edges going from this node. </summary>
        /// <value> The list of edges from this Node. The tuples contain the index 
        /// of the Node where the edge goes to, the homology with the first Node 
        /// and the homology with the second Node in this order. Only has a getter. </value>
        public List<ValueTuple<int, int, int>> ForwardEdges { get { return forwardEdges; } }

        /// <summary> The list of edges to this Node. The tuples contain the index 
        /// of the Node where the edge goes to, the homology with the first Node 
        /// and the homology with the second Node in this order. The private 
        /// member to store the list. </summary>
        private List<ValueTuple<int, int, int>> backwardEdges;

        /// <summary> The list of edges going to this node. </summary>
        /// <value> The list of edges to this Node. The tuples contain the index 
        /// of the Node where the edge goes to, the homology with the first Node 
        /// and the homology with the second Node in this order. Only has a getter. </value>
        public List<ValueTuple<int, int, int>> BackwardEdges { get { return backwardEdges; } }

        /// <summary> Whether or not this node is visited yet. </summary>
        public bool Visited;

        /// <summary> The creator of Nodes. </summary>
        /// <param name="seq"> The sequence of this Node. </param>
        /// <param name="origin"> The origin(s) of this (k-1)-mer. </param>
        /// <remarks> It will initialize the edges list. </remarks>
        public Node(AminoAcid[] seq, List<int> origin)
        {
            sequence = seq;
            origins = origin;
            forwardEdges = new List<ValueTuple<int, int, int>>();
            backwardEdges = new List<ValueTuple<int, int, int>>();
            Visited = false;
        }

        /// <summary> To add a forward edge to the Node. Wil only be added if the score is high enough. </summary>
        /// <param name="target"> The index of the Node where this edge goes to. </param>
        /// <param name="score1"> The homology of the edge with the first Node. </param>
        /// <param name="score2"> The homology of the edge with the second Node. </param>
        public void AddForwardEdge(int target, int score1, int score2)
        {
            bool inlist = false;
            foreach (var edge in forwardEdges)
            {
                if (edge.Item1 == target)
                {
                    inlist = true;
                    break;
                }
            }
            if (!inlist) forwardEdges.Add((target, score1, score2));
            return;
        }
        /// <summary> To add a backward edge to the Node. </summary>
        /// <param name="target"> The index of the Node where this edge comes from. </param>
        /// <param name="score1"> The homology of the edge with the first Node. </param>
        /// <param name="score2"> The homology of the edge with the second Node. </param>
        public void AddBackwardEdge(int target, int score1, int score2)
        {
            bool inlist = false;
            foreach (var edge in backwardEdges)
            {
                if (edge.Item1 == target)
                {
                    inlist = true;
                    break;
                }
            }
            if (!inlist) backwardEdges.Add((target, score1, score2));
            return;
        }
        /// <summary> To check if the Node has forward edges. </summary>
        /// <returns> Returns true if the node has forward edges. </returns>
        public bool HasForwardEdges()
        {
            return forwardEdges.Count > 0;
        }
        /// <summary> To check if the Node has backward edges. </summary>
        /// <returns> Returns true if the node has backward edges. </returns>
        public bool HasBackwardEdges()
        {
            return backwardEdges.Count > 0;
        }
        /// <summary> To get the amount of edges (forward and backward). </summary>
        /// <remarks> O(1) runtime </remarks>
        /// <returns> The amount of edges (forwards and backwards). </returns>
        public int EdgesCount()
        {
            return forwardEdges.Count + backwardEdges.Count;
        }
    }
    /// <summary> A struct to hold meta information about the assembly to keep it organized 
    /// and to report back to the user. </summary>
    public struct MetaInformation
    {
        /// <summary> The total time needed to run Assemble(). See <see cref="Assembler.Assemble"/></summary>
        public long total_time;
        /// <summary> The needed to do the pre work, creating k-mers and (k-1)-mers. See <see cref="Assembler.Assemble"/></summary>
        public long pre_time;
        /// <summary> The time needed the build the graph. See <see cref="Assembler.Assemble"/></summary>
        public long graph_time;
        /// <summary> The time needed to find the path through the de Bruijn graph. See <see cref="Assembler.Assemble"/></summary>
        public long path_time;
        /// <summary> The time needed to filter the sequences. See <see cref="Assembler.Assemble"/></summary>
        public long sequence_filter_time;
        /// <summary> The time needed for template matching. </summary>
        public long template_matching_time;
        /// <summary> The time needed to draw the graphs.</summary>
        public long drawingtime;
        /// <summary> The amount of reads used by the program. See <see cref="Assembler.Assemble"/>.</summary>
        public int reads;
        /// <summary> The amount of k-mers generated. See <see cref="Assembler.Assemble"/></summary>
        public int kmers;
        /// <summary> The amount of (k-1)-mers generated. See <see cref="Assembler.Assemble"/></summary>
        public int kmin1_mers;
        /// <summary> The amount of (k-1)-mers generated, before removing all duplicates. See <see cref="Assembler.Assemble"/></summary>
        public int kmin1_mers_raw;
        /// <summary> The number of sequences found. See <see cref="Assembler.Assemble"/></summary>
        public int sequences;
    }
    /// <summary> Nodes in the condensed graph with a variable sequence length. </summary>
    public class CondensedNode
    {
        /// <summary> The index this node. The index is defined as the index of the startnode in the adjacency list of the de Bruijn graph. </summary>
        public int Index;
        /// <summary> The index of the last node (going from back to forth). To build the condensed graph with indexes in the condensed graph instead of the de Bruijn graph in the edges lists. </summary>
        public int ForwardIndex;
        /// <summary> The index of the first node (going from back to forth). To build the condensed graph with indexes in the condensed graph instead of the de Bruijn graph in the edges lists. </summary>
        public int BackwardIndex;
        /// <summary> Whether or not this node is visited yet. </summary>
        public bool Visited;
        /// <summary> The sequence of this node. It is the longest constant sequence to be 
        /// found in the de Bruijn graph starting at the Index. See <see cref="CondensedNode.Index"/></summary>
        public List<AminoAcid> Sequence;
        /// <summary>
        /// The possible prefix sequence before the sequence, trimmed off in creating the condensed graph
        /// </summary>
        public List<AminoAcid> Prefix;
        /// <summary>
        /// The possible suffix sequence after the sequence, trimmed off in creating the condensed graph
        /// </summary>
        public List<AminoAcid> Suffix;
        /// <summary> The list of forward edges, defined as the indexes in the de Bruijn graph. </summary>
        public List<int> ForwardEdges;
        /// <summary> The list of backward edges, defined as the indexes in the de Bruijn graph. </summary>
        public List<int> BackwardEdges;
        /// <summary> The origins where the (k-1)-mers used for this sequence come from. Defined as the index in the list with reads. </summary>
        public List<List<int>> Origins;
        public List<int> UniqueOrigins
        {
            get
            {
                var output = new List<int>();
                foreach (var outer in Origins)
                {
                    foreach (var inner in outer)
                    {
                        if (!output.Contains(inner)) output.Add(inner);
                    }
                }
                output.Sort();
                return output;
            }
        }
        /// <summary> Creates a condensed node to be used in the condensed graph. </summary>
        /// <param name="sequence"> The sequence of this node. See <see cref="CondensedNode.Sequence"/></param>
        /// <param name="index"> The index of the node, the index in the de Bruijn graph. See <see cref="CondensedNode.Index"/></param>
        /// <param name="forward_index"> The index of the last node of the sequence (going from back to forth). See <see cref="CondensedNode.ForwardIndex"/></param>
        /// <param name="backward_index"> The index of the first node of the sequence (going from back to forth). See <see cref="CondensedNode.BackwardIndex"/></param>
        /// <param name="forward_edges"> The forward edges from this node (indexes). See <see cref="CondensedNode.ForwardEdges"/></param>
        /// <param name="backward_edges"> The backward edges from this node (indexes). See <see cref="CondensedNode.BackwardEdges"/></param>
        /// <param name="origins"> The origins where the (k-1)-mers used for this sequence come from. See <see cref="CondensedNode.Origins"/></param>
        public CondensedNode(List<AminoAcid> sequence, int index, int forward_index, int backward_index, List<int> forward_edges, List<int> backward_edges, List<List<int>> origins)
        {
            Sequence = sequence;
            Index = index;
            ForwardIndex = forward_index;
            BackwardIndex = backward_index;
            ForwardEdges = forward_edges;
            BackwardEdges = backward_edges;
            Origins = origins;
            Visited = false;
            Suffix = new List<AminoAcid>();
            Prefix = new List<AminoAcid>();
        }
    }
    /// <summary>
    /// To contain an alphabet with scoring matrix to score pairs of amino acids
    /// </summary>
    public class Alphabet
    {
        /// <summary> The matrix used for scoring of the alignment between two characters in the alphabet. 
        /// As such this matrix is rectangular. </summary>
        public int[,] scoring_matrix;
        /// <summary> The alphabet used for alignment. The default value is all the amino acids in order of
        /// natural abundance in prokaryotes to make finding the right amino acid a little bit faster. </summary>
        public char[] alphabet;
        /// <summary> Find the index of the given character in the alphabet. </summary>
        /// <param name="c"> The character to look up. </param>
        /// <returns> The index of the character in the alphabet or -1 if it is not in the alphabet. </returns>
        public int getIndexInAlphabet(char c)
        {
            for (int i = 0; i < alphabet.Length; i++)
            {
                if (c == alphabet[i])
                {
                    return i;
                }
            }
            Console.WriteLine($"Could not find '{c}' in the alphabet: '{alphabet}'");
            return -1;
        }

        /// <summary> Set the alphabet of the assembler. </summary>
        /// <param name="rules"> A list of rules implemented as tuples containing the chars to connect, 
        /// the value to put into the matrix and whether or not the rule should be bidirectional (the value
        ///  in the matrix is the same both ways). </param>
        /// <param name="diagonals_value"> The value to place on the diagonals of the matrix. </param>
        /// <param name="input"> The alphabet to use, it will be iterated over from the front to the back so
        /// the best case scenario has the most used characters at the front of the string. </param>
        public Alphabet(List<ValueTuple<char, char, int, bool>> rules = null, int diagonals_value = 1, string input = "LSAEGVRKTPDINQFYHMCWOU")
        {
            alphabet = input.ToCharArray();

            scoring_matrix = new int[alphabet.Length, alphabet.Length];

            // Only set the diagonals to te given value
            for (int i = 0; i < alphabet.Length; i++) scoring_matrix[i, i] = diagonals_value;

            // Use the rules to 
            if (rules != null)
            {
                foreach (var rule in rules)
                {
                    scoring_matrix[getIndexInAlphabet(rule.Item1), getIndexInAlphabet(rule.Item2)] = rule.Item3;
                    if (rule.Item4) scoring_matrix[getIndexInAlphabet(rule.Item2), getIndexInAlphabet(rule.Item1)] = rule.Item3;
                }
            }
        }
        /// <summary>
        /// To indicate if the given string is data or a path to the data
        /// </summary>
        public enum AlphabetParamType
        {
            /// <summary> It is the data itself. </summary>
            Data,
            /// <summary> It is a path to a file containing the data. </summary>
            Path
        }
        /// <summary> Set the alphabet based on data in csv format. </summary>
        /// <param name="data"> The csv data. </param>
        /// <param name="type"> To indicate if the data is data or a path to data </param>
        public Alphabet(string data, AlphabetParamType type)
        {
            if (type == AlphabetParamType.Path)
            {
                try
                {
                    data = File.ReadAllText(data);
                }
                catch (Exception e)
                {
                    throw new Exception($"Could not open the alphabetfile: {data}; {e.Message}");
                }
            }
            var input = data.Split('\n');
            int rows = input.Length;
            List<string[]> array = new List<string[]>();

            foreach (string line in input)
            {
                if (line != "")
                    array.Add(line.Split(new char[] { ';', ',' }).Select(x => x.Trim(new char[] { ' ', '\n', '\r', '\t', '-', '.' })).ToArray());
            }

            int columns = array[0].Length;

            for (int line = 0; line < rows; line++)
            {
                if (rows != array[line].Length)
                {
                    throw new ParseException($"The amount of rows ({rows}) is not equal to the amount of columns ({array[line].Length}) for line {line + 1}.");
                }
            }

            alphabet = String.Join("", array[0].SubArray(1, columns - 1)).ToCharArray();
            scoring_matrix = new int[columns - 1, columns - 1];

            for (int i = 0; i < columns - 1; i++)
            {
                for (int j = 0; j < columns - 1; j++)
                {
                    try
                    {
                        scoring_matrix[i, j] = Int32.Parse(array[i + 1][j + 1]);
                    }
                    catch
                    {
                        throw new ParseException($"The reading on the alphabet file was not successfull, because at column {i} and row {j} the value ({array[i + 1][j + 1]}) is not a valid integer.");
                    }
                }
            }
        }
    }
    /// <summary> A class to store extension methods to help in the process of coding. </summary>
    static public class HelperFunctionality
    {
        /// <summary> To copy a subarray to a new array. </summary>
        /// <param name="data"> The old array to copy from. </param>
        /// <param name="index"> The index to start copying. </param>
        /// <param name="length"> The length of the created subarray. </param>
        /// <typeparam name="T"> The type of the elements in the array. </typeparam>
        /// <returns> Returns a new array with clones of the original array. </returns>
        public static T[] SubArray<T>(this T[] data, int index, int length)
        {
            T[] result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }
        public struct ReadPlacement
        {
            public string Sequence;
            public int StartPosition;
            public int EndPosition;
            public int Identifier;
            public string StartOverhang;
            public string EndOverhang;
            public ReadPlacement(string sequence, int startposition, int identifier)
            {
                Sequence = sequence;
                StartPosition = startposition;
                EndPosition = startposition + sequence.Length;
                Identifier = identifier;
                StartOverhang = "";
                EndOverhang = "";
            }
            public ReadPlacement(string sequence, int startposition, int identifier, string startoverhang, string endoverhang)
            {
                Sequence = sequence;
                StartPosition = startposition;
                EndPosition = startposition + sequence.Length;
                Identifier = identifier;
                StartOverhang = startoverhang;
                EndOverhang = endoverhang;
            }
        }
        /// <summary> This aligns a list of sequences to a template sequence based on the alphabet. </summary>
        /// <returns> Returns a list of tuples with the sequences as first item, startingposition as second item, 
        /// end position as third item and identifier from the given list as fourth item. </returns>
        /// <remark> This code does not account for small defects in reads, it will only align perfect matches 
        /// and it will only align matches tha fit entirely inside the template sequence (no overhang at the start or end). </remark>
        /// <param name="template"> The template to match against. </param>
        /// <param name="sequences"> The sequences to match with. </param>
        /// <param name="reverse"> Whether or not the alignment should also be done in reverse direction. </param>
        /// <param name="alphabet"> The alphabet to be used. </param>
        public static List<ReadPlacement> MultipleSequenceAlignmentToTemplate(string template, Dictionary<int, string> sequences, List<List<int>> positions, Alphabet alphabet, bool reverse = false)
        {
            // Keep track of all places already covered
            var result = new List<ReadPlacement>();

            // Loop through all to be placed reads
            foreach (var current in sequences)
            {
                int identifier = current.Key;
                string seq = current.Value;
                string seq_rev = string.Concat(seq.Reverse());
                int firsthit = -1;
                int lasthit = -1;

                // Try to find the first (and last) position it is in the originslist
                for (int i = 0; i < positions.Count(); i++)
                {
                    bool hit = false;
                    for (int j = 0; j < positions[i].Count(); j++)
                    {
                        if (positions[i][j] == identifier)
                        {
                            if (firsthit == -1) firsthit = i;
                            lasthit = i;
                            hit = true;
                            break;
                        }
                    }
                    if (!hit || i == positions.Count() - 1)
                    {
                        if (firsthit >= 0 && lasthit >= 0)
                        {
                            // Find the placement of the read in this patch

                            int lengthpatch = lasthit - firsthit;

                            if (lengthpatch == seq.Length)
                            {
                                // Determine forwards or backwards
                                if (reverse)
                                {
                                    int score_fw = GetPositionScore(ref template, ref seq, alphabet, firsthit);
                                    int score_bw = GetPositionScore(ref template, ref seq_rev, alphabet, firsthit + 1);
                                    if (score_fw >= score_bw) result.Add(new ReadPlacement(seq, firsthit, identifier));
                                    else result.Add(new ReadPlacement(seq_rev, firsthit, identifier));
                                }
                                else
                                {
                                    result.Add(new ReadPlacement(seq, firsthit, identifier));
                                }

                            }
                            else if (lengthpatch < seq.Length)
                            {
                                // Offset, score, reverse
                                var possibilities = new List<(int, ReadPlacement)>();

                                for (int offset = 0; offset < seq.Length - lengthpatch + 1; offset++)
                                {
                                    int score = 0;
                                    string tseq = "";
                                    if (reverse)
                                    {
                                        tseq = seq_rev.Substring(offset, lengthpatch);
                                        score = GetPositionScore(ref template, ref tseq, alphabet, firsthit + 1);
                                        possibilities.Add((score, new ReadPlacement(tseq, firsthit + 1, identifier, seq_rev.Substring(0, offset), seq_rev.Substring(offset + lengthpatch))));
                                    }
                                    tseq = seq.Substring(offset, lengthpatch);
                                    score = GetPositionScore(ref template, ref tseq, alphabet, firsthit);
                                    possibilities.Add((score, new ReadPlacement(tseq, firsthit, identifier, seq.Substring(0, offset), seq.Substring(offset + lengthpatch))));
                                }

                                var best = possibilities.First();
                                foreach (var option in possibilities)
                                {
                                    if (option.Item1 > best.Item1) best = option;
                                }
                                result.Add(best.Item2);
                            }
                            else
                            {
                                // The patch is bigger than the sequence??? how that??
                                throw new Exception($"While aligning read {seq} onto contig {template} the read seems to be shorter than the length of the match between the read and contig. (read length: {seq.Length}, length patch: {lengthpatch}).");
                            }

                            firsthit = -1;
                            lasthit = -1;
                        }
                    }
                }
            }

            return result;
        }

        static int GetPositionScore(ref string template, ref string read, Alphabet alphabet, int position)
        {
            int score = 0;
            for (int i = 0; i < read.Length && i < template.Length - position; i++)
            {
                score += alphabet.scoring_matrix[alphabet.getIndexInAlphabet(template[position + i]), alphabet.getIndexInAlphabet(read[i])];
            }
            return score;
        }
        /// <summary>DO a local alignment based on the SmithWaterman algorithm of two sequences. </summary>
        /// <param name="left">The template sequence to use.</param>
        /// <param name="right">The query sequence to use.</param>
        /// <param name="gap_penalty">The penalty for introducing a gap.</param>
        static public SequenceMatch SmithWaterman(AminoAcid[] left, AminoAcid[] right, int gap_penalty = 2)
        {
            var score_matrix = new (int, Direction)[left.Length + 1, right.Length + 1]; // Default value of 0
            int max_value = 0;
            (int, int) max_index = (0, 0);

            for (int x = 1; x <= left.Length; x++)
            {
                for (int y = 1; y <= right.Length; y++)
                {
                    int score = left[x - 1].Homology(right[y - 1]);
                    // Calculate the score for the current position
                    int a = score_matrix[x - 1, y - 1].Item1 + score; // Match
                    int b = score_matrix[x, y - 1].Item1 - gap_penalty; // GapRight
                    int c = score_matrix[x - 1, y].Item1 - gap_penalty; // GapLeft
                    int d = 0;
                    if (a > b && a > c && a > d) score_matrix[x, y] = (a, Direction.Match);
                    else if (b > c && b > d) score_matrix[x, y] = (b, Direction.GapRight);
                    else if (c > d) score_matrix[x, y] = (c, Direction.GapLeft);
                    else score_matrix[x, y] = (d, Direction.NoMatch);

                    // Keep track of the maximal value
                    if (score_matrix[x, y].Item1 > max_value)
                    {
                        max_value = score_matrix[x, y].Item1;
                        max_index = (x, y);
                    }
                }
            }

            // Traceback
            var match_list = new List<SequenceMatch.MatchPiece>();

            (int, int) index = max_index;
            int start_index = 0;
            bool found_end = false;
            while (!found_end)
            {
                int x = index.Item1;
                int y = index.Item2;
                if (x == 0 || y == 0) {
                    found_end = true;
                    start_index = x;
                    break;
                }
                switch (score_matrix[x, y].Item2)
                {
                    case Direction.Match:
                        match_list.Add(new SequenceMatch.Match(1));
                        index = (x - 1, y - 1);
                        break;
                    case Direction.GapLeft:
                        match_list.Add(new SequenceMatch.GapTemplate(1));
                        index = (x - 1, y);
                        break;
                    case Direction.GapRight:
                        match_list.Add(new SequenceMatch.GapContig(1));
                        index = (x, y - 1);
                        break;
                    case Direction.NoMatch:
                        found_end = true;
                        start_index = x;
                        break;
                }
            }
            match_list.Reverse();

            var match = new SequenceMatch(start_index, max_value, match_list);
            return match;
        }

        enum Direction { Match, GapLeft, GapRight, NoMatch }
    }
    /// <summary>A class to save a match of two sequences in a space efficient way, based on CIGAR strings.</summary>
    public class SequenceMatch
    {
        /// <summary>The position on the template where the match begins.</summary>
        public int StartPosition;
        public int Score;
        public List<MatchPiece> Alignment;
        public SequenceMatch(int pos, int s, List<MatchPiece> m)
        {
            StartPosition = pos;
            Score = s;
            Alignment = m;
            simplify();
        }
        public override string ToString() {
            var buffer = new StringBuilder();
            buffer.Append($"SequenceMatch< starting at: {StartPosition}, score: {Score}, match: ");
            foreach (var m in Alignment) {
                buffer.Append(m.ToString());
            }
            buffer.Append(" >");
            return buffer.ToString();
        }
        public abstract class MatchPiece
        {
            public int count;
            public MatchPiece(int c)
            {
                count = c;
            }
            public override string ToString() {
                return $"{count}None";
            }
        }
        public class Match : MatchPiece
        {
            public Match(int c) : base(c) { }
            public override string ToString() {
                return $"{count}M";
            }
        }
        public class GapTemplate : MatchPiece
        {
            public GapTemplate(int c) : base(c) { }
            public override string ToString() {
                return $"{count}D";
            }
        }
        public class GapContig : MatchPiece
        {
            public GapContig(int c) : base(c) { }
            public override string ToString() {
                return $"{count}I";
            }
        }
        void simplify()
        {
            MatchPiece lastElement = null;
            int count = Alignment.Count();
            int i = 0;
            while (i < count)
            {
                if (lastElement != null && lastElement.GetType() == Alignment[i].GetType())
                {
                    Alignment[i].count += Alignment[i - 1].count;
                    Alignment.RemoveAt(i - 1);
                    i--;
                    count--;
                }
                lastElement = Alignment[i];
                i++;
            }
        }
    }
}