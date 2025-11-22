using System.Collections.ObjectModel;

namespace BoarCompiler.LL1;

/// <summary>
/// Builds FIRST/FOLLOW sets and the LL(1) parsing table for a Grammar.
/// </summary>
public sealed class LL1Builder
{
    public const string EndMarker = "$";

    private readonly Grammar _grammar;
    private readonly Dictionary<string, HashSet<string>> _firstSets;
    private readonly Dictionary<string, HashSet<string>> _followSets;
    private readonly Dictionary<string, Dictionary<string, ProductionRule>> _parsingTable;

    public LL1Builder(Grammar grammar)
    {
        _grammar = grammar;
        _firstSets = BuildFirstSets();
        _followSets = BuildFollowSets();
        _parsingTable = BuildParsingTable();
        ParsingTable = _parsingTable.ToDictionary(
            kvp => kvp.Key,
            kvp => (IReadOnlyDictionary<string, ProductionRule>)new ReadOnlyDictionary<string, ProductionRule>(kvp.Value),
            StringComparer.Ordinal);
    }

    public IReadOnlyDictionary<string, HashSet<string>> FirstSets => _firstSets;
    public IReadOnlyDictionary<string, HashSet<string>> FollowSets => _followSets;
    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, ProductionRule>> ParsingTable { get; }

    public ProductionRule? Lookup(string nonTerminal, string terminal)
    {
        if (_parsingTable.TryGetValue(nonTerminal, out var row) &&
            row.TryGetValue(terminal, out var production))
        {
            return production;
        }

        return null;
    }

    private Dictionary<string, HashSet<string>> BuildFirstSets()
    {
        var first = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (var nonTerminal in _grammar.NonTerminals)
        {
            first[nonTerminal] = new HashSet<string>(StringComparer.Ordinal);
        }

        var changed = true;
        while (changed)
        {
            changed = false;
            foreach (var rule in _grammar.Productions)
            {
                var firstOfRhs = FirstOfSequence(rule.RightHandSide, first);
                foreach (var symbol in firstOfRhs)
                {
                    if (first[rule.LeftHandSide].Add(symbol))
                    {
                        changed = true;
                    }
                }
            }
        }

        return first;
    }

    private Dictionary<string, HashSet<string>> BuildFollowSets()
    {
        var follow = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (var nonTerminal in _grammar.NonTerminals)
        {
            follow[nonTerminal] = new HashSet<string>(StringComparer.Ordinal);
        }

        follow[_grammar.StartSymbol].Add(EndMarker);

        var changed = true;
        while (changed)
        {
            changed = false;

            foreach (var rule in _grammar.Productions)
            {
                var trailer = new HashSet<string>(follow[rule.LeftHandSide], StringComparer.Ordinal);
                for (var i = rule.RightHandSide.Count - 1; i >= 0; i--)
                {
                    var symbol = rule.RightHandSide[i];
                    if (_grammar.IsNonTerminal(symbol))
                    {
                        changed |= UnionInto(follow[symbol], trailer);

                        if (_firstSets[symbol].Contains(Grammar.Epsilon))
                        {
                            trailer = UnionWithoutEpsilon(trailer, _firstSets[symbol]);
                        }
                        else
                        {
                            trailer = new HashSet<string>(
                                _firstSets[symbol].Where(s => s != Grammar.Epsilon),
                                StringComparer.Ordinal);
                        }
                    }
                    else
                    {
                        trailer = new HashSet<string>(StringComparer.Ordinal) { symbol };
                    }
                }
            }
        }

        return follow;
    }

    private Dictionary<string, Dictionary<string, ProductionRule>> BuildParsingTable()
    {
        var table = new Dictionary<string, Dictionary<string, ProductionRule>>(StringComparer.Ordinal);

        foreach (var nonTerminal in _grammar.NonTerminals)
        {
            table[nonTerminal] = new Dictionary<string, ProductionRule>(StringComparer.Ordinal);
        }

        foreach (var rule in _grammar.Productions)
        {
            var firstSet = FirstOfSequence(rule.RightHandSide, _firstSets);
            foreach (var terminal in firstSet.Where(t => t != Grammar.Epsilon))
            {
                Assign(table, rule.LeftHandSide, terminal, rule);
            }

            if (firstSet.Contains(Grammar.Epsilon))
            {
                foreach (var followSymbol in _followSets[rule.LeftHandSide])
                {
                    Assign(table, rule.LeftHandSide, followSymbol, rule);
                }
            }
        }

        return table;
    }

    private static bool UnionInto(HashSet<string> target, HashSet<string> source)
    {
        var previousCount = target.Count;
        target.UnionWith(source);
        return target.Count != previousCount;
    }

    private static void Assign(
        Dictionary<string, Dictionary<string, ProductionRule>> table,
        string nonTerminal,
        string terminal,
        ProductionRule rule)
    {
        var row = table[nonTerminal];
        if (row.TryGetValue(terminal, out var existing))
        {
            var message = $"Grammar is not LL(1): conflict at [{nonTerminal}, {terminal}] " +
                          $"between productions {existing.Index} and {rule.Index}.";
            throw new InvalidOperationException(message);
        }

        row[terminal] = rule;
    }

    private HashSet<string> FirstOfSequence(
        IReadOnlyList<string> symbols,
        IReadOnlyDictionary<string, HashSet<string>> currentFirstSets)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        if (symbols.Count == 0)
        {
            result.Add(Grammar.Epsilon);
            return result;
        }

        var nullablePrefix = true;
        foreach (var symbol in symbols)
        {
            if (symbol == Grammar.Epsilon)
            {
                result.Add(Grammar.Epsilon);
                nullablePrefix = true;
                break;
            }

            if (!_grammar.IsNonTerminal(symbol))
            {
                result.Add(symbol);
                nullablePrefix = false;
                break;
            }

            var firstSet = currentFirstSets[symbol];
            foreach (var item in firstSet)
            {
                if (item != Grammar.Epsilon)
                {
                    result.Add(item);
                }
            }

            if (firstSet.Contains(Grammar.Epsilon))
            {
                continue;
            }

            nullablePrefix = false;
            break;
        }

        if (nullablePrefix)
        {
            result.Add(Grammar.Epsilon);
        }

        return result;
    }

    private static HashSet<string> UnionWithoutEpsilon(HashSet<string> trailer, HashSet<string> addition)
    {
        var result = new HashSet<string>(trailer, StringComparer.Ordinal);
        foreach (var symbol in addition)
        {
            if (symbol == Grammar.Epsilon)
            {
                continue;
            }

            result.Add(symbol);
        }

        return result;
    }
}

