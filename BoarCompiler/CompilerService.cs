using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using BoarCompiler.Grammar;

namespace BoarCompiler;

public sealed class CompilerService
{
	public CompilerOutput Parse(string sourceCode)
	{
		var errors = new List<string>();
		var pif = new List<string>();
		var productions = new List<string>();

		// Input and lexer
		var inputStream = new AntlrInputStream(sourceCode ?? string.Empty);
		var lexer = new BoarLexer(inputStream);

		var errorListener = new BoarErrorListener(errors);
		lexer.RemoveErrorListeners();
		lexer.AddErrorListener(errorListener); // IAntlrErrorListener<int>

		// Tokens
		var tokenStream = new CommonTokenStream(lexer);
		tokenStream.Fill();
		foreach (var token in tokenStream.GetTokens())
		{
			if (token.Type == -1) continue; // EOF in Antlr4.Runtime 4.6.x
			var typeName = lexer.Vocabulary.GetSymbolicName(token.Type) ?? token.Type.ToString();
			var text = token.Text?.Replace("\r", "\\r").Replace("\n", "\\n");
			pif.Add($"[{typeName}]: '{text}'");
		}

		// Parser
		var parser = new BoarParser(tokenStream);
		parser.RemoveErrorListeners();
		parser.AddErrorListener(errorListener); // IAntlrErrorListener<IToken>

		var tree = parser.program();

		// Productions via listener
		var walker = new ParseTreeWalker();
		var prodListener = new BoarProductionListener(productions, parser.RuleNames);
		walker.Walk(prodListener, tree);

		return new CompilerOutput(pif, productions, errors);
	}
}

