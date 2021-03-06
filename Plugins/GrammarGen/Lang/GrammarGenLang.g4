grammar GrammarGenLang;

Whitespaces: Whitespace+ -> channel(HIDDEN);
Comments: '/*' .*? '*/' -> channel(HIDDEN);
LineComments: '//' ~[\r\n]* -> channel(HIDDEN);

EscapedString : '"' ('\\"'|'\\\\'|~["\\])* '"' ;
Identifier : [A-Za-z] [A-Za-z0-9_]* ;

fragment Whitespace
    : ' ' // Space
    | '\u0009' // Tab
    | '\u000A' // New Line
    | '\u000D' // Carriage Return
    | '\u000C' // Form Feed
    | '\u000B' // Vertical Tab
    ;

ggrulebook : ruledef+ ;

ruledef : ggrule | paramrule ;

// cannot call this "rule" because this creates naming conflicts within the generated code
ggrule : Identifier ':' ggproduction ';' ;

paramrule : Identifier '{' Identifier (',' Identifier)+ '}' ':' ggproduction ';' ;

// cannot call this "production" because this creates naming conflicts within the generated code
// ggproduction and alternative are split up to ensure Seq binds more closely than Altern
ggproduction : alternative ('|' alternative)* # Altern ;

alternative : sequenceElem+ # Seq ;

sequenceElem
    : '(' ggproduction ')' # Group
    | '[' ggproduction ']' # Opt
    | sequenceElem '*' # Star
    | sequenceElem '+' # Plus
    | Identifier '{' ggproduction (',' ggproduction)* '}' # Call
    | Identifier # Ident
    | EscapedString # Str
    ;
