# Boar Programming Language & Blazor IDE

An experimental programming language named **Boar** with a web-based IDE built in **Blazor (.NET 10 preview)**. The project contains:
- `BoarCompiler` – ANTLR4-based grammar, lexer/parser generation, and a small compiler service exposing tokens (PIF), productions, and syntax errors.
- `Boar_programming_Language` – Blazor Server front-end providing a minimal in-browser editor and parse output panes.

## Status
Early prototype (education / research). Grammar, tooling, and runtime semantics are incomplete and subject to change.

## Features
- ANTLR4 grammar (`Boar.g4`) with listener + visitor generation.
- Basic primitive & reference types: `numa`, `real`, `flag`, `text`, `vector`, optional reference modifiers `ref`.
- Statements: declarations, assignments, `task` definitions, `when` / `other` conditional blocks, `repeat` loops, `show` (print), `give` (return), `release` memory, function calls.
- Memory / indirection operations: `locate`, `at`, `claim`, `release`.
- Expressions with precedence: unary, power, factor, term, comparison, equality, logic_and, logic_or.
- Vector literals `[ ... ]` and index access `v[i]`.
- Error collection (lexer + parser) via custom `BoarErrorListener`.
- Production tracing via `BoarProductionListener` (indent-based rule entry trace).

## Sample Code
```
// Adds two numbers
task numa add(numa a, numa b) {
    numa sum <- a + b;
    give sum;
}

// Entry point sample
task numa main() {
    show(add(3, 4));
    numa x <- 5;
    numa ref p <- locate(x);
    at(p) <- 10;
    show(x);
    numa ref buf <- claim(4);
    at(buf) <- 42;
    show(at(buf));
    release(buf);
    vector v <- [1, 2, 3];
    numa i <- 0;
    numa s <- 0;
    repeat (i < 3) {
        s <- s + v[i];
        i <- i + 1;
    }
    when (s > 5) do {
        show("big");
    } other {
        show("small");
    }
    give 0;
}
```

## Grammar Overview (High Level)
Top-level: `program -> statement* EOF`
Main statements: `declare_stmt`, `assign_stmt`, `task_def`, `when_stmt`, `repeat_stmt`, `show_stmt`, `give_stmt`, `call_stmt`, `release_stmt`
Types: `type -> baseType (REF)*`, `baseType -> numa | real | flag | text | vector`
Control flow: `when (expr) do { ... } other { ... }`, `repeat (expr) { ... }`
Memory ops: `locate(id)`, `at(expr)` (dereference or buffer access), `claim(expr)` allocate, `release(expr)` free.
Expressions: logical OR ? AND ? equality ? comparison ? term ? factor ? power ? primary.
See `BoarCompiler/Grammar/Boar.g4` for full details.

## Project Structure
```
/BoarCompiler
  BoarCompiler.csproj          // Library (ANTLR runtime + codegen)
  Grammar/Boar.g4              // ANTLR grammar
  CompilerService.cs           // Orchestrates lexing/parsing
  BoarErrorListener.cs         // Custom error listener
  BoarProductionListener.cs    // Production trace listener
  CompilerOutput.cs            // Result DTO
/Boar_programming_Language
  Boar_programming_Language.csproj // Blazor Server app
  Components/Pages/IDE.razor       // Simple IDE page
  Components/Layout/MainLayout.razor
  ...
```
ANTLR generation is triggered by `dotnet build` through the `<Antlr4 />` MSBuild item in `BoarCompiler.csproj`.

## Quick Start
Prerequisites: .NET 10 preview SDK installed.

1. Clone repository.
2. Restore & build: `dotnet restore` then `dotnet build` (or just `dotnet build`).
3. Run the Blazor IDE: `dotnet run --project Boar_programming_Language`.
4. Open browser at the shown URL (usually `https://localhost:xxxx`).
5. Paste sample code and press Parse to view tokens, productions, and errors.

## How Parsing Works
1. `CompilerService.Parse` creates an `AntlrInputStream` from source.
2. `BoarLexer` tokenizes input; tokens collected into PIF list.
3. `BoarParser` builds a parse tree (`program`).
4. `ParseTreeWalker` + `BoarProductionListener` record rule entry order.
5. `BoarErrorListener` captures both lexer and parser syntax errors.
6. Results packaged in `CompilerOutput` for display.

## Extending
- Modify grammar in `Boar.g4` (add rules, tokens).
- Build to regenerate generated lexer/parser classes.
- Augment listeners or introduce a visitor for semantic analysis / symbol table / IR.
- Add runtime semantics (execution / interpreter) in `BoarCompiler`.

## Roadmap Ideas
- Semantic analysis (types, scopes).
- Interpreter or bytecode emitter.
- Vector & memory safety checks.
- Improved IDE (syntax highlighting, diagnostics, inline errors).
- Unit tests for grammar and compiler pipeline.

## Contributing
Open issues or submit PRs. Keep changes small and focused. Update README & sample code when adding language features.

## License
(Choose a license – e.g., MIT – and place the text in a `LICENSE` file.)

## Disclaimer
Prototype educational project; not production-ready. Language design and syntax may change frequently.