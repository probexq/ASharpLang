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

    public Token(TokenType type, string value){
        Type = type;
        Value = value;
    }

    public override string ToString() => $"Token({Type}, {Value})";
}

public class Lexer {
    private readonly string _text;
    private int _pos;
    private char Current => _pos < _text.Length ? _text[_pos] : '\0';

    public Lexer(string text) => _text = text;
    private void advance() => _pos++;
    private char peek() => _text[_pos + 1];
    private char peek_next() => _text[_pos + 2];
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
                        if(_pos + 1 < _text.Length && _text[_pos + 1] == '+'){
                            tokens.Add(new Token(TokenType.MAX, "++"));
                            advance(); advance();
                        } else {
                            tokens.Add(new Token(TokenType.PLUS, "+"));
                            advance();
                        }
                        break;
                    case '-':
                        if(_pos + 1 < _text.Length && _text[_pos +1] == '-'){
                            tokens.Add(new Token(TokenType.MIN, "--"));
                            advance(); advance();
                        } else {
                            tokens.Add(new Token(TokenType.MINUS, "-"));
                            advance();
                        }
                        break;
                    case '*': tokens.Add(new Token(TokenType.MULT, "*")); advance(); break;
                    case '/': tokens.Add(new Token(TokenType.DIV, "/")); advance(); break;
                    case '^': tokens.Add(new Token(TokenType.POW, "^")); advance(); break;
                    case '_': tokens.Add(new Token(TokenType.SQRT, "_")); advance(); break;
                    case '~': tokens.Add(new Token(TokenType.ROUND, "~")); advance(); break;
                    case '|': tokens.Add(new Token(TokenType.ABS, "|")); advance(); break;
                    case '!': tokens.Add(new Token(TokenType.NOT, "!")); advance(); break;
                    case '&': tokens.Add(new Token(TokenType.AND, "&")); advance(); break;
                    case ',': tokens.Add(new Token(TokenType.COMMA, ",")); advance(); break;
                    case ':': if (_pos + 1 < _text.Length && _text[_pos + 1] == ':'){
                        tokens.Add(new Token(TokenType.LAMBDA, "::"));
                        advance(); advance();
                    } else throw new Exception($"Unexpected character ':' at {_pos}");
                    break;
                    case '=': tokens.Add(new Token(TokenType.EQ, "=")); advance(); break;
                    case '$': tokens.Add(new Token(TokenType.IMPORT, "$")); advance(); break;
                    case '(': tokens.Add(new Token(TokenType.LPAR, "(")); advance(); break;
                    case ')': tokens.Add(new Token(TokenType.RPAR, ")")); advance(); break;
                    // handle comments
                    case '`': advance(); if(peek() == '<') {
                        advance();
                        while(!(peek() == '>' && peek_next() == '`') && peek() != '\0') advance();
                        advance(); advance();
                    } else {
                        while(peek() != '\n' && peek() != '\0') advance();
                    }
                    break;
                    default: throw new Exception($"Unknown character: {Current}");
                }
            }
        }

        tokens.Add(new Token(TokenType.EOF, string.Empty));
        return tokens;
    }
    private Token Number() {
        int start = _pos;
        bool hasDot = false;
        while(char.IsDigit(Current) || Current == '.'){
            if (Current == '.'){
                if(hasDot) break;
                hasDot = true;
            }
            advance();
        }

        var value = _text.Substring(start, _pos - start);
        return new Token(TokenType.NUMBER, value);
    }

    private Token Identifier(){
        int start = _pos;
        while(char.IsLetterOrDigit(Current) || Current == '_') advance();

        var value = _text.Substring(start, _pos - start);
        if(value == "let") return new Token(TokenType.LET, value);
        if(value == "log") return new Token(TokenType.LOG, value);
        return new Token(TokenType.IDENT, value);
    }
}