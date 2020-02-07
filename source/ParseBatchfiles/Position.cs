using System;
using System.IO;

namespace AssemblyNameSpace
{
    /// <summary>
    /// To keep track of a location in a file, for example for error messages.
    /// </summary>
    public struct Position
    {
        /// <summary>
        /// The file this position is in
        /// </summary>
        public readonly ParsedFile File;

        /// <summary>
        /// The Line number (0 based) in the file
        /// </summary>
        public readonly int Line;

        /// <summary>
        /// The column number (1-based) on the line
        /// </summary>
        public readonly int Column;

        /// <summary>
        /// Creates a new location with the given parameters
        /// </summary>
        /// <param name="line">The line (0-based)</param>
        /// <param name="column">The column (1-based)</param>
        /// <param name="file">The file</param>
        public Position(int line, int column, ParsedFile file)
        {
            Line = line;
            Column = column;
            File = file;
        }

        /// <summary>
        /// Summarises this location into a string for human readability
        /// </summary>
        public override string ToString()
        {
            return $"{File}:{Line},{Column}";
        }
        public static bool operator ==(Position p1, Position p2)
        {
            return p1.Equals(p2);
        }
        public static bool operator !=(Position p1, Position p2)
        {
            return !p1.Equals(p2);
        }
        public override bool Equals(object p2)
        {
            if (p2.GetType() != this.GetType()) return false;
            Position pos = (Position)p2;
            return this.Line == pos.Line && this.Column == pos.Column;
        }
        public override int GetHashCode()
        {
            return Line.GetHashCode() + Column.GetHashCode();
        }
    }

    /// <summary>
    /// Tracks a range in a file, for example the range of a keyword
    /// </summary>
    public struct Range
    {
        /// <summary>
        /// The file this positions are in
        /// </summary>
        public readonly ParsedFile File;

        /// <summary>
        /// The start position
        /// </summary>
        public readonly Position Start;

        /// <summary>
        /// The end position
        /// </summary>
        public readonly Position End;

        /// <summary>
        /// Creates a new range
        /// </summary>
        /// <param name="start">The start position</param>
        /// <param name="end">The end position</param>
        /// <exception cref="ArgumentException">If the two positions are not in the same file, or if the start position is after the end position.</exception>
        public Range(Position start, Position end)
        {
            Start = start;
            End = end;
            File = start.File;
            if (start.File != end.File)
            {
                throw new ArgumentException("Two positions in two different files do not form a range in a single file.");
            }
            if (start.Line > end.Line || (start.Line == end.Line && start.Column > end.Column))
            {
                throw new ArgumentException($"The Start position '{start}' cannot be before the End '{end}' position.");
            }
        }

        /// <summary>
        /// Summarises this range into a string for human readability
        /// </summary>
        public override string ToString()
        {
            return $"{Start} to {End}";
        }
    }

    /// <summary>
    /// Tracks a range of a key in a keyvalue element in a file, |Key| :&gt; &lt;:|
    /// </summary>
    public struct KeyRange
    {
        /// <summary>
        /// The file of the range
        /// </summary>
        public readonly ParsedFile File;

        /// <summary>
        /// The start of the range, |Key :&gt; &lt;:
        /// </summary>
        public readonly Position Start;

        /// <summary>
        /// The end of the key, Key| :&gt; &lt;:
        /// </summary>
        public readonly Position NameEnd;

        /// <summary>
        /// The end of the field, Key :&gt; &lt;:|
        /// </summary>
        public readonly Position FieldEnd;

        /// <summary>
        /// The full range, from <see cref="Start"/> to <see cref="FieldEnd"/>, |Key :&gt; &lt;:|
        /// </summary>
        public Range Full
        {
            get
            {
                return new Range(Start, FieldEnd);
            }
        }

        /// <summary>
        /// The range of the name, from <see cref="Start"/> to <see cref="NameEnd"/>, |Key| :&gt; &lt;:
        /// </summary>
        public Range Name
        {
            get
            {
                return new Range(Start, NameEnd);
            }
        }

        /// <summary>
        /// Creates a KeyRange
        /// </summary>
        /// <param name="start">Start position, <see cref="Start"/></param>
        /// <param name="nameEnd">NameEnd position, <see cref="NameEnd"/></param>
        /// <param name="fieldEnd">FieldEnd position, <see cref="FieldEnd"/></param>
        /// <exception member="ArgumentException">If the positions are not in the same file or if positions are not in the right order.</exception>
        public KeyRange(Position start, Position nameEnd, Position fieldEnd)
        {
            Start = start;
            NameEnd = nameEnd;
            FieldEnd = fieldEnd;
            File = start.File;
            if (start.File != nameEnd.File || nameEnd.File != fieldEnd.File)
            {
                throw new ArgumentException("Three positions in different files do not form a range in a single file.");
            }
            if (Start.Line > NameEnd.Line || (Start.Line == NameEnd.Line && Start.Column > NameEnd.Column))
            {
                throw new ArgumentException("The Start position cannot be before the NameEnd position.");
            }
            if (Start.Line > FieldEnd.Line || (Start.Line == FieldEnd.Line && Start.Column > FieldEnd.Column))
            {
                throw new ArgumentException("The Start position cannot be before the FieldEnd position.");
            }
            if (NameEnd.Line > FieldEnd.Line || (NameEnd.Line == FieldEnd.Line && NameEnd.Column > FieldEnd.Column))
            {
                throw new ArgumentException("The NameEnd position cannot be before the FieldEnd position.");
            }
        }

        /// <summary>
        /// Creates a KeyRange
        /// </summary>
        /// <param name="name">Name range, from <see cref="Start"/> to <see cref="NameEnd"/></param>
        /// <param name="fieldEnd">FieldEnd position, <see cref="FieldEnd"/></param>
        /// <exception member="ArgumentException">If the positions are not in the same file or if positions are not in the right order.</exception>
        public KeyRange(Range name, Position fieldEnd)
        {
            Start = name.Start;
            NameEnd = name.End;
            FieldEnd = fieldEnd;
            File = name.File;
            if (name.File != fieldEnd.File)
            {
                throw new ArgumentException("Two positions in two different files do not form a range in a single file.");
            }
            if (Start.Line > NameEnd.Line || (Start.Line == NameEnd.Line && Start.Column > NameEnd.Column))
            {
                throw new ArgumentException("The Start position cannot be before the NameEnd position.");
            }
            if (Start.Line > FieldEnd.Line || (Start.Line == FieldEnd.Line && Start.Column > FieldEnd.Column))
            {
                throw new ArgumentException("The Start position cannot be before the FieldEnd position.");
            }
            if (NameEnd.Line > FieldEnd.Line || (NameEnd.Line == FieldEnd.Line && NameEnd.Column > FieldEnd.Column))
            {
                throw new ArgumentException("The NameEnd position cannot be before the FieldEnd position.");
            }
        }

        /// <summary>
        /// Summarises this range into a string for human readability
        /// </summary>
        public override string ToString()
        {
            return $"{Start} to {NameEnd} to {FieldEnd}";
        }
    }

    /// <summary>
    /// Saves a file to use with positions
    /// </summary>
    public class ParsedFile
    {
        /// <summary>
        /// The filename
        /// </summary>
        public readonly string Filename;

        /// <summary>
        /// The content of this file, as an array of all lines
        /// </summary>
        public readonly string[] Lines;

        /// <summary>
        /// Creates a new ParsedFile
        /// </summary>
        /// <param name="name">The filename (will be resolved to full path)</param>
        /// <param name="content">The file content, as an array of all lines</param>
        public ParsedFile(string name, string[] content)
        {
            Filename = name;
            Lines = content;
        }

        /// <summary>
        /// Creates an empty ParsedFile
        /// </summary>
        public ParsedFile()
        {
            Filename = "";
            Lines = new string[0];
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType() != this.GetType())
            {
                return false;
            }
            else
            {
                var that = (ParsedFile)obj;
                if (this.Filename == that.Filename) return true;
                else return false;
            }
        }

        public override int GetHashCode()
        {
            return 23 + 17 * Filename.GetHashCode();
        }

        public override string ToString()
        {
            return Filename;
        }
    }
}
