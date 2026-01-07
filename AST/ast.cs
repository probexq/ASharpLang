using ASharp.Compiler.Lexere;

namespace ASharp.Compiler.AST;

public abstract class Node {}

public class NumberNode : Node {
    public double Value {get;}
    public NumberNode(double value) => Value = value;
}

public class VarNode : Node {
    public string Name {get;}
    public VarNode(string name) => Name = name;
}

public class BinOpNode : Node {
    public Node Left {get;}
    public TokenType Op {get;}
    public Node Right {get;}
    public BinOpNode(Node left, TokenType op, Node right){
        Left = left;
        Op = op;
        Right = right;
    }
}

public class UnOpNode : Node {
    public TokenType Op {get;}
    public Node Expr{get;}
    public UnOpNode(TokenType op, Node expr){
        Op = op;
        Expr = expr;
    }
}

public class CallNode : Node {
    public string FuncName {get;}
    public List<Node> Args {get;}
    public CallNode(string funcName, List<Node> args){
        FuncName = funcName;
        Args = args;
    }
}

public class LetNode : Node {
    public string Name {get;}
    public Node Value {get;}

    public LetNode(string name, Node value){
        Name = name;
        Value = value;
    }
}

public class ProgramNode : Node {
    public List<Node> Stats {get;}
    public ProgramNode(List<Node> stats) => Stats = stats;
}