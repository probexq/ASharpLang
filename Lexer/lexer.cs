//using System;
//using System.Collections.Generic;
//using System.Globalization;

namespace sharpash.Lexering;

public enum TokenType{
    NUMBER, IDENT, // 3, x - Basic
    PLUS, MINUS, MULT, DIV, POW, // +, -, *, /, ^ - Binary
    SQRT, ROUND, ABS, // _, ~, |x| - Unary
    NOT, AND, OR, MORE, LESS, IFEQ, NOTEQ, // '!', 'and', 'or', '>', '<', '==', '!=' - Logical 
    MAX, MIN, // +#(), -#() - Syntax Functions
    COMMA, LPAR, RPAR, GATE, // ',' '(' ')' '\' - syntax 
    LET, CONST, COND, // let, const - keywords
    LOG, // log() basic terminal functions
    LAMBDA, EQ, IMPORT, // ::, =, $mconst - Misc
    EOF // End of file
}

public class Token{
    public TokenType Type { get; }
    public string Value { get; }
    public int Line {get;}
    public int Column {get;}

    public Token(TokenType type, string value, int line, int column){
        Type = type;
        Value = value;
        Line = line;
        Column = column;
    }

    public override string ToString() => $"Token({Type}, {Value})";
}

public class Lexer {
    private readonly string _text;
    private int _pos;
    private int _line = 1;
    private int _column = 1;
    private char Current => _pos < _text.Length ? _text[_pos] : '\0';
    private char Next => _pos + 1 <_text.Length ? _text[_pos + 1]: '\0';

    public Lexer(string text) => _text = text;
    private void advance() {
        _pos++;

        if (Current == '\n'){
            _line++;
            _column = 1; 
        } else if(Current != '\r') _column++;
        }
    public char peek_char(int offset = 1) {
        if(_pos + offset >= _text.Length) return '\0';
        return _text[_pos + offset];
    }
    private void SkipWhitespace(){
        while(char.IsWhiteSpace(Current)) advance();
    }

    public List<Token> GenerateTokens(){
        var tokens = new List<Token>();
        while(_pos < _text.Length){
            int startCol = _column;
            int startLine = _line;
            if(char.IsWhiteSpace(Current)){
                SkipWhitespace();
            }
            else if(char.IsDigit(Current) || Current == '.'){
                tokens.Add(Number());
            }
            else if(char.IsLetter(Current)){
                tokens.Add(Identifier());
            } else {
                switch(Current) {
                    case '+':
                        if(Next == '#'){
                            tokens.Add(new Token(TokenType.MAX, "+#", startLine, startCol));
                            advance(); advance();
                        } else {
                            tokens.Add(new Token(TokenType.PLUS, "+", startLine, startCol));
                            advance();
                        }
                        break;
                    case '-': 
                        if(Next == '#'){
                            tokens.Add(new Token(TokenType.MIN, "-#", startLine, startCol));
                            advance(); advance();
                        } else {
                            tokens.Add(new Token(TokenType.MINUS, "-", startLine, startCol));
                            advance();
                        }
                        break;
                    case '*': tokens.Add(new Token(TokenType.MULT, "*", startLine, startCol)); advance(); break;
                    case '/': 
                        if(Next == '/'){
                            advance(); advance();
                            while(Current != '\n' && Current != '\0') advance();
                            continue;
                        }
                        else if(Next == '*'){
                            advance();  advance();
                            while(_pos < _text.Length){
                                if(Current == '*' && Next == '/'){
                                    advance(); advance();
                                    break;
                                }
                                advance();
                            }
                            continue;
                        } else {
                            tokens.Add(new Token(TokenType.DIV, "/", startLine, startCol)); advance();
                        } break;
                    case '^': tokens.Add(new Token(TokenType.POW, "^", startLine, startCol)); advance(); break;
                    case '_': tokens.Add(new Token(TokenType.SQRT, "_", startLine, startCol)); advance(); break;
                    case '~': tokens.Add(new Token(TokenType.ROUND, "~", startLine, startCol)); advance(); break;
                    case '|': tokens.Add(new Token(TokenType.ABS, "|", startLine, startCol)); advance(); break;
                    case '!':
                        if(Next == '='){
                            tokens.Add(new Token(TokenType.NOTEQ, "!=", startLine, startCol));
                            advance(); advance();
                        } else {
                            tokens.Add(new Token(TokenType.NOT, "!", startLine, startCol)); advance();
                        }
                        break;
                    case '&': tokens.Add(new Token(TokenType.AND, "&", startLine, startCol)); advance(); break;
                    case '>': tokens.Add(new Token(TokenType.MORE, ">", startLine, startCol)); advance(); break;
                    case '<': tokens.Add(new Token(TokenType.LESS, "<", startLine, startCol)); advance(); break;
                    case ',': tokens.Add(new Token(TokenType.COMMA, ",", startLine, startCol)); advance(); break;
                    /* lambdas for 0.3.0
                    case ':': if (_pos + 1 < _text.Length && _text[_pos + 1] == ':'){
                        tokens.Add(new Token(TokenType.LAMBDA, "::", startLine, startCol));
                        advance(); advance();
                    } else continue;
                    break;*/
                    case '=': 
                        if(Next == '='){
                            tokens.Add(new Token(TokenType.IFEQ, "==", startLine, startCol));
                            advance(); advance();
                        } else {
                        tokens.Add(new Token(TokenType.EQ, "=", startLine, startCol)); advance();
                        }
                        break;
                    case '$': tokens.Add(new Token(TokenType.IMPORT, "$", startLine, startCol)); advance(); break;
                    case '(': tokens.Add(new Token(TokenType.LPAR, "(", startLine, startCol)); advance(); break;
                    case ')': tokens.Add(new Token(TokenType.RPAR, ")", startLine, startCol)); advance(); break;
                    case '\\': tokens.Add(new Token(TokenType.GATE, "\\", startLine, startCol)); advance(); break;
                    default: 
                        int errLine = _line;
                        int errCol = _column;
                        char errChar = Current;

                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Unexpected character: '{Current}' at line {errLine}, column {errCol}");
                        Console.ResetColor();
                        Environment.Exit(1);
                        break;
                }
            }
        }

        tokens.Add(new Token(TokenType.EOF, string.Empty, _line, _column));
        return tokens;
    }
    private Token Number() {
        int start = _pos;
        int startLine = _line;
        int startColumn = _column;
        bool hasDot = false;
        while(char.IsDigit(Current) || Current == '.'){
            if (Current == '.'){
                if(hasDot) break;
                hasDot = true;
            }
            advance();
        }

        var value = _text.Substring(start, _pos - start);
        return new Token(TokenType.NUMBER, value, startLine, startColumn);
    }

    private Token Identifier(){
        int start = _pos;
        int startLine = _line;
        int startColumn =_column;
        while(char.IsLetterOrDigit(Current) || Current == '_') advance();

        var value = _text.Substring(start, _pos - start);
        if(value == "let") return new Token(TokenType.LET, value, startLine, startColumn);
        if(value == "const") return new Token(TokenType.CONST, value, startLine, startColumn);
        if(value == "condition") return new Token(TokenType.COND, value, startLine, startColumn);
        if(value == "log") return new Token(TokenType.LOG, value, startLine, startColumn);
        if(value == "and") return new Token(TokenType.AND, value, startLine, startColumn);
        if(value == "or") return new Token(TokenType.OR, value, startLine, startColumn);
        return new Token(TokenType.IDENT, value, startLine, startColumn);
    }
}