using System;
using System.Text.Json.Serialization;

namespace Stitch
{
    /// <summary> To keep track of a location in a file, for example for error messages. </summary> 
    public struct Position
    {
        /// <summary> The file this position is in </summary>
        public readonly ParsedFile File;

        /// <summary> The Line number (0 based) in the file </summary>
        public readonly int Line;

        /// <summary> The column number (1-based) on the line </summary>
        public readonly int Column;

        /// <summary> Creates a new location with the given parameters </summary>
        /// <param name="line">The line (0-based)</param>
        /// <param name="column">The column (1-based)</param>
        /// <param name="file">The file</param>
        public Position(int line, int column, ParsedFile file)
        {
            Line = line;
            Column = column;
            File = file;
        }

        /// <summary> Summarises this location into a string for human readability </summary>
        public override string ToString()
        {
            return $"{File}:{Line + 1},{Column}";
        }
        public static bool operator ==(Position p1, Position p2)
        {
            return p1.Equals(p2);
        }
        public static bool operator !=(Position p1, Position p2)
        {
            return !p1.Equals(p2);
        }
        public override bool Equals(object obj)
        {
            if (obj.GetType() != this.GetType()) return false;
            Position pos = (Position)obj;
            return this.Line == pos.Line && this.Column == pos.Column;
        }
        public override int GetHashCode()
        {
            return Line.GetHashCode() + Column.GetHashCode();
        }
    }

    /// <summary> Tracks a range in a file, for example the range of a keyword </summary> 
    public struct FileRange
    {
        /// <summary> The file this positions are in </summary>
        public readonly ParsedFile File;

        /// <summary> The start position </summary>
        public readonly Position Start;

        /// <summary> The end position </summary>
        public readonly Position End;

        /// <summary> Creates a new range </summary>
        /// <param name="start">The start position</param>
        /// <param name="end">The end position</param>
        /// <exception cref="ArgumentException">If the two positions are not in the same file, or if the start position is after the end position.</exception>
        public FileRange(Position start, Position end)
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

        /// <summary> Summarises this range into a string for human readability </summary>
        public override string ToString()
        {
            return $"{Start} to {End}";
        }
    }

    /// <summary> Tracks a range of a key in a keyvalue element in a file, |Key| :&gt; &lt;:| </summary> 
    public struct KeyRange
    {
        /// <summary> The file of the range </summary>
        public readonly ParsedFile File;

        /// <summary> The start of the range, |Key :&gt; &lt;: </summary>
        public readonly Position Start;

        /// <summary> The end of the key, Key| :&gt; &lt;: </summary>
        public readonly Position NameEnd;

        /// <summary> The end of the field, Key :&gt; &lt;:| </summary>
        public readonly Position FieldEnd;

        /// <summary> The full range, from <see cref="Start"/> to <see cref="FieldEnd"/>, |Key :&gt; &lt;:| </summary>
        public FileRange Full
        {
            get
            {
                return new FileRange(Start, FieldEnd);
            }
        }

        /// <summary> The range of the name, from <see cref="Start"/> to <see cref="NameEnd"/>, |Key| :&gt; &lt;: </summary>
        public FileRange Name
        {
            get
            {
                return new FileRange(Start, NameEnd);
            }
        }

        /// <summary> Creates a KeyRange </summary>
        /// <param name="name">Name range, from <see cref="Start"/> to <see cref="NameEnd"/></param>
        /// <param name="fieldEnd">FieldEnd position, <see cref="FieldEnd"/></param>
        /// <exception member="ArgumentException">If the positions are not in the same file or if positions are not in the right order.</exception>
        public KeyRange(FileRange name, Position fieldEnd)
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

        /// <summary> Summarises this range into a string for human readability </summary>
        public override string ToString()
        {
            return $"{Start} to {NameEnd} to {FieldEnd}";
        }
    }

    /// <summary> Saves a file to use with positions </summary> 
    public class ParsedFile
    {
        [JsonIgnore]
        /// <summary> The content of this file, as an array of all lines </summary>
        public readonly string[] Lines;

        [JsonIgnore]
        public readonly Read.FileIdentifier Identifier;

        /// <summary> Creates a new ParsedFile </summary>
        /// <param name="path">The filename (will be resolved to full path)</param>
        /// <param name="content">The file content, as an array of all lines</param>
        /// <param name="name">The given name to the file so it is easier for users to track where the file comes from.</param>
        /// <param name="content">The original definition of this file, if given in the batchfile or derivatives.</param>
        public ParsedFile(string path, string[] content, string name, InputNameSpace.KeyValue origin)
        {
            Identifier = new Read.FileIdentifier(path, name, origin);
            Lines = content;
        }

        /// <summary> Creates a new ParsedFile </summary>
        /// <param name="file"> The identifier for the file. </param>
        /// <param name="content"> The file content, as an array of all lines. </param>
        public ParsedFile(Read.FileIdentifier file, string[] content)
        {
            Identifier = file;
            Lines = content;
        }

        /// <summary> Creates an empty ParsedFile </summary>
        public ParsedFile()
        {
            Identifier = new Read.FileIdentifier();
            Lines = new string[0];
        }

        public override bool Equals(object obj)
        {
            if (obj is ParsedFile that)
            {
                return this.Identifier.Equals(that.Identifier);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return 23 + 397 * Identifier.GetHashCode();
        }

        public override string ToString()
        {
            return Identifier.ToString();
        }
    }
}
