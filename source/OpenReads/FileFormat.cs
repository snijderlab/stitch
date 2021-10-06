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
        /// <summary>
        /// To contain the positions of a piece of information in the CSV export file. 
        /// The position signifies the column in the CSV file and a value of -1 signifies 
        /// that that piece of information is not included in this particular format.
        /// </summary>
        public class Peaks
        {
            public int fraction = -1;
            public int source_file = -1;
            public int feature = -1;
            public int scan = -1;
            public int peptide = -1;
            public int tag_length = -1;
            public int de_novo_score = -1;
            public int alc = -1;
            public int length = -1;
            public int mz = -1;
            public int z = -1;
            public int rt = -1;
            public int predicted_rt = -1;
            public int area = -1;
            public int mass = -1;
            public int ppm = -1;
            public int ptm = -1;
            public int local_confidence = -1;
            public int tag = -1;
            public int mode = -1;
            public string name = "undefined";

            /// <summary>
            /// An older version of a PEAKS export.
            /// </summary>
            /// <returns>The fileformat.</returns>
            public static FileFormat.Peaks OldFormat()
            {
                return new FileFormat.Peaks
                {
                    scan = 0,
                    peptide = 1,
                    tag_length = 2,
                    alc = 3,
                    length = 4,
                    mz = 5,
                    z = 6,
                    rt = 7,
                    area = 8,
                    mass = 9,
                    ppm = 10,
                    ptm = 11,
                    local_confidence = 12,
                    tag = 13,
                    mode = 14,
                    name = "Old"
                };
            }

            /// <summary>
            /// Version X of PEAKS export. (made for build 31 january 2019)
            /// </summary>
            /// <returns>The fileformat.</returns>
            public static FileFormat.Peaks PeaksX()
            {
                return new FileFormat.Peaks
                {
                    fraction = 0,
                    source_file = 1,
                    feature = 2,
                    peptide = 3,
                    scan = 4,
                    tag_length = 5,
                    alc = 6,
                    length = 7,
                    mz = 8,
                    z = 9,
                    rt = 10,
                    area = 11,
                    mass = 12,
                    ppm = 13,
                    ptm = 14,
                    local_confidence = 15,
                    tag = 16,
                    mode = 17,
                    name = "X"
                };
            }

            /// <summary>
            /// Version X+ of PEAKS export. (made for build 20 november 2019)
            /// </summary>
            /// <returns>The fileformat.</returns>
            public static FileFormat.Peaks PeaksXPlus()
            {
                return new FileFormat.Peaks
                {
                    fraction = 0,
                    source_file = 1,
                    feature = 2,
                    peptide = 3,
                    scan = 4,
                    tag_length = 5,
                    de_novo_score = 6,
                    alc = 7,
                    length = 8,
                    mz = 9,
                    z = 10,
                    rt = 11,
                    predicted_rt = 12,
                    area = 13,
                    mass = 14,
                    ppm = 15,
                    ptm = 16,
                    local_confidence = 17,
                    tag = 18,
                    mode = 19,
                    name = "X+"
                };
            }

            /// <summary>
            /// A custom version of a PEAKS fileformat.
            /// </summary>
            /// <returns>The fileformat.</returns>
            public static FileFormat.Peaks CustomFormat(int fraction, int sourceFile, int feature, int scan, int peptide, int tagLength, int deNovoScore, int alc, int length, int mz, int z, int rt, int predictedRT, int area, int mass, int ppm, int ptm, int localConfidence, int tag, int mode)
            {
                return new FileFormat.Peaks
                {
                    fraction = fraction,
                    source_file = sourceFile,
                    feature = feature,
                    peptide = peptide,
                    scan = scan,
                    tag_length = tagLength,
                    de_novo_score = deNovoScore,
                    alc = alc,
                    length = length,
                    mz = mz,
                    z = z,
                    rt = rt,
                    predicted_rt = predictedRT,
                    area = area,
                    mass = mass,
                    ppm = ppm,
                    ptm = ptm,
                    local_confidence = localConfidence,
                    tag = tag,
                    mode = mode,
                    name = "Custom"
                };
            }
        }
    }
}