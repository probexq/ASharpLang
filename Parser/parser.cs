//using System;
//using System.Collections.Generic;
using sharpash.AST;
using sharpash.Lexering;

namespace sharpash.Parsing;

public class Parser{
    private readonly List<Token> _tokens;
    private int _pos;
    private Dictionary<string, VarNode> _variables;
    private Token Current => _pos < _tokens.Count ? _tokens[_pos] : null!;

    public Parser(List<Token> tokens, Dictionary<string, VarNode> variables = null!) { 
        _tokens = tokens;
        _variables = variables ?? new Dictionary<string, VarNode>();
    }

    private void advance() => _pos++;
    private Token expect(TokenType type, string cmessage = ""){
        var tok = Current;
        if(tok.Type == type){
            advance();
            return tok;
        }
        string message = string.IsNullOrEmpty(cmessage) ? $"Expected '{type}' but found '{tok.Type}'" : cmessage;
        ThrowError(message, tok);
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
        if(_tokens[0].Type == TokenType.EOF) {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Warning: empty file");
            Console.ResetColor();
            Environment.Exit(0);
        }

        while(Current.Type != TokenType.EOF){
            if(Current.Type == TokenType.IMPORT) stats.Add(parse_import());
            Node stmt = Current.Type == TokenType.LET || Current.Type == TokenType.CONST ? parse_new_type() : parse_expr();
            stats.Add(stmt);
            expect(TokenType.COMMA, "Expected ',' at the end of the line.");
        }
        return new ProgramNode(stats);
    }

    public Node parse_new_type(){
        TokenType type = Current.Type;
        advance();
        var nameToken = expect(TokenType.IDENT, $"Expected a variable to assign, got '{Current.Value}'");
        if(!_variables.TryGetValue(nameToken.Value, out var v)){
            expect(TokenType.EQ, $"Expected '=' for variable assignment.");
            var value = parse_expr();
            v = new VarNode(nameToken.Value, value);
            _variables[nameToken.Value] = v;
        } else ThrowError($"Already defined variable '{v.Name}'", nameToken);
        return new TypeNode(type, v.Name, v.Value);
    }

    public Node parse_import(){
        advance();
        var pathToken = expect(TokenType.IDENT);
        expect(TokenType.COMMA, "Expected ',' at the of the line.");
        return new ImportNode(pathToken.Value + ".ash");
    }

    private Node call_or_atom(){
        if(Current.Type == TokenType.NUMBER){
            double val = double.Parse(Current.Value, System.Globalization.CultureInfo.InvariantCulture);
            advance();
            return new NumberNode(val);
        }
        if(Current.Type == TokenType.IDENT){
            string name = Current.Value;
            var token = Current;
            advance();
            if(Current.Type == TokenType.EQ){
                advance();
                if(!_variables.TryGetValue(name, out var value)){
                    ThrowError($"Variable '{name}' is not defined", token);
                    return null!;
                }
                var val = parse_expr();
                _variables[name] = new VarNode(name, val);
                return new VarNode(name, val);
            }
            return new VarNode(name);
        }
        if(Current.Type == TokenType.MAX || Current.Type == TokenType.MIN || Current.Type == TokenType.LOG){
            string name = Current.Value;
            advance();
            expect(TokenType.LPAR, $"Expected '(', but got '{name}'");
            var args = new List<Node>();
            while(Current.Type != TokenType.RPAR){
                args.Add(parse_expr());
                if(Current.Type == TokenType.COMMA){
                    advance();
                } else if(Current.Type != TokenType.RPAR) {
                    ThrowError("Missing a comma between function arguments", Current);
                }
            }
            expect(TokenType.RPAR, "Didn't close '('");
            return new CallNode(name, args);
        }
        
        if(Current.Type == TokenType.ABS){
            advance();
            Node expr = parse_expr();
            expect(TokenType.ABS, "Opened, but didn't close the modulus");
            return new CallNode("ABS", new List<Node> {expr});
        }

        if(Current.Type == TokenType.LPAR){
            advance();
            Node expr = parse_expr();
            expect(TokenType.RPAR, "Didn't close '('");
            return expr;
        }
        ThrowError($"Unexpected '{Current.Value}'", Current);
        return null!; 
    }

    private void ThrowError(string message, Token token){
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"{message} at line {token.Line}, column {token.Column}");
        Console.ResetColor();
        Environment.Exit(1);
    }
}