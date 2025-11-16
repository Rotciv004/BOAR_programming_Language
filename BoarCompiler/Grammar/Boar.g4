grammar Boar;

// Parser rules
program
    : statement* EOF
    ;

statement
    : declare_stmt SEMICOLON
    | assign_stmt SEMICOLON
    | when_stmt
    | repeat_stmt
    | show_stmt SEMICOLON
    | task_def
    | give_stmt SEMICOLON
    | call_stmt SEMICOLON
    | release_stmt SEMICOLON
    ;

declare_stmt
    : type IDENTIFIER (ASSIGN_OP expr)?
    ;

assign_stmt
    : IDENTIFIER ASSIGN_OP expr
    ;

task_def
    : TASK type IDENTIFIER LPAREN param_list? RPAREN LBRACE statement* RBRACE
    ;

param_list
    : type IDENTIFIER (COMMA type IDENTIFIER)*
    ;

when_stmt
    : WHEN LPAREN expr RPAREN DO LBRACE statement* RBRACE (OTHER LBRACE statement* RBRACE)?
    ;

repeat_stmt
    : REPEAT LPAREN expr RPAREN LBRACE statement* RBRACE
    ;

show_stmt
    : SHOW LPAREN expr RPAREN
    ;

give_stmt
    : GIVE expr?
    ;

call_stmt
    : IDENTIFIER LPAREN expr_list? RPAREN
    ;

release_stmt
    : RELEASE LPAREN expr RPAREN
    ;

type
    : baseType (REF)*
    ;

baseType
    : NUMA
    | REAL
    | FLAG
    | TEXT
    | VECTOR
    ;

expr_list
    : expr (COMMA expr)*
    ;

expr
    : logic_or
    ;

logic_or
    : logic_and (OR logic_and)*
    ;

logic_and
    : equality (AND equality)*
    ;

equality
    : comparison ((EQ | NEQ) comparison)*
    ;

comparison
    : term ((LT | GT | LE | GE) term)*
    ;

term
    : factor ((ADD | SUB) factor)*
    ;

factor
    : unary ((MUL | DIV | MOD) unary)*
    ;

unary
    : (NOT | SUB) unary
    | power
    ;

power
    : primary (POW unary)*
    ;

primary
    : IDENTIFIER
    | NUM_LITERAL
    | REAL_LITERAL
    | TEXT_LITERAL
    | FLAG_LITERAL
    | LPAREN expr RPAREN
    | call_stmt
    | vector_literal
    | vector_access
    | LOCATE LPAREN IDENTIFIER RPAREN
    | AT LPAREN expr RPAREN
    | CLAIM LPAREN expr RPAREN
    ;

vector_literal
    : LBRACK expr_list? RBRACK
    ;

vector_access
    : IDENTIFIER LBRACK expr RBRACK
    ;

// Lexer rules
WHEN       : 'when';
DO         : 'do';
OTHER      : 'other';
REPEAT     : 'repeat';
SHOW       : 'show';
GIVE       : 'give';
TASK       : 'task';
RELEASE    : 'release';
LOCATE     : 'locate';
AT         : 'at';
CLAIM      : 'claim';

NUMA       : 'numa';
REAL       : 'real';
FLAG       : 'flag';
TEXT       : 'text';
VECTOR     : 'vector';
REF        : 'ref';

FLAG_LITERAL
    : 'true' | 'false'
    ;

TEXT_LITERAL
    : '"' (~["\\] | '\\' .)* '"'
    ;

REAL_LITERAL
    : DIGIT+ '.' DIGIT+ ([eE] [+-]? DIGIT+)?
    ;

NUM_LITERAL
    : DIGIT+
    ;

IDENTIFIER
    : [a-zA-Z_] [a-zA-Z_0-9]*
    ;

ASSIGN_OP  : '<-';
EQ         : '==';
NEQ        : '!=';
LE         : '<=';
GE         : '>=';
LT         : '<';
GT         : '>';
ADD        : '+';
SUB        : '-';
MUL        : '*';
DIV        : '/';
MOD        : '%';
POW        : '^';
NOT        : 'not';
AND        : 'and';
OR         : 'or';

LPAREN     : '(';
RPAREN     : ')';
LBRACE     : '{';
RBRACE     : '}';
LBRACK     : '[';
RBRACK     : ']';
COMMA      : ',';
SEMICOLON  : ';';

WS         : [ \t\r\n]+ -> skip;
LINE_COMMENT : '//' ~[\r\n]* -> skip;
BLOCK_COMMENT: '/*' .*? '*/' -> skip;

fragment DIGIT : [0-9];

