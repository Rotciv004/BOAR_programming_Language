# Boar LL(1) Grammar Preparation

This note captures the manual preprocessing that turns the raw EBNF specification from the lab handout into the LL(1)‑ready grammar consumed by the parser.

## 1. Removing EBNF Sugar

- Repetitions `{ X }` were rewritten as right-recursive tails, e.g.  
  `{ <statement> } → <statement_block>` with  
  `<statement_block> ::= <statement> <statement_block> | ε`.
- Optional fragments `[ X ]` became small helper productions such as  
  `<declare_stmt_tail> ::= "<-" <expr> | ε`.

## 2. Eliminating Left Recursion

- The `type` production (`<type> ::= <type> "ref" | ...`) was rewritten using a base and a tail:  
  `<type> ::= <base_type> <type_ref_tail>` and `<type_ref_tail> ::= "ref" <type_ref_tail> | ε`.
- Expression levels already used repetition in the EBNF form, so after step 1 they become right-recursive chains (`<term_tail>`, `<factor_tail>`, ...), which are LL(1)-friendly. Exponentiation was rewritten with `<power_opt>` and the unary prefixes are now explicit (`<unary> ::= "not" <unary> | "-" <unary> | <power>`) so that FIRST/FOLLOW no longer collide on the `^` lookahead.

## 3. Left Factoring

- Statement-level ambiguity caused by `IDENTIFIER` (assignment vs. call) is handled with `<ident_stmt_tail>`, which inspects the following token (`"<-"`, `"["`, or `"("`) before deciding.
- Lvalues that start with `at` or indexed identifiers are kept deterministic via `<vector_index_opt>` and a dedicated `"at" "(" <expr> ")" "<-" ...` branch, so assignments like `at(ptr) <- value;` or `vec[i] <- value;` parse without introducing new conflicts.
- Ambiguity caused by `IDENTIFIER` inside primaries was handled with `<primary_id_tail>` so the parser can decide between a simple identifier, a call, or a vector access with a single lookahead.
- Optional keyword branches such as the `other` arm of `when` were factored into dedicated helpers (`<when_other_tail>`) so their FIRST sets stay disjoint.

The resulting productions are stored verbatim in `LL1/grammar.txt`, which is the source the parser loads at runtime. The preprocessing is documented here so the lab grader can trace every compile-time transformation step. No automatic grammar rewriting is performed in code; maintaining this file keeps the LL(1) assets deterministic and easy to audit.*** End Patch

