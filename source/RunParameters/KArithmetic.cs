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
    namespace RunParameters
    {
        /// <summary>
        /// A value possibly based on the value of K
        /// </summary>
        public class KArithmetic
        {
            /// <summary>
            /// Shows a maximal bracketed version of the expression
            /// </summary>
            /// <returns>The expression</returns>
            public string Value { get { return expression.Show(); } }
            /// <summary>
            /// The expression
            /// </summary>
            Arithmetic.Expression expression;
            /// <summary>
            /// To retrieve the value of the expression, given this value of K
            /// </summary>
            /// <param name="k">The value of K</param>
            /// <returns>The value of the expression</returns>
            public int GetValue(int k)
            {
                return expression.Solve(k);
            }
            /// <summary>
            /// To generate a KArithmetic, parses the given string immediately to be sure to be error free if it succeeds.
            /// </summary>
            /// <param name="value">The expression</param>
            public KArithmetic(string value)
            {
                expression = Parse(value);
            }
            /// <summary>
            /// To contain Arithmetic stuffs
            /// </summary>
            class Arithmetic
            {
                /// <summary>
                /// A general class for expressions
                /// </summary>
                public abstract class Expression
                {
                    /// <summary>
                    /// Solves the expression
                    /// </summary>
                    /// <param name="k">The value of K</param>
                    /// <returns>The value of the expression</returns>
                    public abstract int Solve(int k);
                    /// <summary>
                    /// Creates a maximal bracketed version of the expression
                    /// </summary>
                    /// <returns>This expression in string form</returns>
                    public abstract string Show();
                }
                /// <summary>
                /// An expression with an operator
                /// </summary>
                public class Operator : Expression
                {
                    /// <summary>
                    /// The operator
                    /// </summary>
                    OpType type;
                    /// <summary>
                    /// The left hand side
                    /// </summary>
                    Expression left;
                    /// <summary>
                    /// The right hand side
                    /// </summary>
                    Expression right;
                    /// <summary>
                    /// Creates an operator expression
                    /// </summary>
                    /// <param name="type_input">The operator</param>
                    /// <param name="lhs">The left hand side</param>
                    /// <param name="rhs">The right hand side</param>
                    public Operator(OpType type_input, Expression lhs, Expression rhs)
                    {
                        type = type_input;
                        left = lhs;
                        right = rhs;
                    }
                    /// <summary>
                    /// Solves the expression
                    /// </summary>
                    /// <param name="k">The value of K</param>
                    /// <returns>The value of the expression</returns>
                    public override int Solve(int k)
                    {
                        switch (type)
                        {
                            case OpType.Minus:
                                return left.Solve(k) - right.Solve(k);
                            case OpType.Add:
                                return left.Solve(k) + right.Solve(k);
                            case OpType.Times:
                                return left.Solve(k) * right.Solve(k);
                            case OpType.Divide:
                                return left.Solve(k) / right.Solve(k);
                            default:
                                throw new ParseException($"An unkown operator type is signalled while solving the calculation {type.ToString()}");
                        }
                    }
                    /// <summary>
                    /// Creates a maximal bracketed version of the expression
                    /// </summary>
                    /// <returns>This expression in string form</returns>
                    public override string Show()
                    {
                        string op = "";
                        switch (type)
                        {
                            case OpType.Minus:
                                op = "-";
                                break;
                            case OpType.Add:
                                op = "+";
                                break;
                            case OpType.Times:
                                op = "*";
                                break;
                            case OpType.Divide:
                                op = "/";
                                break;
                            default:
                                throw new ParseException($"An unkown operator type is signalled while solving the calculation {type.ToString()}");
                        }
                        return "(" + left.Show() + op + right.Show() + ")";
                    }
                }
                /// <summary>
                /// The possible operators
                /// </summary>
                public enum OpType
                {
                    /// <summary> - </summary>
                    Minus,
                    /// <summary> + </summary>
                    Add,
                    /// <summary> * </summary>
                    Times,
                    /// <summary> / </summary>
                    Divide
                }
                /// <summary>
                /// An expression containing only the variable K
                /// </summary>
                public class K : Expression
                {
                    /// <summary>
                    /// Solves the expression
                    /// </summary>
                    /// <param name="k">The value of K</param>
                    /// <returns>The value of the expression</returns>
                    public override int Solve(int k)
                    {
                        return k;
                    }
                    /// <summary>
                    /// Creates a new instance
                    /// </summary>
                    public K() { }
                    /// <summary>
                    /// Creates a maximal bracketed version of the expression
                    /// </summary>
                    /// <returns>This expression in string form</returns>
                    public override string Show()
                    {
                        return "K";
                    }
                }
                /// <summary>
                /// An expression containing a constant
                /// </summary>
                public class Constant : Expression
                {
                    /// <summary>
                    /// The constant
                    /// </summary>
                    public int Value;
                    /// <summary>
                    /// Solves the expression
                    /// </summary>
                    /// <param name="k">The value of K</param>
                    /// <returns>The value of the expression</returns>
                    public override int Solve(int k)
                    {
                        return Value;
                    }
                    /// <summary>
                    /// Creates a new instance
                    /// </summary>
                    /// <param name="value">The value of the constant</param>
                    public Constant(int value)
                    {
                        Value = value;
                    }
                    /// <summary>
                    /// Creates a maximal bracketed version of the expression
                    /// </summary>
                    /// <returns>This expression in string form</returns>
                    public override string Show()
                    {
                        return Value.ToString();
                    }
                }
            }
            /// <summary>
            /// Parses a string into an expression
            /// </summary>
            /// <param name="input">The string to parse</param>
            /// <returns>The expression (if successfull)</returns>
            Arithmetic.Expression Parse(string input)
            {
                input = input.Trim();
                // Scan for low level operators
                if (input.Contains('+'))
                {
                    int pos = input.IndexOf('+');
                    return new Arithmetic.Operator(Arithmetic.OpType.Add, Parse(input.Substring(0, pos)), Parse(input.Substring(pos + 1)));
                }
                if (input.Contains('-'))
                {
                    int pos = input.IndexOf('-');
                    return new Arithmetic.Operator(Arithmetic.OpType.Minus, Parse(input.Substring(0, pos)), Parse(input.Substring(pos + 1)));
                }
                // Scan for high level operators
                if (input.Contains('*'))
                {
                    int pos = input.IndexOf('*');
                    return new Arithmetic.Operator(Arithmetic.OpType.Times, Parse(input.Substring(0, pos)), Parse(input.Substring(pos + 1)));
                }
                if (input.Contains('/'))
                {
                    int pos = input.IndexOf('/');
                    return new Arithmetic.Operator(Arithmetic.OpType.Divide, Parse(input.Substring(0, pos)), Parse(input.Substring(pos + 1)));
                }
                // Scan for constants and K's
                if (input.ToLower() == "k")
                {
                    return new Arithmetic.K();
                }
                try
                {
                    return new Arithmetic.Constant(Convert.ToInt32(input));
                }
                catch
                {
                    throw new ParseException($"The following could not be parsed into a calculation based on K: {input}");
                }
            }
        }
    }
}