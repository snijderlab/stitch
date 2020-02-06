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
    /// To contain definitions for file formats.
    /// </summary>
    public class FileFormat
    {
		//Review: waarom zijn al deze constructors static?

        /// <summary>
        /// To contain all options for PEAKS file formats.
        /// </summary>
        public class Peaks
        {
			//review: Alles een eigen lijn voor leesbaarheid. eventueel onderaan het document als je liever wilt dat het niet in de weg staat?
			//review: waarom zet je dingen naar -1? deze hoort toch niet opngeinitialiseerd gebruikt te worden?

			/// <summary>
			/// The position of this column in this peaks file format version.
			/// </summary>
			public int fraction;
            public int source_file;
            public int feature;
            public int scan;
            public int peptide;
            public int tag_length;
            public int alc;
            public int length;
            public int mz;
            public int z;
            public int rt;
            public int area;
            public int mass;
            public int ppm;
            public int ptm;
            public int local_confidence;
            public int tag;
            public int mode;

            /// <summary>
            /// An older version of a PEAKS export.
            /// </summary>
            /// <returns>The fileformat.</returns>
            public static FileFormat.Peaks OldFormat()
            {
                var pf = new FileFormat.Peaks();
                pf.scan = 0;
                pf.peptide = 1;
                pf.tag_length = 2;
                pf.alc = 3;
                pf.length = 4;
                pf.mz = 5;
                pf.z = 6;
                pf.rt = 7;
                pf.area = 8;
                pf.mass = 9;
                pf.ppm = 10;
                pf.ptm = 11;
                pf.local_confidence = 12;
                pf.tag = 13;
                pf.mode = 14;
                return pf;
            }

            /// <summary>
            /// A newer version of a PEAKS export.
            /// </summary>
            /// <returns>The fileformat.</returns>
            public static FileFormat.Peaks NewFormat()
            {
                var pf = new FileFormat.Peaks();
                pf.fraction = 0;
                pf.source_file = 1;
                pf.feature = 2;
                pf.peptide = 3;
                pf.scan = 4;
                pf.tag_length = 5;
                pf.alc = 6;
                pf.length = 7;
                pf.mz = 8;
                pf.z = 9;
                pf.rt = 10;
                pf.area = 11;
                pf.mass = 12;
                pf.ppm = 13;
                pf.ptm = 14;
                pf.local_confidence = 15;
                pf.tag = 16;
                pf.mode = 17;
                return pf;
            }

            /// <summary>
            /// An custom version of a PEAKS fileformat.
            /// </summary>
            /// <returns>The fileformat.</returns>
            public static FileFormat.Peaks CustomFormat(int fraction, int source_file, int feature, int scan, int peptide, int tag_length, int alc, int length, int mz, int z, int rt, int area, int mass, int ppm, int ptm, int local_confidence, int tag, int mode)
            {
                var pf = new FileFormat.Peaks();
                pf.fraction = fraction;
                pf.source_file = source_file;
                pf.feature = feature;
                pf.peptide = peptide;
                pf.scan = scan;
                pf.tag_length = tag_length;
                pf.alc = alc;
                pf.length = length;
                pf.mz = mz;
                pf.z = z;
                pf.rt = rt;
                pf.area = area;
                pf.mass = mass;
                pf.ppm = ppm;
                pf.ptm = ptm;
                pf.local_confidence = local_confidence;
                pf.tag = tag;
                pf.mode = mode;
                return pf;
            }
        }
    }
}