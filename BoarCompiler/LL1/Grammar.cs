using System.Collections.Immutable;
using System.Text;

namespace BoarCompiler.LL1;

public sealed record ProductionRule(int Index, string LeftHandSide, IReadOnlyList<string> RightHandSide);

/// <summary>
/// Loads and stores a context-free grammar expressed with BNF-style productions.
/// </summary>
public sealed class Grammar
{
    public const string Epsilon = "ε";

    private readonly Dictionary<string, List<ProductionRule>> _rulesByNonTerminal;
    private readonly HashSet<string> _nonTerminals;
    private readonly HashSet<string> _terminals;

    private Grammar(
        string startSymbol,
        Dictionary<string, List<ProductionRule>> rulesByNonTerminal,
        HashSet<string> nonTerminals,
        HashSet<string> terminals,
        IReadOnlyList<ProductionRule> allRules)
    {
        StartSymbol = startSymbol;
        _rulesByNonTerminal = rulesByNonTerminal;
        _nonTerminals = nonTerminals;
        _terminals = terminals;
        Productions = allRules;
    }

    public string StartSymbol { get; }
    public IReadOnlyList<ProductionRule> Productions { get; }

    public IReadOnlyCollection<string> NonTerminals => _nonTerminals;
    public IReadOnlyCollection<string> Terminals => _terminals;

    public static Grammar LoadFromFile(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Grammar file not found.", path);
        }

        var lines = File.ReadAllLines(path);
        var rulesByNonTerminal = new Dictionary<string, List<ProductionRule>>(StringComparer.Ordinal);
        var declaredNonTerminals = new HashSet<string>(StringComparer.Ordinal);
        var referencedNonTerminals = new HashSet<string>(StringComparer.Ordinal);
        var allRules = new List<ProductionRule>();
        string? startSymbol = null;

        foreach (var rawLine in lines)
        {
            var trimmed = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                continue;
            }

            if (trimmed.StartsWith("//", StringComparison.Ordinal) ||
                trimmed.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            var separatorIndex = trimmed.IndexOf("::=", StringComparison.Ordinal);
            if (separatorIndex < 0)
            {
                throw new InvalidOperationException($"Invalid production (missing ::=): {trimmed}");
            }

            var lhs = trimmed[..separatorIndex].Trim();
            var rhs = trimmed[(separatorIndex + 3)..].Trim();

            var lhsSymbol = NormalizeNonTerminal(lhs);
            declaredNonTerminals.Add(lhsSymbol);
            startSymbol ??= lhsSymbol;

            foreach (var alternative in SplitAlternatives(rhs))
            {
                var symbols = TokenizeSymbols(alternative, referencedNonTerminals);
                if (symbols.Count == 0)
                {
                    symbols.Add(Epsilon);
                }

                var rule = new ProductionRule(allRules.Count + 1, lhsSymbol, symbols.ToImmutableArray());
                allRules.Add(rule);

                if (!rulesByNonTerminal.TryGetValue(lhsSymbol, out var list))
                {
                    list = new List<ProductionRule>();
                    rulesByNonTerminal[lhsSymbol] = list;
                }

                list.Add(rule);
            }
        }

        if (startSymbol is null)
        {
            throw new InvalidOperationException("Grammar file does not contain any productions.");
        }

        var nonTerminals = new HashSet<string>(declaredNonTerminals, StringComparer.Ordinal);
        nonTerminals.UnionWith(referencedNonTerminals);

        var terminals = new HashSet<string>(StringComparer.Ordinal);
        foreach (var rule in allRules)
        {
            foreach (var symbol in rule.RightHandSide)
            {
                if (symbol == Epsilon)
                {
                    continue;
                }

                if (nonTerminals.Contains(symbol))
                {
                    continue;
                }

                terminals.Add(symbol);
            }
        }

        return new Grammar(
            startSymbol,
            rulesByNonTerminal,
            nonTerminals,
            terminals,
            allRules);
    }

    public IReadOnlyList<ProductionRule> GetProductions(string nonTerminal)
    {
        if (!_rulesByNonTerminal.TryGetValue(nonTerminal, out var list))
        {
            return Array.Empty<ProductionRule>();
        }

        return list;
    }

    public bool IsNonTerminal(string symbol) => _nonTerminals.Contains(symbol);

    public bool IsTerminal(string symbol) => _terminals.Contains(symbol);

    private static string NormalizeNonTerminal(string symbol)
    {
        var trimmed = symbol.Trim();
        if (trimmed.StartsWith('<') && trimmed.EndsWith('>'))
        {
            return trimmed[1..^1].Trim();
        }

        return trimmed;
    }

    private static IEnumerable<string> SplitAlternatives(string rhs)
    {
        var builder = new StringBuilder();
        var insideQuote = false;

        for (var i = 0; i < rhs.Length; i++)
        {
            var ch = rhs[i];
            if (ch == '"')
            {
                insideQuote = !insideQuote;
                builder.Append(ch);
                continue;
            }

            if (ch == '|' && !insideQuote)
            {
                yield return builder.ToString().Trim();
                builder.Clear();
                continue;
            }

            builder.Append(ch);
        }

        if (builder.Length > 0)
        {
            yield return builder.ToString().Trim();
        }
    }

    private static List<string> TokenizeSymbols(string production, HashSet<string> referencedNonTerminals)
    {
        var tokens = new List<string>();
        var span = production.AsSpan();
        var index = 0;

        while (index < span.Length)
        {
            var ch = span[index];
            if (char.IsWhiteSpace(ch))
            {
                index++;
                continue;
            }

            if (ch == '"')
            {
                index++;
                var literal = new StringBuilder();
                while (index < span.Length && span[index] != '"')
                {
                    literal.Append(span[index]);
                    index++;
                }

                if (index >= span.Length)
                {
                    throw new InvalidOperationException($"Unterminated literal in production: {production}");
                }

                index++; // Consume closing quote
                tokens.Add(literal.ToString());
                continue;
            }

            if (ch == '<')
            {
                index++;
                var nonTerminal = new StringBuilder();
                while (index < span.Length && span[index] != '>')
                {
                    nonTerminal.Append(span[index]);
                    index++;
                }

                if (index >= span.Length)
                {
                    throw new InvalidOperationException($"Unterminated non-terminal in production: {production}");
                }

                index++; // Consume '>'
                var name = nonTerminal.ToString().Trim();
                tokens.Add(name);
                referencedNonTerminals.Add(name);
                continue;
            }

            var symbol = new StringBuilder();
            while (index < span.Length && !char.IsWhiteSpace(span[index]))
            {
                symbol.Append(span[index]);
                index++;
            }

            tokens.Add(symbol.ToString());
        }

        return tokens;
    }
}

