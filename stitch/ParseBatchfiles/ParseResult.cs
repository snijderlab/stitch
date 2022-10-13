using System;
using System.Collections.Generic;
using Stitch.InputNameSpace;

namespace Stitch
{

    /// <summary>To save a result of a parse action, the value or a error message. </summary>
    public class ParseResult<T>
    {
        public T Value;
        public List<ErrorMessage> Messages = new();
        public ParseResult(T t)
        {
            Value = t;
        }
        public ParseResult(ErrorMessage error)
        {
            Messages.Add(error);
        }
        public ParseResult(List<ErrorMessage> errors)
        {
            Messages.AddRange(errors);
        }
        public ParseResult() { }
        public bool HasFailed()
        {
            foreach (var msg in Messages)
            {
                if (!msg.Warning) return true;
            }
            return false;
        }
        public bool HasOnlyWarnings()
        {
            if (Messages.Count == 0) return false;
            foreach (var msg in Messages)
            {
                if (!msg.Warning) return false;
            }
            return true;
        }
        public T ReturnOrFail()
        {
            if (this.HasFailed())
            {
                PrintMessages();

                throw new ParseException("");
            }
            else
            {
                foreach (var msg in Messages)
                {
                    msg.Print();
                }

                return Value;
            }
        }
        public T ReturnOrDefault(T def)
        {
            if (this.HasFailed()) return def;
            else return this.Value;
        }
        public T GetValue<TOut>(ParseResult<TOut> fail)
        {
            fail.Messages.AddRange(Messages);
            return Value;
        }
        public bool TryGetValue(out T output)
        {
            output = this.Value;
            return !this.HasFailed();
        }
        public void AddMessage(ErrorMessage failMessage)
        {
            Messages.Add(failMessage);
        }

        public void PrintMessages()
        {
            if (this.HasFailed())
            {
                var defaultColour = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                var s = Messages.Count == 1 ? "" : "s";
                var were = Messages.Count == 1 ? "was" : "were";
                Console.WriteLine($"\nThere {were} {Messages.Count} error{s} while parsing.");
                Console.ForegroundColor = defaultColour;

                foreach (var msg in Messages)
                {
                    msg.Print();
                }
            }
        }
    }
}