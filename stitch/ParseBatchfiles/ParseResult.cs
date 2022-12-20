using System;
using System.Collections.Generic;
using Stitch.InputNameSpace;

namespace Stitch {
    /// <summary>To save a result of a parse action, the value or a error message. </summary>
    public struct ParseResult<T> {
        /// <summary> The inner value of this ParseResult, it is UB to use this value if `this.IsErr()`. </summary>
        public T Value = default(T);

        /// <summary> All contained error and warning messages. </summary>
        public readonly List<ErrorMessage> Messages = new();

        /// <summary> Create a ParseResult with the given value. </summary>
        public ParseResult(T t) {
            Value = t;
        }

        /// <summary> Create a ParseResult with the given error message. </summary>
        public ParseResult(ErrorMessage error) {
            Messages.Add(error);
        }

        /// <summary> Create a ParseResult with the given error messages. </summary>
        public ParseResult(List<ErrorMessage> errors) {
            Messages.AddRange(errors);
        }

        /// <summary> Create an empty ParseResult. </summary>
        public ParseResult() { }

        /// <summary> Check if this result is erroneous. It will ignore any warnings. </summary>
        public bool IsErr() {
            foreach (var msg in Messages) {
                if (!msg.Warning) return true;
            }
            return false;
        }

        /// <summary> See if this ParseResult is ok. It will ignore any warnings. </summary>
        public bool IsOk() {
            return !IsErr();
        }

        /// <summary> See if this ParseResult is ok and store its messages in the given Result. </summary>
        /// <param name="other"> Another ParseResult to store this ParseResults messages in. </param>
        /// <typeparam name="TOut"> Any Type. </typeparam>
        /// <returns> A bool to indicate if this Result is Ok. </returns>
        public bool IsOk<TOut>(ParseResult<TOut> other) {
            other.Messages.AddRange(this.Messages);
            foreach (var msg in Messages) {
                if (!msg.Warning) return false;
            }
            return true;
        }

        /// <summary> Unwrap the result, meaning return the result or raise an exception. It will print contained error messages. </summary>
        /// <exception cref="ParseException"> Raises an exception if `this.IsErr()`. </exception>
        /// <returns> The contained value. </returns>
        public T Unwrap() {
            if (this.IsErr()) {
                PrintMessages();

                throw new ParseException("");
            } else {
                foreach (var msg in Messages) {
                    msg.Print();
                }

                return Value;
            }
        }

        /// <summary> Get the contained value while adding all contained error messages to the given other result. </summary>
        /// <param name="fail"> The result to give all contained error messages. </param>
        /// <param name="def"> The default value to use if `this.IsErr()`. </param>
        /// <typeparam name="TOut"> Any type. </typeparam>
        /// <returns> The value or default. </returns>
        public T UnwrapOrDefault<TOut>(ParseResult<TOut> fail, T def) {
            fail.Messages.AddRange(Messages);
            if (this.IsErr()) return def;
            else return this.Value;
        }

        /// <summary> Try to get the contained value, if it returns false the value is not set properly. </summary>
        /// <param name="output"> The out parameter to save the value in. </param>
        /// <returns> A bool determining if the value is valid, it is UB to use the value if the result is false. </returns>
        public bool TryGetValue(out T output) {
            output = this.Value;
            return !this.IsErr();
        }

        /// <summary> Add an extra error message to this result. </summary>
        /// <param name="failMessage"> The message to add. </param>
        public void AddMessage(ErrorMessage failMessage) {
            Messages.Add(failMessage);
        }

        /// <summary> Nicely print all contained error messages. </summary>
        public void PrintMessages() {
            if (this.IsErr()) {
                var defaultColour = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                var s = Messages.Count == 1 ? "" : "s";
                var were = Messages.Count == 1 ? "was" : "were";
                Console.WriteLine($"\nThere {were} {Messages.Count} error{s} while parsing.");
                Console.ForegroundColor = defaultColour;

                foreach (var msg in Messages) {
                    msg.Print();
                }
            }
        }

        public ParseResult<O> Map<O>(Func<T, O> f) {
            var output = new ParseResult<O>();
            output.Messages.AddRange(Messages);
            if (this.IsOk())
                output.Value = f(this.Value);
            return output;
        }
    }
}