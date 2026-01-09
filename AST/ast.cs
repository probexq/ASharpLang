using sharpash.Lexering;

namespace sharpash.AST;

public abstract class Node {}

public class NumberNode : Node {
    public double Value {get;}
    public NumberNode(double value) => Value = value;
}

public class VarNode : Node {
    public string Name {get;}
    public Node Value {get;}
    public VarNode(string name, Node value = null!){ 
        Name = name;
        Value = value;
    }
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

public class TypeNode : Node {
    public TokenType Type {get;}
    public string Name {get;}
    public Node Var {get;}

    public TypeNode(TokenType type, string name, Node var){
        Type = type;
        Name = name;
        Var = var;
    }
}

public class ImportNode : Node {
    public string Path {get;}

    public ImportNode(string path) => Path = path;
}

public class ProgramNode : Node {
    public List<Node> Stats {get;}
    public ProgramNode(List<Node> stats) => Stats = stats;
}