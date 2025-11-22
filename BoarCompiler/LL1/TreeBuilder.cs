namespace BoarCompiler.LL1;

public sealed record ParseTreeNode(int Index, string Info, int? Parent, int? RightSibling);

/// <summary>
/// Builds a father-sibling parsing tree from the sequence of productions used by the parser.
/// </summary>
public sealed class TreeBuilder
{
    private readonly Grammar _grammar;

    public TreeBuilder(Grammar grammar)
    {
        _grammar = grammar;
    }

    public IReadOnlyList<ParseTreeNode> Build(IReadOnlyList<ProductionApplication> applications)
    {
        if (applications.Count == 0)
        {
            throw new InvalidOperationException("Cannot build tree without applied productions.");
        }

        var nodes = new List<NodeState>();
        var stack = new Stack<NodeState>();
        var nextId = 1;

        var root = new NodeState(nextId++, _grammar.StartSymbol, null);
        nodes.Add(root);
        stack.Push(root);

        var applicationIndex = 0;

        while (stack.Count > 0)
        {
            var node = stack.Pop();

            if (!_grammar.IsNonTerminal(node.Symbol))
            {
                continue;
            }

            if (applicationIndex >= applications.Count)
            {
                throw new InvalidOperationException(
                    "The production sequence ended before the tree could be fully expanded.");
            }

            var application = applications[applicationIndex++];
            if (!string.Equals(application.Rule.LeftHandSide, node.Symbol, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Production mismatch: expected to expand <{node.Symbol}> but found <{application.Rule.LeftHandSide}>.");
            }

            var children = new List<NodeState>();
            NodeState? previous = null;

            foreach (var symbol in application.Rule.RightHandSide)
            {
                if (symbol == Grammar.Epsilon)
                {
                    continue;
                }

                var child = new NodeState(nextId++, symbol, node.Id);
                nodes.Add(child);
                children.Add(child);

                if (previous is not null)
                {
                    previous.RightSiblingId = child.Id;
                }

                previous = child;
            }

            for (var i = children.Count - 1; i >= 0; i--)
            {
                if (_grammar.IsNonTerminal(children[i].Symbol))
                {
                    stack.Push(children[i]);
                }
            }
        }

        if (applicationIndex != applications.Count)
        {
            throw new InvalidOperationException(
                "Not all production applications were consumed while building the tree.");
        }

        return nodes
            .Select(n => new ParseTreeNode(
                n.Id,
                FormatInfo(n.Symbol),
                n.ParentId,
                n.RightSiblingId))
            .ToList();
    }

    public void PrintTable(IEnumerable<ParseTreeNode> nodes, TextWriter writer)
    {
        writer.WriteLine("Index\tInfo\tParent\tRightSibling");
        foreach (var node in nodes)
        {
            var parent = node.Parent?.ToString() ?? "-";
            var sibling = node.RightSibling?.ToString() ?? "-";
            writer.WriteLine($"{node.Index,5}\t{node.Info,-20}\t{parent,6}\t{sibling,12}");
        }
    }

    private string FormatInfo(string symbol)
    {
        return _grammar.IsNonTerminal(symbol)
            ? $"<{symbol}>"
            : symbol;
    }

    private sealed class NodeState
    {
        public NodeState(int id, string symbol, int? parentId)
        {
            Id = id;
            Symbol = symbol;
            ParentId = parentId;
        }

        public int Id { get; }
        public string Symbol { get; }
        public int? ParentId { get; }
        public int? RightSiblingId { get; set; }
    }
}

