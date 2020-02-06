using System;

namespace AssemblyNameSpace
{
    /// <summary>
    /// To keep track of a location of a token.
    /// </summary>
    public struct Position
    {
        public readonly int Line;
        public readonly int Column;
        public readonly ParsedFile File;
        public Position(int l, int c, ParsedFile file)
        {
            Line = l;
            Column = c;
            File = file;
        }
        public override string ToString()
        {
            return $"({Line},{Column})";
        }
        public static bool operator ==(Position p1, Position p2)
        {
            return p1.Equals(p2);
        }
        public static bool operator !=(Position p1, Position p2)
        {
            return !(p1.Equals(p2));
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

    public struct Range
    {
        public readonly Position Start;
        public readonly Position End;
        public readonly ParsedFile File;
        public Range(Position start, Position end)
        {
            Start = start;
            End = end;
            File = start.File;
            if (start.File != end.File)
            {
                throw new ArgumentException("Two positions in two different files do not form a range in a single file.");
            }
        }
        public override string ToString()
        {
            return $"{Start} to {End}";
        }
    }

    public struct KeyRange
    {
        public readonly Position Start;
        public readonly Position NameEnd;
        public readonly Position FieldEnd;
        public readonly ParsedFile File;
        public Range Full
        {
            get
            {
                return new Range(Start, FieldEnd);
            }
        }
        public Range Name
        {
            get
            {
                return new Range(Start, NameEnd);
            }
        }
        public KeyRange(Position start, Position nend, Position fend)
        {
            Start = start;
            NameEnd = nend;
            FieldEnd = fend;
            File = start.File;
            if (start.File != nend.File || nend.File != fend.File)
            {
                throw new ArgumentException("Three positions in different files do not form a range in a single file.");
            }
        }
        public KeyRange(Range name, Position fend)
        {
            Start = name.Start;
            NameEnd = name.End;
            FieldEnd = fend;
            File = name.File;
            if (name.File != fend.File)
            {
                throw new ArgumentException("Two positions in two different files do not form a range in a single file.");
            }
        }
        public override string ToString()
        {
            return $"{Start} to {NameEnd} to {FieldEnd}";
        }
    }

    public class ParsedFile
    {
        public readonly string Filename;
        public readonly string[] Lines;
        public ParsedFile(string name, string[] content)
        {
            Filename = name;
            Lines = content;
        }
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
            return Filename.GetHashCode();
        }
    }
}