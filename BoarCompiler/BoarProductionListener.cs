using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using BoarCompiler.Grammar;

namespace BoarCompiler;

public sealed class BoarProductionListener : BoarBaseListener
{
	private readonly List<string> _productions;
	private int _indentLevel;
	private readonly string[] _ruleNames;

	public BoarProductionListener(List<string> productions, string[] ruleNames)
	{
		_productions = productions;
		_ruleNames = ruleNames;
		_indentLevel = 0;
	}

	public override void EnterEveryRule([NotNull] ParserRuleContext ctx)
	{
		var ruleIndex = ctx.RuleIndex;
		var name = ruleIndex >= 0 && ruleIndex < _ruleNames.Length ? _ruleNames[ruleIndex] : $"rule_{ruleIndex}";
		_productions.Add($"{new string(' ', _indentLevel * 2)}{name}");
		_indentLevel++;
	}

	public override void ExitEveryRule([NotNull] ParserRuleContext ctx)
	{
		_indentLevel = Math.Max(0, _indentLevel - 1);
	}
}

