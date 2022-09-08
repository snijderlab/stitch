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
using System.Net.Http;

namespace AssemblyNameSpace
{
    /// <summary> The main class which is the entry point from the command line. </summary>
    public class ToRunWithCommandLine
    {
        static readonly Stopwatch stopwatch = new();

        /// <summary> The entry point. </summary>
        static int Main()
        {
            Console.CancelKeyPress += HandleUserAbort;

            static List<string> ParseArgs()
            {
                string args = Environment.CommandLine.Trim();
                string current_arg = "";
                var output = new List<string>();
                for (int i = 0; i < args.Length; i++)
                {
                    switch (args[i])
                    {
                        case ' ':
                            output.Add(current_arg.Trim());
                            current_arg = "";
                            if (args[i + 1] == '-' && args[i + 2] == '-')
                            {
                                var end = args.IndexOf(' ', i + 1);
                                end = end < 0 ? args.Length - 1 : end;
                                output.Add(args.Substring(i + 1, end - i - 1));
                            }
                            break;
                        case '\'':
                            if (!string.IsNullOrEmpty(current_arg))
                                output.Add(current_arg.Trim());
                            current_arg = "";
                            int next = args.IndexOf('\'', i + 1);
                            output.Add(args.Substring(i + 1, next - i - 1).Trim());
                            i = next + 1;
                            break;
                        case '\"':
                            if (!string.IsNullOrEmpty(current_arg))
                                output.Add(current_arg.Trim());
                            current_arg = "";
                            int n = args.IndexOf('\"', i + 1);
                            output.Add(args.Substring(i + 1, n - i - 1).Trim());
                            i = n + 1;
                            break;
                        default:
                            current_arg += args[i];
                            break;
                    }
                }
                if (!string.IsNullOrEmpty(current_arg))
                    output.Add(current_arg.Trim());

                return output;
            }

            // Retrieve the name of the batch file to run or file to clean
            var args = ParseArgs();
            string filename = "";
            string output_filename = "";

            if (args.Count <= 1 || args[1] == "help" || args[1] == "?")
            {
                Console.WriteLine(@"Please provide as the first and only argument the path to the batch file to be run.

Or use any of the following commands:
* clean - removes duplicates and incomplete sequences from fasta
    1   [path] the fasta file to clean 
    2?  [path] possibly followed by the new name for the cleaned file (if missing it will overwrite the old file)
eg: assembler.exe clean Homo_sapiens_IGHV.fasta

* annotate - parses the sequences from an IMGT html file and creates annotated fasta files
    1   [path] the html file to parse the annotated sequences from 
    2?  [path] possibly followed by the new name for the cleaned file (if missing it will overwrite the old file).
eg: assembler.exe annotate Homo_sapiens_IGHV.html Homo_sapiens_IGHV.fasta
note: for an example IMGT html file see: http://www.imgt.org/IMGTrepertoire/Proteins/proteinDisplays.php?species=human&latin=Homo%20sapiens&group=IGHV

* download - creates annotated fasta files for the given species
    1   [string] the latin name of the species to download (as used by IMGT: http://www.imgt.org/IMGTrepertoire/Proteins/)
    2?  [string] the segments to download, multiple can be downloaded in one file by combining with a comma ','
        default: ""IGHV IGKV,IGLV IGHJ IGKJ,IGLJ IGKC,IGLC""
eg: assembler.exe download ""Homo sapiens""
note: IGHC is not included as this is not present in a useful form in the IMGT database, get from uniprot for the best results");
                return 2;
            }

            try
            {
                filename = args[1];
                if (filename == "clean")
                {
                    filename = args[2];
                    if (args.Count > 3)
                        output_filename = args[3];
                    else
                        output_filename = filename;
                    CleanFasta(filename, output_filename);
                }
                else if (filename == "annotate")
                {
                    filename = args[2];
                    if (args.Count > 3)
                        output_filename = args[3];
                    else
                        output_filename = filename;

                    string content = InputNameSpace.ParseHelper.GetAllText(InputNameSpace.ParseHelper.GetFullPath(filename).ReturnOrFail()).ReturnOrFail();
                    GenerateAnnotatedTemplate(content, output_filename);
                }
                else if (filename == "download")
                {
                    if (args.Count > 3)
                        DownloadSpecies(args[2], args[3]);
                    else
                        DownloadSpecies(args[2]);
                }
                else
                {
                    filename = args[1];
                    var location_expected = args.IndexOf("--expect");
                    var expected = location_expected > 0 ? args[location_expected + 2].Split(',').ToList() : new List<string>();
                    RunBatchFile(filename, new RunVariables(args.Contains("--open"), args.Contains("--live"), expected));
                }
            }
            catch (ParseException)
            {
                return 3;
            }
            catch (Exception e)
            {
                Console.WriteLine($"ERROR: {e.Message}\nSTACKTRACE: {e.StackTrace}\nSOURCE: {e.Source}\nTARGET: {e.TargetSite}");
                return 1;
            }
            return 0;
        }

        /// <summary>
        /// Run the given batch file
        /// </summary>
        /// <param name="filename"></param>
        public static void RunBatchFile(string filename, RunVariables runVariables)
        {
            ProgressBar bar = null;
            if (runVariables.ExpectedResult.Count == 0)
            {
                bar = new ProgressBar();
                bar.Start(4); // Max steps, can be turned down if no Recombination is done
            }

            var inputparams = ParseCommandFile.Batch(filename, false);
            if (runVariables.ExpectedResult.Count == 0)
            {
                bar.Update();
                bar.Start(inputparams.Recombine != null ? 4 : 3);
            }

            inputparams.CreateRun(runVariables, bar).Calculate();
        }

        /// <summary>
        /// Gracefully handles user abort by printing a final message and the total time the program ran, after that it aborts.
        /// </summary>
        static void HandleUserAbort(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            var def = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("== Aborted based on user input ==");
            Console.ForegroundColor = def;
            Console.WriteLine($"Total time ran {HelperFunctionality.DisplayTime(stopwatch.ElapsedMilliseconds)}.");
            Console.Out.Flush();
            Environment.Exit(1);
        }

        /// <summary> Cleans the given fasta file by deleting duplicates and removing sequences tagged as 'partial'. </summary>
        static void CleanFasta(string filename, string output)
        {
            var path = InputNameSpace.ParseHelper.GetFullPath(filename).ReturnOrFail();
            var namefilter = new NameFilter();
            var reads = OpenReads.Fasta(namefilter, new ReadMetaData.FileIdentifier(path, "name", null), new Regex("^[^|]*\\|([^|]*)\\*\\d\\d\\|")).ReturnOrFail();
            SaveAndCleanFasta(output, reads);
        }

        /// <summary>
        /// Do common deduplication and clean up steps. Take note: Assumes all MetaData to be of type ReadMetaData.Fasta.
        /// </summary>
        /// <param name="output">The file to save the fasta sequences in.</param>
        /// <param name="reads">The reads/sequences itself.</param>
        private static void SaveAndCleanFasta(string output, List<(string Sequence, ReadMetaData.IMetaData MetaData)> reads)
        {
            // Two dictionaries to ensure both unique ids (to join isoforms) and unique sequences.
            var id_dict = new Dictionary<string, (string, string)>();
            var sequence_dict = new Dictionary<string, string>();

            // Condense all isoforms
            foreach (var read in reads)
            {
                var long_id = ((ReadMetaData.Fasta)read.MetaData).FastaHeader;
                if (!long_id.Contains("partial") && !long_id.Contains("/OR")) // Filter out all partial variants and /OR variants
                {
                    // Join all isoforms
                    if (id_dict.ContainsKey(read.MetaData.Identifier))
                    {
                        id_dict[read.MetaData.Identifier] = (id_dict[read.MetaData.Identifier].Item1 + " " + long_id, read.Sequence);
                    }
                    else
                    {
                        // Join all equal sequences
                        if (sequence_dict.ContainsKey(read.Sequence))
                        {
                            var deduplicated_id = sequence_dict[read.Sequence];
                            id_dict[deduplicated_id] = (id_dict[deduplicated_id].Item1 + " " + long_id, read.Sequence);
                        }
                        else
                        {
                            id_dict[read.MetaData.Identifier] = (long_id, read.Sequence);
                            sequence_dict[read.Sequence] = read.MetaData.Identifier;
                        }
                    }
                }
            }

            var sb = new StringBuilder();
            foreach (var (_, (id, seq)) in id_dict)
            {
                sb.AppendLine($">{id}");
                foreach (var chunk in seq.Chunk(80)) // Chunk the sequence so the lines are always max 80 chars long
                {
                    sb.AppendLine(new String(chunk));
                }
            }

            File.WriteAllText(InputNameSpace.ParseHelper.GetFullPath(output).ReturnOrFail(), sb.ToString());
        }

        static List<(String CommonName, String ShortName, String ShortHand, String ScientificName)> predefined_species = new List<(String, String, String, String)> {
            ("Mammalia",                   "Mammalia",    "Ma",         ""),
            ("human",                      "human",       "Hu",         "Homo sapiens"),
            ("house mouse",                "mouse",       "Mu",         "Mus musculus"),
            ("sheep",                      "sheep",       "Sh",         "Ovis aries"),
            ("bovine",                     "Btaurus",     "Bt",         "Bos taurus"),
            ("pig",                        "pig",         "Sc",         "Sus scrofa"),
            ("gray short-tailed opossum",  "opossum",     "Md",         "Monodelphis domestica"),
            ("common brush-tailed possum", "possum",      "Tv",         "Trichosurus vulpecula"),
            ("Arabian camel",              "camel",       "Cd",         "Camelus dromedarius"),
            ("domestic horse",             "horse",       "Ec",         "Equus caballus"),
            ("dog",                        "dog",         "Cf",         "Canis lupus familiaris"),
            ("Norway rat",                 "rat",         "Rn",         "Rattus norvegicus"),
            ("Chimpanzee",                 "chimpanzee",  "Pt",         "Pan troglodytes"),
            ("Common gibbon",              "gibbon",      "Hl",         "Hylobates lar"),
            ("Gorilla",                    "gorilla",     "Gg",         "Gorilla gorilla"),
            ("Bornean orangutan",          "orangutan",   "Pp",         "Pongo pygmaeus"),
            ("Macaques",                   "macaque",     "Macaca",     ""),
            ("rabbit",                     "rabbit",      "Rb",         "Oryctolagus cuniculus"),
            ("platypus",                   "platypus",    "Pl",         "Ornithorhynchus anatinus"),
            ("Teleostei",                  "teleostei",   "Teleostei",  ""),
            ("horn shark",                 "Hshark",      "Hs",         "Heterodontus francisci"),
            ("Spotted ratfish",            "Sratfish",    "Sr",         "Hydrolagus colliei"),
            ("Spotted wobbegong shark",    "Wshark",      "Ws",         "Orectolobus maculatus"),
            ("clearnose skate",            "Cskate",      "Cs",         "Raja eglanteria"),
            ("Little skate",               "Lskate",      "Ls",         "Leucoraja erinacea"),
            ("African lungfish",           "Lungfish",    "Pa",         "Protopterus aethiopicus")
        };

        static void DownloadSpecies(string name, string segments = "IGHV IGKV,IGLV IGHJ IGKJ,IGLJ IGHC IGKC,IGLC")
        {
            foreach (var single in name.Split(','))
            {
                DownloadSingleSpecies(single, segments);
            }
        }

        /// <summary>
        /// Download a set of templates for a mammalian organism assuming the same structure as Homo sapiens.
        /// </summary>
        /// <param name="name"> The name of the species, it will be matched to all predefined names. It can 
        /// be scientific name, common name, shorthand or short name (matched in that order). </param>
        static void DownloadSingleSpecies(string name, string segments = "IGHV IGKV,IGLV IGHJ IGKJ,IGLJ IGHC IGKC,IGLC")
        {
            (string CommonName, string ShortName, string ShortHand, string ScientificName) species = ("", "", "", "");
            var found = false;
            name = name.ToLower().Trim();
            foreach (var sp in predefined_species)
            {
                if (sp.ScientificName.ToLower() == name
                 || sp.CommonName.ToLower() == name
                 || sp.ShortHand.ToLower() == name
                 || sp.ShortName.ToLower() == name)
                {
                    species = sp;
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                Console.WriteLine("Could not find given species");
                return;
            }

            var basename = $"http://www.imgt.org/3Dstructure-DB/cgi/DomainDisplay-include.cgi?species={species.ScientificName.Replace(" ", "%20")}&groups=";
            HttpClient client = new();
            Console.WriteLine(species.ScientificName);
            foreach (var segment in segments.Split(' '))
            {
                try
                {
                    Console.WriteLine(segment);
                    if (segment == "IGHC")
                    {
                        try
                        {
                            var url_base = $"https://www.imgt.org/IMGTrepertoire/Proteins/protein/{species.ShortName}/IGH/IGHC/{species.ShortHand}_IGHCallgene";
                            Task<string> download;
                            try // Sometimes the url is different using `IGHCallgenes` instead of `IGHCallgene`
                            {
                                download = client.GetStringAsync(url_base + ".html");
                                download.Wait();
                            }
                            catch
                            {
                                download = client.GetStringAsync(url_base + "s.html");
                                download.Wait();
                            }
                            try
                            {
                                CreateAnnotatedTemplatePre(download.Result, species.ScientificName.Replace(' ', '_') + "_" + segment + ".fasta");
                            }
                            catch
                            {
                                Console.WriteLine($"    Could not process IGHC file");
                            }
                        }
                        catch
                        {
                            Console.WriteLine($"    Could not download IGHC file");
                        }
                    }
                    else
                    {
                        var download = client.GetStringAsync(basename + segment);
                        download.Wait();
                        GenerateAnnotatedTemplate(download.Result, species.ScientificName.Replace(' ', '_') + "_" + segment + ".fasta");
                    }
                }
                catch
                {
                    Console.WriteLine("   Not available");
                }
            }
            File.Delete("temp.html");
        }

        /// <summary>
        /// Create an annotated fasta file from a HTML page from IMGT in the old format, based on &lt;pre&gt; or preredered text.
        /// </summary>
        /// <param name="filename">The HTML file</param>
        /// <param name="output">The file name for the Fasta file</param>
        static void CreateAnnotatedTemplatePre(string content, string output)
        {
            // Extract the main block of content
            content = content.Substring(content.IndexOf("<pre>") + 5);
            content = content.Substring(0, Regex.Match(content, "</pre>", RegexOptions.IgnoreCase).Index);
            var title_re = new Regex(@"<B><FONT size=\+1>\s*(\w+)\n?</FONT></B>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            var matches = title_re.Matches(content);
            var pieces = new List<(string, Dictionary<string, string>)>();

            // Go over all regions (CH1, CH2...)
            foreach (Match match in matches)
            {
                var name = match.Groups[1].Value.Trim();
                var subcontent = content.Substring(match.Groups[0].Index + match.Groups[0].Value.Length); // skip above pattern
                var dict = new Dictionary<string, string>();
                subcontent = subcontent.TrimStart();

                // Go over all lines, which all contain a single isotype plus its sequence
                while (!subcontent.StartsWith("<B") && !subcontent.StartsWith("<b") && subcontent.Length > 0)
                {
                    var index = subcontent.IndexOf("\n");
                    string line;
                    if (index > 0)
                    {
                        line = subcontent.Substring(0, index).Trim();
                        subcontent = subcontent.Substring(index).TrimStart();
                    }
                    else
                    {
                        line = subcontent;
                    }
                    // Parse the line to retrieve the isotype name and sequence and remove all other stuff
                    var line_re = new Regex(@"^.+(IG\w+)[^<]*</font>.+?(\(.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                    var line_match = line_re.Match(line);
                    var isotype = line_match.Groups[1].Value.Trim();
                    if (string.IsNullOrEmpty(isotype)) continue;
                    var sequence = line_match.Groups[2].Value;
                    // Remove all mark up of the sequence and leave only the sequence itself
                    sequence = Regex.Replace(sequence, @"(</?[^>]+>)|(\(\d+\))|(\[\w+\])|\.| |\(|\)|\d+", "");
                    dict.Add(isotype, sequence);
                }
                pieces.Add((name, dict));
            }

            // If there is a hinge region position it at the right place (after CH1), otherwise expect the regions in the right order
            var pos = pieces.FindIndex(a => a.Item1.ToLower() == "hinge");
            if (pos > 0)
            {
                var region = (pieces[pos].Item1.Clone() as string, new Dictionary<string, string>(pieces[pos].Item2));
                for (int i = pieces.Count - 1; i >= 1; i--)
                    pieces[i] = pieces[i - 1]; // Shift all regions one to the side starting at the back to make place for the Hinge region
                pieces[1] = region;
            }

            // Join all pieces from all regions in order
            var results = new Dictionary<string, string>();
            foreach (var region in pieces)
            {
                foreach (var (isotype, sequence) in region.Item2)
                {
                    if (isotype != "IGHGP") // This is a known pseudogene which is present on some pages
                        if (results.ContainsKey(isotype))
                            results[isotype] = results[isotype] + sequence;
                        else
                            results.Add(isotype, sequence);
                }
            }

            var namefilter = new NameFilter();
            var file = new ReadMetaData.FileIdentifier();
            var list = new List<(string, ReadMetaData.IMetaData)>(results.Count);
            foreach (var (isotype, sequence) in results)
            {
                list.Add((sequence, new ReadMetaData.Fasta(isotype, isotype, file, namefilter)));
            }

            SaveAndCleanFasta(output, list);
        }

        /// <summary> Generates an annotated fasta file from the HTML files from IMGT </summary>
        static void GenerateAnnotatedTemplate(string content, string output, bool remove_gaps = true)
        {
            var sequences = new List<(string Sequence, ReadMetaData.IMetaData)>();
            var namefilter = new NameFilter();
            var file = new ReadMetaData.FileIdentifier();

            content = content.Substring(content.IndexOf("<table class=\"tableseq\">"));
            content = content.Substring(0, content.IndexOf("</table>"));
            content = content.Substring(content.IndexOf("</pre></td>\n") + 12);
            while (content.StartsWith("</tr>\n<tr>\n"))
            {
                content = content.Substring(content.IndexOf("</tr>\n<tr>\n") + 11);
                int newline = content.IndexOf('\n');
                var line = content.Substring(0, newline);
                content = content.Substring(newline + 1);

                if (line == "<td class=\"separegroup\"></td>") continue;

                var pieces = line.Split("</td><td>").ToArray();
                // 0 - nothing
                // 1 - Species
                // 2 - ID
                // 3 - Link Allele
                // 4 - Link AccNum
                // 5 - Segment
                // 6 - Functionality
                // 7 - Sequence
                bool J = pieces[5] == "J-REGION";
                bool V = new List<String> { "VH", "VL", "V-KAPPA", "V-LAMBDA", "V-ALPHA", "V-BETA", "V-GAMMA", "V-DELTA" }.Contains(pieces[5]);
                bool C = new List<String> { "CL", "C-KAPPA", "C-LAMBDA", "C-IOTA", "C-ALPHA", "C-BETA", "C-BETA-1", "C-BETA-2", "C-GAMMA", "C-GAMMA-1", "C-GAMMA-2", "C-DELTA" }.Contains(pieces[5]);
                var sequence = pieces[7].Substring(5, pieces[7].Length - 5 - 11); // Strip opening <pre> and closing </pre></td>

                if (pieces[6] == "ORF" || pieces[6] == "P") continue; // Remove Pseudogenes and Open Reading Frames 

                List<(List<string> classes, string seq)> ParseSequence(string input, List<string> current_classes, string current_seq, List<(List<string> classes, string seq)> result)
                {
                    if (input.Length == 0)
                    {
                        if (!string.IsNullOrEmpty(current_seq))
                            result.Add((new List<string>(current_classes), current_seq));
                        return result;
                    }
                    else if (input.StartsWith("<span"))
                    {
                        if (!string.IsNullOrEmpty(current_seq))
                            result.Add((new List<string>(current_classes), current_seq));
                        int i = 13; // Skip to classname
                        for (int j = 0; i + j < input.Length; j++)
                        {
                            if (input[i + j] == '"')
                            {
                                current_classes.Add(input.Substring(i, j));
                                i += j + 2;
                                break;
                            }
                        }
                        return ParseSequence(input.Substring(i), current_classes, "", result);
                    }
                    else if (input.StartsWith("</span>"))
                    {
                        if (!string.IsNullOrEmpty(current_seq))
                            result.Add((new List<string>(current_classes), current_seq));
                        current_classes.RemoveAt(current_classes.Count - 1);
                        return ParseSequence(input.Substring(7), current_classes, "", result);
                    }
                    else if (input.StartsWith(' ') || (remove_gaps && input.StartsWith('.')))
                    {
                        return ParseSequence(input.Substring(1), current_classes, current_seq, result);
                    }
                    else if (input.StartsWith(Alphabet.StopCodon))
                    {
                        return null; // Contains a stop codon so is not a valid sequence
                    }
                    else
                    {
                        return ParseSequence(input.Substring(1), current_classes, current_seq + input[0], result);
                    }
                }

                var final_sequence = ParseSequence(sequence.Replace("<b>", "").Replace("</b>", ""), new List<string>(), "", new List<(List<string> classes, string seq)>());
                if (final_sequence == null)
                    continue;
                var typed = new List<(string Type, string Sequence)>();
                foreach (var (classes, seq) in final_sequence)
                {
                    var type = "";
                    foreach (var classname in classes)
                    {
                        switch (classname)
                        {
                            case "loop":
                                if (V) // Only in the V region are loops designated as CDRs
                                    type = "CDR";
                                break;
                            case "J-motif":
                            case "1st-CYS": // Used for any CYS
                            case "CONSERVED-TRP": // Used for any conserved amino acid
                                type = "Conserved";
                                break;
                            case "N-glycosylation":
                                type = "Glycosylationsite";
                                break;
                            case "mutation":
                                break;
                            default:
                                type = "UNKOWN: " + classname;
                                break;
                        };
                    }
                    typed.Add((type, seq));
                }
                var compressed = new List<(string Type, string Sequence)>();
                (string Type, string Sequence) last = ("", "");
                foreach (var piece in typed)
                {
                    if (piece.Type == last.Type)
                    {
                        last = (last.Type, last.Sequence + piece.Sequence);
                    }
                    else
                    {
                        compressed.Add(last);
                        last = piece;
                    }
                }
                compressed.Add(last);

                int cdr = 1;
                bool cdr_found = false;
                bool any_cdr_found = false;
                for (int i = 0; i < compressed.Count; i++)
                {
                    if (compressed[i].Type == "CDR")
                    {
                        compressed[i] = ("CDR" + cdr.ToString(), compressed[i].Sequence);
                        cdr_found = true;
                        any_cdr_found = true;
                    }
                    else if (string.IsNullOrEmpty(compressed[i].Type) && cdr_found)
                    {
                        cdr++;
                        cdr_found = false;
                    }
                }
                if (cdr > 1 && !cdr_found || cdr >= 1 && cdr < 3 && any_cdr_found)
                    continue; // It misses the last CDR from the V segment so it is 'partial' and should be removed
                if (compressed.Any(a => Regex.Matches(a.Sequence, "[^ACDEFGHIKLMNOPQRSTUVWY]", RegexOptions.IgnoreCase).Count > 0))
                    continue; // It has non Amino Acids characters in the raw sequence

                // J segments always start with the last pieces of CDR3 but this is not properly annotated in the HTML files
                if (J)
                    compressed[0] = ("CDR3", compressed[0].Sequence);

                var encoded_sequence = new StringBuilder();
                foreach (var piece in compressed)
                {
                    if (string.IsNullOrEmpty(piece.Type))
                        encoded_sequence.Append(piece.Sequence);
                    else
                        encoded_sequence.Append($"({piece.Type} {piece.Sequence})");
                }
                sequences.Add((encoded_sequence.ToString(), new ReadMetaData.Fasta(pieces[2], pieces[2], file, namefilter)));
            }
            SaveAndCleanFasta(output, sequences);
        }
    }
}