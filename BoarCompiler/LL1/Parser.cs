namespace BoarCompiler.LL1;

public sealed record ProductionApplication(int Step, ProductionRule Rule);

public sealed record ParserResult(IReadOnlyList<ProductionApplication> ProductionSequence);

/// <summary>
/// Standard LL(1) predictive parser.
/// </summary>
public sealed class Parser
{
    private readonly Grammar _grammar;
    private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, ProductionRule>> _parseTable;

    public Parser(
        Grammar grammar,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, ProductionRule>> parseTable)
    {
        _grammar = grammar;
        _parseTable = parseTable;
    }

    public ParserResult Parse(IEnumerable<string> tokens)
    {
        var input = tokens.ToList();
        input.Add(LL1Builder.EndMarker);

        var workingStack = new Stack<string>();
        workingStack.Push(LL1Builder.EndMarker);
        workingStack.Push(_grammar.StartSymbol);

        var productions = new List<ProductionApplication>();
        var position = 0;
        var step = 1;

        while (workingStack.Count > 0)
        {
            var top = workingStack.Pop();
            var lookahead = input[position];

            if (top == LL1Builder.EndMarker)
            {
                if (lookahead == LL1Builder.EndMarker)
                {
                    break;
                }

                throw new InvalidOperationException(
                    $"Parsing failed: unexpected trailing input '{lookahead}'.");
            }

            if (!_grammar.IsNonTerminal(top))
            {
                if (!string.Equals(top, lookahead, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Parsing failed at token {position}: expected '{top}' but found '{lookahead}'.");
                }

                position++;
                continue;
            }

            var production = ResolveProduction(top, lookahead);
            productions.Add(new ProductionApplication(step++, production));

            for (var i = production.RightHandSide.Count - 1; i >= 0; i--)
            {
                var symbol = production.RightHandSide[i];
                if (symbol == Grammar.Epsilon)
                {
                    continue;
                }

                workingStack.Push(symbol);
            }
        }

        if (position != input.Count - 1)
        {
            throw new InvalidOperationException("Parsing stopped before consuming all tokens.");
        }

        return new ParserResult(productions);
    }

    private ProductionRule ResolveProduction(string nonTerminal, string lookahead)
    {
        if (_parseTable.TryGetValue(nonTerminal, out var row) &&
            row.TryGetValue(lookahead, out var rule))
        {
            return rule;
        }

        var expected = row is null
            ? "no productions available"
            : $"expected one of [{string.Join(", ", row.Keys)}]";
        throw new InvalidOperationException(
            $"Parsing failed: no table entry for ({nonTerminal}, {lookahead}); {expected}.");
    }
}

