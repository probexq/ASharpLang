using System;
using System.Collections.Generic;
using System.Globalization;

namespace ASharp.Compiler.Lexere;

public enum TokenType{
    NUMBER, IDENT, // 3, x - Basic
    PLUS, MINUS, MULT, DIV, POW, // +, -, *, /, ^ - Binary
    SQRT, ROUND, ABS, // _, ~, |x| - Unary
    NOT, AND, OR, // !, &, -- - Logical 
    MAX, MIN, // ++(), --() - Syntax Functions
    COMMA, LPAR, RPAR, // , ( ) - syntax
    LET, // let - keywords
    LOG, // log() basic terminal functions
    LAMBDA, EQ, IMPORT, // :: - Misc
    EOF
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
        Column = Column;
    }

    public override string ToString() => $"Token({Type}, {Value})";
}

public class Lexer {
    private readonly string _text;
    private int _pos;
    private int _line;
    private int _column;
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
                        if(Next == '+'){
                            tokens.Add(new Token(TokenType.MAX, "++", _line, _column));
                            advance(); advance();
                        } else {
                            tokens.Add(new Token(TokenType.PLUS, "+", _line, _column));
                            advance();
                        }
                        break;
                    case '-':
                        if(Next == '-'){
                            tokens.Add(new Token(TokenType.MIN, "--", _line, _column));
                            advance(); advance();
                        } else {
                            tokens.Add(new Token(TokenType.MINUS, "-", _line, _column));
                            advance();
                        }
                        break;
                    case '*': tokens.Add(new Token(TokenType.MULT, "*", _line, _column)); advance(); break;
                    case '/': 
                        if(Next == '/'){
                            while(Next != '\n' && Next != '\0') advance();
                        }
                        else if(Next == '*'){
                            advance();
                            while(!(Next == '*' && _text[_pos + 2] == '/')) advance();
                            advance(); advance();
                        } else {
                            tokens.Add(new Token(TokenType.DIV, "/", _line, _column)); advance();
                        } break;
                    case '^': tokens.Add(new Token(TokenType.POW, "^", _line, _column)); advance(); break;
                    case '_': tokens.Add(new Token(TokenType.SQRT, "_", _line, _column)); advance(); break;
                    case '~': tokens.Add(new Token(TokenType.ROUND, "~", _line, _column)); advance(); break;
                    case '|': tokens.Add(new Token(TokenType.ABS, "|", _line, _column)); advance(); break;
                    case '!': tokens.Add(new Token(TokenType.NOT, "!", _line, _column)); advance(); break;
                    case '&': tokens.Add(new Token(TokenType.AND, "&", _line, _column)); advance(); break;
                    case ',': tokens.Add(new Token(TokenType.COMMA, ",", _line, _column)); advance(); break;
                    case ':': if (_pos + 1 < _text.Length && _text[_pos + 1] == ':'){
                        tokens.Add(new Token(TokenType.LAMBDA, "::", _line, _column));
                        advance(); advance();
                    } else continue;
                    break;
                    case '=': tokens.Add(new Token(TokenType.EQ, "=", _line, _column)); advance(); break;
                    case '$': tokens.Add(new Token(TokenType.IMPORT, "$", _line, _column)); advance(); break;
                    case '(': tokens.Add(new Token(TokenType.LPAR, "(", _line, _column)); advance(); break;
                    case ')': tokens.Add(new Token(TokenType.RPAR, ")", _line, _column)); advance(); break;
                    default: 
                        int errLine = _line;
                        int errCol = _column;
                        char errChar = Current;

                        throw new Exception($"Unexpected character: '{Current}' at {errLine}:{errCol}");
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
        if(value == "log") return new Token(TokenType.LOG, value, startLine, startColumn);
        return new Token(TokenType.IDENT, value, startLine, startColumn);
    }
}