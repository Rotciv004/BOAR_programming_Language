using Antlr4.Runtime;

namespace BoarCompiler;

public sealed class BoarErrorListener :
	IAntlrErrorListener<int>,
	IAntlrErrorListener<IToken>
{
	private readonly List<string> _errors;

	public BoarErrorListener(List<string> errors)
	{
		_errors = errors;
	}

	// Lexer errors (char stream)
	public void SyntaxError(
		IRecognizer recognizer,
		int offendingSymbol,
		int line,
		int charPositionInLine,
		string msg,
		RecognitionException e)
	{
		_errors.Add($"Line {line}, Col {charPositionInLine}: {msg}");
	}

	// Parser errors (token stream)
	public void SyntaxError(
		IRecognizer recognizer,
		IToken offendingSymbol,
		int line,
		int charPositionInLine,
		string msg,
		RecognitionException e)
	{
		var tokenText = offendingSymbol?.Text ?? "<EOF>";
		_errors.Add($"Line {line}, Col {charPositionInLine}: {msg} (token: '{tokenText}')");
	}
}

