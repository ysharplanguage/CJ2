/* Copyright (c) 2017 Cyril Jandia

http://www.cjandia.com/

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
``Software''), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be included
in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED ``AS IS'', WITHOUT WARRANTY OF ANY KIND, EXPRESSED
OR IMPLIED, INCLUDING, BUT NOT LIMITED TO, THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
IN NO EVENT SHALL CYRIL JANDIA BE LIABLE FOR ANY CLAIM, DAMAGES OR
OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
OTHER DEALINGS IN THE SOFTWARE.

Except as contained in this notice, the name of Cyril Jandia shall
not be used in advertising or otherwise to promote the sale, use or
other dealings in this Software without prior written authorization
from Cyril Jandia.

Inquiries : ysharp {dot} design {at} gmail {dot} com */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Common
{
    #region Context-free Language Parsing (interface)

    #region Chomsky Normal Form (interface)
    public class CnfProduction : Tuple<string, string>
    {
        protected string Literal { get; private set; }

        protected Regex Pattern { get; private set; }

        public CnfProduction(string lhs, string rhs, int index, bool isLexical)
            : base(lhs, rhs)
        {
            Index = index;
            if (IsLexical = isLexical)
            {
                if ((rhs.Length > 3) && rhs.StartsWith("@\"") && (rhs[rhs.Length - 1] == '"'))
                {
                    Pattern = new Regex(string.Format("^({0})", rhs.Substring(2, rhs.Length - 3)), RegexOptions.Compiled);
                }
                else if ((rhs.Length > 2) && (rhs[0] == '"') && (rhs[rhs.Length - 1] == '"'))
                {
                    Literal = rhs.Substring(1, rhs.Length - 2);
                }
                else
                {
                    Literal = rhs;
                }
            }
        }

        public override int GetHashCode()
        {
            return Lhs.GetHashCode() ^ Rhs.GetHashCode();
        }

        public override string ToString()
        {
            return string.Concat(Lhs, " -> ", Rhs);
        }

        public string Lexical(string input)
        {
            Match match;
            var token =
                IsLexical ?
                (
                    Pattern != null ?
                    (
                        (match = Pattern.Match(input)).Success ?
                        match.Value
                        :
                        string.Empty
                    )
                    :
                    (input.StartsWith(Literal) ? Literal : string.Empty)
                )
                :
                string.Empty;
            return token;
        }

        public string Lhs { get { return Item1; } }

        public string Rhs { get { return Item2; } }

        public int Index { get; private set; }

        public bool IsLexical { get; private set; }
    }

    public class CnfGrammar
    {
        protected virtual Regex DefineInsignificantWhiteSpace()
        {
            return DefaultWhiteSpace;
        }

        protected virtual string ReductionKey(string from, string to)
        {
            return string.Concat(from, '>', to);
        }

        protected IDictionary<string, IList<CnfProduction>> Binaries { get; private set; }

        public readonly Regex DefaultWhiteSpace = new Regex("\\s+", RegexOptions.Compiled);

        public readonly Regex WhiteSpace;

        public CnfGrammar()
        {
            Productions = new Dictionary<string, CnfProduction>();
            Binaries = new Dictionary<string, IList<CnfProduction>>();
            Error = new CnfProduction("Error", string.Empty, -1, true);
            WhiteSpace = DefineInsignificantWhiteSpace();
        }

        public virtual CnfGrammar Load(string[] specs)
        {
            Func<string, string> trim = s => s.Trim(new[] { '\t', ' ' });
            Productions.Clear();
            Binaries.Clear();
            for (var at = 0; at < specs.Length; at++)
            {
                var spec = specs[at];
                string line;
                if (!(line = trim(spec)).StartsWith("#"))
                {
                    var sides = line.Split(new[] { "->" }, StringSplitOptions.RemoveEmptyEntries);
                    if (sides.Length == 2)
                    {
                        var lhs = trim(sides[0]);
                        var rhs = sides[1];
                        var commentMark = rhs.IndexOf('#');
                        rhs = trim(commentMark >= 0 ? rhs.Substring(0, commentMark) : rhs);
                        var normalizedRhs = DefaultWhiteSpace.Split(rhs);
                        var isBinaryRhs = normalizedRhs.Length > 1;
                        var binLeft = normalizedRhs[0];
                        var binRight = isBinaryRhs ? normalizedRhs[1] : null;
                        rhs = isBinaryRhs ? string.Concat(binLeft, ' ', binRight) : binLeft;
                        var reduction = new CnfProduction(lhs, rhs, at, !isBinaryRhs);
                        var binaryKey = isBinaryRhs ? ReductionKey(binLeft, binRight) : null;
                        var reduceKey = isBinaryRhs ? ReductionKey(binaryKey, lhs) : ReductionKey(rhs, lhs);
                        if (Productions.Count < 1)
                        {
                            Start = reduction;
                        }
                        Productions.Add(reduceKey, reduction);
                        if (isBinaryRhs)
                        {
                            IList<CnfProduction> reductions;
                            if (!Binaries.TryGetValue(binaryKey, out reductions))
                            {
                                Binaries.Add(binaryKey, new List<CnfProduction>());
                            }
                            if (reduction.Lhs != Start.Lhs)
                            {
                                Binaries[binaryKey].Insert(0, reduction);
                            }
                            else
                            {
                                Binaries[binaryKey].Add(reduction);
                            }
                        }
                    }
                }
            }
            Lexicon =
                Productions.
                Values.
                Where(production => production.IsLexical).
                OrderBy(production => production.Index).
                ToArray();
            return this;
        }

        public IList<CnfProduction> Lookup(CnfProduction left, CnfProduction right, bool reduceStart)
        {
            IList<CnfProduction> reductions;
            Binaries.TryGetValue(ReductionKey(left.Lhs, right.Lhs), out reductions);
            return
                reduceStart ?
                (
                    reductions != null ?
                    (
                        reductions[reductions.Count - 1].Lhs == Start.Lhs ?
                        new[] { reductions[reductions.Count - 1] }
                        :
                        null
                    )
                    :
                    reductions
                )
                :
                reductions;
        }

        public IDictionary<string, CnfProduction> Productions { get; protected set; }

        public CnfProduction[] Lexicon { get; protected set; }

        public CnfProduction Start { get; protected set; }

        public CnfProduction Error { get; protected set; }
    }
    #endregion

    public class CflParse : Tuple<CflParse, CflParse>
    {
        private CflParse(string spaces)
            : this(null, spaces)
        {
        }

        public static CflParse WhiteSpace(string spaces)
        {
            return new CflParse(spaces);
        }

        public CflParse(CnfProduction production, string token)
            : this(production, null, null)
        {
            Value = token;
        }

        public CflParse(CnfProduction production, CflParse left, CflParse right)
            : base(left, right)
        {
            Production = production;
        }

        public override string ToString()
        {
            return
                Production != null ?
                (
                    (Left != null) && (Right != null) ? string.Format("( {0} {1} {2} )", Production.Lhs, Left, Right)
                    :
                    string.Format("( {0} {1} )", Production.Lhs, Value)
                )
                :
                base.ToString();
        }

        public CnfProduction Production { get; private set; }

        public CflParse Left { get { return Item1; } }

        public CflParse Right { get { return Item2; } }

        public string Value { get; private set; }

        public bool IsWhiteSpace { get { return Production == null; } }
    }

    public abstract class CflParser
    {
        private CflParse Next(ref string input)
        {
            if (!string.IsNullOrEmpty(input))
            {
                var value = string.Empty;
                var sofar = input;
                var lexical = Grammar.Lexicon.FirstOrDefault(current => (value = current.Lexical(sofar)).Length > 0);
                var match = lexical != null;
                if (!match)
                {
                    var spaces = Grammar.WhiteSpace.Match(input);
                    if (spaces.Success)
                    {
                        input = input.Substring(spaces.Value.Length);
                        return CflParse.WhiteSpace(spaces.Value);
                    }
                }
                value = match ? value : input;
                input = match ? input.Substring(value.Length) : string.Empty;
                return new CflParse(lexical ?? Grammar.Error, value);
            }
            return null;
        }

        protected CflParser(CnfGrammar grammar)
        {
            Grammar = grammar;
        }

        public CflParse[] Tokenize(string input)
        {
            var tokens = new List<CflParse>();
            CflParse token;
            while ((token = Next(ref input)) != null)
            {
                if (!token.IsWhiteSpace)
                {
                    tokens.Add(token);
                }
            }
            return tokens.ToArray();
        }

        public abstract CflParse Parse(string input);

        public CnfGrammar Grammar { get; private set; }
    }
    #endregion
}

namespace CFL
{
    using Common;

    // Cocke-Younger-Kasami algorithm ( https://en.wikipedia.org/wiki/CYK_algorithm )
    public class CYKParser : CflParser
    {
        public CYKParser(CnfGrammar grammar)
            : base(grammar)
        {
        }

        public override CflParse Parse(string input)
        {
            var tokens = Tokenize(input);
            var length = tokens.Length;
            var matrix = new CflParse[length, length];
            for (var s = 1; s <= length; s++)
            {
                matrix[0, s - 1] = tokens[s - 1];
            }
            for (var l = 2; l <= length; l++)
            {
                for (var s = 1; s <= length - l + 1; s++)
                {
                    for (var p = 1; p <= l - 1; p++)
                    {
                        var left = matrix[p - 1, s - 1];
                        var right = matrix[l - p - 1, s + p - 1];
                        if ((left != null) && (right != null))
                        {
                            var reduceStart = (l == length) && (s == 1);
                            var reductions = Grammar.Lookup(left.Production, right.Production, reduceStart);
                            if (reductions != null)
                            {
                                var production = reductions[0];
                                matrix[l - 1, s - 1] = new CflParse(production, left, right);
                            }
                        }
                    }
                }
            }
            matrix[length - 1, 0] = matrix[length - 1, 0] ?? new CflParse(Grammar.Error, input);
            return matrix[length - 1, 0];
        }
    }

    // CJ2 algorithm ( http://lambda-the-ultimate.org/node/5414 )
    public class CJ2Parser : CflParser
    {
        private CflParse LeftDerivative(CflParse[] tokens, string input)
        {
            var length = tokens.Length;
            var dv = new LinkedList<CflParse>();
            var at = -1;
            while (at < length)
            {
                while (dv.Count >= 2)
                {
                    var left = dv.Last.Previous.Value;
                    var right = dv.Last.Value;
                    var reduceStart = (dv.Count == 2) && (at == length - 1);
                    var reductions = Grammar.Lookup(left.Production, right.Production, reduceStart);
                    if (reductions != null)
                    {
                        var production = reductions[0];
                        var last = new CflParse(production, left, right);
                        dv.RemoveLast();
                        dv.RemoveLast();
                        dv.AddLast(last);
                    }
                    else
                    {
                        break;
                    }
                }
                at++;
                if (at < length)
                {
                    dv.AddLast(tokens[at]);
                }
            }
            return dv.Count == 1 ? dv.Last.Value : new CflParse(Grammar.Error, input);
        }

        private CflParse RightDerivative(CflParse[] tokens, string input)
        {
            var length = tokens.Length;
            var dv = new LinkedList<CflParse>();
            var at = length;
            while (at >= 0)
            {
                while (dv.Count >= 2)
                {
                    // Rewriting via R2
                    // Lc Rc ~> Lc (U head(Rc) head(tail(Rc))) tail(tail(Rc))
                    var left = dv.First.Value;
                    var right = dv.First.Next.Value;
                    var reduceStart = (dv.Count == 2) && (at == 0);
                    var reductions = Grammar.Lookup(left.Production, right.Production, reduceStart);
                    if (reductions != null)
                    {
                        var production = reductions[0];
                        var first = new CflParse(production, left, right);
                        dv.RemoveFirst();
                        dv.RemoveFirst();
                        dv.AddFirst(first);
                    }
                    else
                    {
                        break;
                    }
                }
                at--;
                if (at >= 0)
                {
                    // Rewriting via R1
                    // Lc Rc ~> (Lc \ w) (U w) Rc
                    dv.AddFirst(tokens[at]);
                }
            }
            return dv.Count == 1 ? dv.First.Value : new CflParse(Grammar.Error, input);
        }

        public CJ2Parser(CnfGrammar grammar)
            : base(grammar)
        {
        }

        public override CflParse Parse(string input)
        {
            CflParse derivative;
            var tokens = Tokenize(input);
            derivative = LeftDerivative(tokens, input);
            return derivative = derivative.Production.Lhs != Grammar.Start.Lhs ? RightDerivative(tokens, input) : derivative;
        }
    }

    class Program
    {
        private static CflParser CreateParser(string parserName, CnfGrammar grammar)
        {
            var parserTypeName = string.Format("{0}.{1}Parser", typeof(Program).Namespace, parserName.ToUpper());
            var parserType = Type.GetType(parserTypeName);
            return (CflParser)Activator.CreateInstance(parserType, grammar);
        }

        static void Main(string[] args)
        {
            if ((args == null) || (args.Length < 2) || string.IsNullOrEmpty(args[0]) || string.IsNullOrEmpty(args[1]))
            {
                Console.WriteLine();
                Console.WriteLine("Use: cfl {algorithm} {grammar} [ --verbose ] [ < {input} [ > {output} ] ]");
                Console.WriteLine();
                Console.WriteLine("(where   {algorithm} = \"CYK\" or \"CJ2\")");
                Console.WriteLine();
                return;
            }

            var grammarFilePath = args[1];
            var grammarDefinition = File.ReadAllLines(grammarFilePath);
            var grammar = new CnfGrammar().Load(grammarDefinition);
            var verbose = (args.Length > 2) && (args[2] == "--verbose");

            var parser = CreateParser(args[0], grammar);

            var lineNo = 0;
            string input;
            while ((input = Console.ReadLine()) != null)
            {
                var sw = new Stopwatch();
                sw.Start();
                var parse = parser.Parse(input);
                sw.Stop();
                var ms = sw.ElapsedMilliseconds;
                var ok = parse.Production.Lhs == grammar.Start.Lhs;
                var details = verbose ? string.Format(": {0} => {1}", input, parse) : string.Empty;
                Console.WriteLine(string.Format("(in {0} ms) line #{1}: {2}{3}", sw.ElapsedMilliseconds.ToString().PadLeft(7, ' '), (++lineNo).ToString().PadLeft(7, ' '), ok ? "OK" : "KO", details));
            }
            Console.WriteLine();
        }
    }
}