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
    
}