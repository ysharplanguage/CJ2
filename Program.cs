/* Copyright(c) 2017 Cyril Jandia

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

Inquiries : ysharp { dot}
design { at}
gmail { dot}
com */
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
    public class CnfLexicon
    {
        protected CnfProduction[] Lexicals { get; private set; }

        protected HashSet<string> Literals { get; private set; }

        public CnfLexicon(CnfProduction[] lexicals)
        {
            Literals =
                new HashSet<string>
                (
                    (Lexicals = lexicals).
                    Where(lexical => lexical.IsLiteral).
                    Select(lexical => lexical.Literal)
                );
        }

        public Tuple<string, string[]> Match(string input)
        {
            var value = null as string;
            var match = null as string;
            var found =
                Lexicals.
                Where
                (
                    lexical =>
                    {
                        if (((match = lexical.Match(input)) != null) && (match.Length > 0))
                        {
                            if (lexical.IsLiteral || !Literals.Contains(match))
                            {
                                value = value ?? match;
                                return match == value;
                            }
                        }
                        return false;
                    }
                ).
                Select(lexical => lexical.Lhs).
                ToArray();
            return Tuple.Create(value, found);
        }
    }

    public class CnfProduction : Tuple<string, string>
    {
        protected Regex Acceptor { get; private set; }

        public CnfProduction(string lhs, string rhs, int index)
            : this(lhs, rhs, null, index)
        {
        }

        public CnfProduction(string lhs, string rhs1, string rhs2, int index)
            : base(lhs, rhs2 != null ? string.Concat(rhs1, ' ', rhs2) : rhs1)
        {
            Index = index;
            Rhs1 = rhs1;
            Rhs2 = rhs2;
            if (IsLexical)
            {
                if ((Rhs.Length > 3) && Rhs.StartsWith("@\"") && (Rhs[Rhs.Length - 1] == '"'))
                {
                    Pattern = Rhs.Substring(2, Rhs.Length - 3);
                }
                else if ((Rhs.Length > 2) && (Rhs[0] == '"') && (Rhs[Rhs.Length - 1] == '"'))
                {
                    Literal = Rhs.Substring(1, Rhs.Length - 2);
                }
                else
                {
                    Literal = Rhs;
                }
                if (Pattern != null)
                {
                    Acceptor = new Regex(string.Format("^({0})", Pattern), RegexOptions.Compiled);
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

        public string Match(string input)
        {
            Match match;
            return
                !string.IsNullOrEmpty(input) ?
                (
                    !IsLiteral ?
                    (match = Acceptor.Match(input)).Success ? match.Value : null
                    :
                    (input.StartsWith(Literal) ? Literal : null)
                )
                :
                null;
        }

        public string Lhs { get { return Item1; } }

        public string Rhs { get { return Item2; } }

        public int Index { get; private set; }

        public string Rhs1 { get; private set; }

        public string Rhs2 { get; private set; }

        public bool IsLiteral { get { return Literal != null; } }

        public bool IsLexical { get { return Rhs2 == null; } }

        public string Pattern { get; private set; }

        public string Literal { get; private set; }
    }

    public class CnfProductions : HashSet<string>
    {
        protected CnfGrammar Grammar { get; private set; }

        public CnfProductions(CnfGrammar grammar, params string[] elements)
            : base(elements)
        {
            Grammar = grammar;
        }

        public override string ToString()
        {
            return string.Concat('{', string.Join(", ", this.OrderBy(lhs => lhs)), '}');
        }
    }

    public class CnfGrammar
    {
        protected virtual Regex GetInsignificantWhiteSpace()
        {
            return DefaultWhiteSpace;
        }

        protected virtual string ReductionKey(string from, string to)
        {
            return string.Concat(from, '>', to);
        }

        public readonly Regex DefaultWhiteSpace = new Regex("^(\\s+)", RegexOptions.Compiled);

        public readonly CnfProductions Error;

        public readonly Regex WhiteSpace;

        public CnfGrammar()
        {
            Productions = new Dictionary<string, CnfProduction>();
            Error = new CnfProductions(this, "Error");
            WhiteSpace = GetInsignificantWhiteSpace();
        }

        public virtual CnfGrammar Load(string[] specs)
        {
            var splitter = new Regex("\\s+");
            var binaries = new Dictionary<string, IList<CnfProduction>>();
            Func<string, string> trim = s => s.Trim(new[] { '\t', ' ' });
            Func<string, string[]> split = s => splitter.Split(s);
            Productions.Clear();
            for (var at = 0; at < specs.Length; at++)
            {
                var spec = specs[at];
                string line;
                if (!(line = trim(spec)).StartsWith("#"))
                {
                    var separatorIndex = line.IndexOf("->");
                    bool arrow;
                    separatorIndex = (arrow = (separatorIndex > 0)) ? separatorIndex : line.IndexOf(":");
                    var sides =
                        separatorIndex > 0 ?
                        new[]
                        {
                            line.Substring(0, separatorIndex),
                            line.Substring(separatorIndex + (arrow ? 2 : 1))
                        }
                        :
                        null;
                    if (sides != null)
                    {
                        var lhs = trim(sides[0]);
                        var rhs = trim(sides[1]);
                        var commentMark = rhs.IndexOf('#');
                        rhs = trim(commentMark >= 0 ? rhs.Substring(0, commentMark) : rhs);
                        var normalizedRhs = split(rhs);
                        var isBinaryRhs = normalizedRhs.Length > 1;
                        var binLeft = normalizedRhs[0];
                        var binRight = isBinaryRhs ? normalizedRhs[1] : null;
                        var reduction = new CnfProduction(lhs, binLeft, binRight, at);
                        var binaryKey = isBinaryRhs ? string.Concat(binLeft, ' ', binRight) : null;
                        var reduceKey = ReductionKey(binaryKey ?? reduction.Rhs, lhs);
                        if (Productions.Count < 1)
                        {
                            Start = reduction;
                        }
                        Productions.Add(reduceKey, reduction);
                        if (binaryKey != null)
                        {
                            IList<CnfProduction> reductions;
                            if (!binaries.TryGetValue(binaryKey, out reductions))
                            {
                                binaries.Add(binaryKey, new List<CnfProduction>());
                            }
                            binaries[binaryKey].Add(reduction);
                        }
                    }
                }
            }
            Binaries = binaries.Values.SelectMany(productions => productions).ToArray();
            Lexicon =
                new CnfLexicon
                (
                    Productions.
                    Values.
                    Where(production => production.IsLexical).
                    OrderByDescending(production => !production.IsLiteral ? production.Pattern.Length : production.Literal.Length).
                    ToArray()
                );
            return this;
        }

        //TODO: implement
        public Tuple<CnfProduction, CnfProduction> GetKernel()
        {
            return null;
        }

        public CnfProductions Lookup(CnfProductions left, CnfProductions right)
        {
            var reductions =
                Binaries.
                Aggregate
                (
                    new CnfProductions(this),
                    (found, production)
                        =>
                        {
                            if (left.Contains(production.Rhs1) && right.Contains(production.Rhs2))
                            {
                                found.Add(production.Lhs);
                            }
                            return found;
                        }
                );
            return (reductions != null) && (reductions.Count > 0) ? reductions : null;
        }

        public IDictionary<string, CnfProduction> Productions { get; protected set; }

        public CnfProduction[] Binaries { get; protected set; }

        public CnfLexicon Lexicon { get; protected set; }

        public CnfProduction Start { get; protected set; }
    }
    #endregion

    public class CflParse : Tuple<CflParse, CflParse>
    {
        public CflParse(string whiteSpace)
            : this(null, whiteSpace)
        {
        }

        public CflParse(CnfProductions productions, string token)
            : this(productions, null, null)
        {
            Value = token;
        }

        public CflParse(CnfProductions productions, CflParse left, CflParse right)
            : base(left, right)
        {
            Productions = productions;
        }

        public override string ToString()
        {
            return
                Productions != null ?
                (
                    (Left != null) && (Right != null) ? string.Format("( {0} {1} {2} )", Productions, Left, Right)
                    :
                    string.Format("( {0} {1} )", Productions, Value)
                )
                :
                base.ToString();
        }

        public CnfProductions Productions { get; private set; }

        public CflParse Left { get { return Item1; } }

        public CflParse Right { get { return Item2; } }

        public string Value { get; private set; }

        public bool IsWhiteSpace { get { return Productions == null; } }
    }

    public abstract class CflParser
    {
        private CflParse Read(ref string input)
        {
            if (!string.IsNullOrEmpty(input))
            {
                var match = null as Tuple<string, string[]>;
                var space = null as Match;
                var sofar = input;
                var lexicals =
                    !(space = Grammar.WhiteSpace.Match(sofar)).Success ?
                    (match = Grammar.Lexicon.Match(sofar)).Item2
                    :
                    null;
                if (lexicals != null)
                {
                    if (lexicals.Length > 0)
                    {
                        var productions = new CnfProductions(Grammar, lexicals);
                        var value = match.Item1;
                        input = input.Substring(value.Length);
                        return new CflParse(productions, value);
                    }
                    else
                    {
                        input = null;
                        return Error(sofar);
                    }
                }
                else
                {
                    input = input.Substring(space.Length);
                    return WhiteSpace(space.Value);
                }
            }
            return null;
        }

        private CflParse Next(ref string input)
        {
            CflParse next;
            while (((next = Read(ref input)) != null) && next.IsWhiteSpace) ;
            return next;
        }

        protected CflParser(CnfGrammar grammar)
        {
            Grammar = grammar;
        }

        protected CflParse WhiteSpace(string value)
        {
            return new CflParse(value);
        }

        protected CflParse Error(string input)
        {
            return new CflParse(Grammar.Error, input);
        }

        public CflParse[] Tokenize(string input)
        {
            var tokens = new List<CflParse>();
            CflParse token;
            while ((token = Next(ref input)) != null)
            {
                tokens.Add(token);
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
                            var reductions = Grammar.Lookup(left.Productions, right.Productions);
                            if (reductions != null)
                            {
                                if (matrix[l - 1, s - 1] != null)
                                {
                                    matrix[l - 1, s - 1].Productions.UnionWith(reductions);
                                }
                                else
                                {
                                    matrix[l - 1, s - 1] = new CflParse(reductions, left, right);
                                }
                            }
                        }
                    }
                }
            }
            matrix[length - 1, 0] = matrix[length - 1, 0] ?? Error(input);
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
                    var reductions = Grammar.Lookup(left.Productions, right.Productions);
                    if (reductions != null)
                    {
                        var last = new CflParse(reductions, left, right);
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
            return dv.Count == 1 ? dv.Last.Value : Error(input);
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
                    var reductions = Grammar.Lookup(left.Productions, right.Productions);
                    if (reductions != null)
                    {
                        var first = new CflParse(reductions, left, right);
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
            return dv.Count == 1 ? dv.First.Value : Error(input);
        }

        public CJ2Parser(CnfGrammar grammar)
            : base(grammar)
        {
        }

        public override CflParse Parse(string input)
        {
            var tokens = Tokenize(input);
            var derivative = LeftDerivative(tokens, input);
            return
                !derivative.Productions.Contains(Grammar.Start.Lhs) ?
                RightDerivative(tokens, input)
                :
                derivative;
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
            var errors = 0;
            string input;
            while ((input = Console.ReadLine()) != null)
            {
                var sw = new Stopwatch();
                sw.Start();
                var parse = parser.Parse(input);
                sw.Stop();
                var ms = sw.ElapsedMilliseconds;
                var ok = parse.Productions.Contains(grammar.Start.Lhs);
                var details = verbose ? string.Format(": {0} => {1}", input, parse) : string.Empty;
                errors += !ok ? 1 : 0;
                Console.WriteLine(string.Format("(in {0} ms) line #{1}: {2}{3}", sw.ElapsedMilliseconds.ToString().PadLeft(7, ' '), (++lineNo).ToString().PadLeft(7, ' '), ok ? "OK" : "KO", details));
            }
            if (lineNo > 0)
            {
                Console.WriteLine(string.Format("{0} / {1} OK: {2}%", lineNo - errors, lineNo, 100 * (lineNo - errors) / lineNo));
            }
            Console.WriteLine();
            Environment.Exit(errors > 0 ? 1 : 0);
        }
    }
}