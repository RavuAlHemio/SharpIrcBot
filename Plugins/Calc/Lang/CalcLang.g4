grammar CalcLang;

Whitespaces: Whitespace+ -> channel(HIDDEN);

Decimal: [0-9]+ '.' [0-9]+ ;
Identifier: [A-Za-z] [A-Za-z0-9_]* ;

Integer
    : Integer10
    | Integer16
    | Integer8
    | Integer2
    ;

Integer10: [0-9_]+ ;
Integer16: '0x' [0-9a-fA-F_]+ ;
Integer8: '0o' [0-7_]+ ;
Integer2: '0b' [01_]+ ;

fragment Whitespace
    : ' ' // Space
    | '\u0009' // Tab
    | '\u000A' // New Line
    | '\u000D' // Carriage Return
    | '\u000C' // Form Feed
    | '\u000B' // Vertical Tab
    ;

fullExpression: expression EOF;

expression
    : '(' expression ')' # Parens
    | Identifier '(' arglist? ')' # Func
    | '-' expression # Neg
    | expression '**' expression # Pow
    | expression '*' expression # Mul
    | expression '/' expression # Div
    | expression '%' expression # Rem
    | expression '+' expression # Add
    | expression '-' expression # Sub
    | Identifier # Cst
    | Integer # Int
    | Decimal # Dec
    ;

arglist
    : expression ',' arglist
    | expression
    ;
