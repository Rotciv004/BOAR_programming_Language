using System.Linq;
using Antlr4.Runtime;
using BoarCompiler.Grammar;
using BoarCompiler.LL1;
using Ll1Grammar = BoarCompiler.LL1.Grammar;
using Ll1Parser = BoarCompiler.LL1.Parser;

namespace BoarCompiler;

public sealed class CompilerService
{
	private const int EofTokenType = -1;

	private readonly Ll1Grammar _ll1Grammar;
	private readonly Ll1Parser _ll1Parser;
	private readonly TreeBuilder _treeBuilder;

	public CompilerService()
	{
		var grammarPath = Path.Combine(AppContext.BaseDirectory, "LL1", "grammar.txt");
		_ll1Grammar = Ll1Grammar.LoadFromFile(grammarPath);
		var builder = new LL1Builder(_ll1Grammar);
		_ll1Parser = new Ll1Parser(_ll1Grammar, builder.ParsingTable);
		_treeBuilder = new TreeBuilder(_ll1Grammar);
	}

	public CompilerOutput Parse(string sourceCode)
	{
		var errors = new List<string>();
		var pif = new List<string>();
		var productionStrings = new List<string>();
		IReadOnlyList<ParseTreeNode> parseTree = Array.Empty<ParseTreeNode>();

		// Input and lexer
		var inputStream = new AntlrInputStream(sourceCode ?? string.Empty);
		var lexer = new BoarLexer(inputStream);

		var errorListener = new BoarErrorListener(errors);
		lexer.RemoveErrorListeners();
		lexer.AddErrorListener(errorListener); // IAntlrErrorListener<int>

		// Tokens
		var tokenStream = new CommonTokenStream(lexer);
		tokenStream.Fill();
		var parserTokens = new List<string>();
		foreach (var token in tokenStream.GetTokens())
		{
			if (token.Type == EofTokenType) continue;

			var typeName = lexer.Vocabulary.GetSymbolicName(token.Type) ?? token.Type.ToString();
			var text = token.Text?
				.Replace("\r", "\\r", StringComparison.Ordinal)
				.Replace("\n", "\\n", StringComparison.Ordinal);
			pif.Add($"[{typeName}]: '{text}'");

			var ll1Symbol = MapTokenSymbol(token);
			if (ll1Symbol is null)
			{
				errors.Add($"Unsupported token '{token.Text}' ({typeName}) for the LL(1) parser.");
				break;
			}

			parserTokens.Add(ll1Symbol);
		}

		if (errors.Count == 0)
		{
			try
			{
				var parseResult = _ll1Parser.Parse(parserTokens);
				productionStrings = parseResult.ProductionSequence
					.Select(app => FormatProduction(app.Rule))
					.ToList();
				parseTree = _treeBuilder.Build(parseResult.ProductionSequence);
			}
			catch (Exception ex)
			{
				errors.Add(ex.Message);
			}
		}

		return new CompilerOutput(pif, productionStrings, errors, parseTree);
	}

	private static string? MapTokenSymbol(IToken token)
	{
		return token.Type switch
		{
			BoarLexer.IDENTIFIER => "IDENTIFIER",
			BoarLexer.NUM_LITERAL => "NUM_LITERAL",
			BoarLexer.REAL_LITERAL => "REAL_LITERAL",
			BoarLexer.TEXT_LITERAL => "TEXT_LITERAL",
			BoarLexer.FLAG_LITERAL => "FLAG_LITERAL",
			_ => token.Text
		};
	}

	private static string FormatProduction(ProductionRule rule)
	{
		var rhs = rule.RightHandSide.Count == 0
			? Ll1Grammar.Epsilon
			: string.Join(' ', rule.RightHandSide);
		return $"{rule.Index}. <{rule.LeftHandSide}> ::= {rhs}";
	}
}

