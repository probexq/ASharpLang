using System;
using System.Collections.Generic;
using ASharp.Compiler.AST;
using ASharp.Compiler.Lexere;

namespace ASharp.Compiler.Parsing;

public class Parser{
    private readonly List<Token> _tokens;
    private int _pos;
    private Token Current => _pos < _tokens.Count ? _tokens[_pos] : null!;

    public Parser(List<Token> tokens) => _tokens = tokens;

    private void advance() => _pos++;
    private Token expect(TokenType type){
        if(Current.Type != type) throw new Exception($"expected {type}, got {Current.Type}");
        var tok = Current;
        advance();
        return tok;
    }

    public Node parse() => parse_program();

    private Node parse_expr(){
        Node node = term();

        while(Current.Type == TokenType.PLUS || Current.Type == TokenType.MINUS){
            TokenType op = Current.Type;
            advance();
            Node right = term();
            node = new BinOpNode(node, op, right);
        }
        return node;
    }

    private Node term(){
        Node node = factor();

        while(Current.Type == TokenType.MULT || Current.Type == TokenType.DIV){
            TokenType op = Current.Type;
            advance();
            Node right = factor();
            node = new BinOpNode(node, op, right);
        }
        return node;
    }

    private Node factor(){
        if(Current.Type == TokenType.PLUS || Current.Type == TokenType.MINUS || Current.Type == TokenType.SQRT || Current.Type == TokenType.ROUND || Current.Type == TokenType.NOT){
            TokenType op = Current.Type;
            advance();
            Node parse_expr = factor();
            return new UnOpNode(op, parse_expr);
        }
        return power();
    }

    private Node power(){
        Node node = call_or_atom();

        if(Current.Type == TokenType.POW){
            TokenType op = Current.Type;
            advance();
            Node right = factor();
            node = new BinOpNode(node, op , right);
        }
        return node;
    }

    public Node parse_program(){
        var stats = new List<Node>();

        while(Current.Type != TokenType.EOF){
            Node stmt = Current.Type == TokenType.LET ? parse_let() : parse_expr();
            stats.Add(stmt);
            expect(TokenType.COMMA);
        }
        return new ProgramNode(stats);
    }

    public Node parse_let(){
        advance();
        var nameToken = expect(TokenType.IDENT);
        expect(TokenType.EQ);
        var value = parse_expr();
        return new LetNode(nameToken.Value, value);
    }

    private Node call_or_atom(){
        if(Current.Type == TokenType.NUMBER){
            double val = double.Parse(Current.Value, System.Globalization.CultureInfo.InvariantCulture);
            advance();
            return new NumberNode(val);
        }
        if(Current.Type == TokenType.IDENT || Current.Type == TokenType.MAX || Current.Type == TokenType.MIN || Current.Type == TokenType.LOG){
            string name = Current.Value;
            advance();
            if(Current.Type == TokenType.LPAR){
                advance();
                var args = new List<Node>();
                if(Current.Type != TokenType.RPAR){
                    while(true){
                        args.Add(parse_expr());
                        if(Current.Type == TokenType.COMMA){
                            advance();
                        } else {
                            break;
                        }
                    }
                }
                expect(TokenType.RPAR);
                return new CallNode(name, args);
            }
            return new VarNode(name);
        }
        if(Current.Type == TokenType.ABS){
            advance();
            Node expr = parse_expr();
            expect(TokenType.ABS);
            return new CallNode("ABS", new List<Node> {expr});
        }

        if(Current.Type == TokenType.LPAR){
            advance();
            Node expr = parse_expr();
            expect(TokenType.RPAR);
            return expr;
        }
        throw new Exception($"Unexpected token: {Current.Type}");
    }
}