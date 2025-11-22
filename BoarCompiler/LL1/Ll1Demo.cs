namespace BoarCompiler.LL1;

/// <summary>
/// Simple entry point that demonstrates running the LL(1) parser over a mock PIF.
/// </summary>
public static class Ll1Demo
{
    public static void Main(string[] args)
    {
        var grammarPath = Path.Combine(AppContext.BaseDirectory, "LL1", "grammar.txt");
        var grammar = Grammar.LoadFromFile(grammarPath);
        var builder = new LL1Builder(grammar);
        var parser = new Parser(grammar, builder.ParsingTable);

        // Mock PIF tokens: numa x <- 10;
        var sampleTokens = new List<string>
        {
            "numa",
            "IDENTIFIER",
            "<-",
            "NUM_LITERAL",
            ";"
        };

        var parseResult = parser.Parse(sampleTokens);
        var treeBuilder = new TreeBuilder(grammar);
        var nodes = treeBuilder.Build(parseResult.ProductionSequence);

        Console.WriteLine("=== Boar LL(1) Demo ===");
        Console.WriteLine($"Grammar file: {grammarPath}");
        Console.WriteLine($"Input tokens: {string.Join(" ", sampleTokens)}");
        Console.WriteLine();
        treeBuilder.PrintTable(nodes, Console.Out);
    }
}

