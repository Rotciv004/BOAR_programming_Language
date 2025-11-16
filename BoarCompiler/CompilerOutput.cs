namespace BoarCompiler;

public sealed record CompilerOutput(
	List<string> PifOutput,
	List<string> ProductionString,
	List<string> Errors
);

