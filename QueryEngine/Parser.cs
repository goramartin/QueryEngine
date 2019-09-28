using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace QueryEngine
{
    struct Token
    {
        public enum TokenType
        {
            Match, Select,

            Asterix, Dot, DoubleDot, Comma, Dash, Less, Greater, LeftParen, RightParen, LeftBrace, RightBrace,
            Identifier,
        }
        public readonly string strValue;
        public TokenType type;

        public Token(string value, TokenType type)
        {
            this.strValue = value;
            this.type = type;
        }
    }

    static class Tokenizer
    {
        static Dictionary<string, Token.TokenType> tokenTypes;
        static char EndOfQueryCharacter = ';';
        static Tokenizer()
        {
            tokenTypes = new Dictionary<string, Token.TokenType>();
            InitialiseRegistry();
        }

        public static List<Token> Tokenize(TextReader reader)
        {
            List<Token> tokens = new List<Token>();
            int ch = 0;
            while (true)
            {
                ch = reader.Read();
                if (ch == EndOfQueryCharacter) break;

                //Is one symbol token?
                if (tokenTypes.TryGetValue(((char)ch).ToString(), out Token.TokenType token))
                {
                    tokens.Add(new Token(null, token));
                }
                else if (char.IsWhiteSpace((char)ch))
                {
                    continue;
                }
                else if (Char.IsLetter((char)ch))
                {
                    //Get identifier value.
                    string ident = GetIdentifier((char)ch, reader);

                    //Try whether it is a Query word.
                    Token.TokenType tok = default;
                    if (tokenTypes.TryGetValue(ident, out tok))
                    {
                        tokens.Add(new Token(null, tok));
                    }
                    //Else it is identifier.
                    else { tokens.Add(new Token(ident, Token.TokenType.Identifier)); }
                }
                else throw new ArgumentException($"{(char)ch} Found character that could not be parsed. Tokenizer.");
            }

            return tokens;
        }

        private static string GetIdentifier(char ch, TextReader reader)
        {
            string strValue = "";
            strValue += ch;

            while (true)
            {
                int peekedChar = reader.Peek();
                if (char.IsLetter((char)peekedChar))
                {
                    strValue += (char)peekedChar;
                    reader.Read();
                }
                else { break; }
            }

            return strValue;

        }

        private static void RegisterToken(string str, Token.TokenType type)
        {

            if (tokenTypes.ContainsKey(str))
                throw new ArgumentException("TokenRegistry: Token Type already registered.");

            tokenTypes.Add(str, type);
        }

        private static void InitialiseRegistry()
        {
            RegisterToken("*", Token.TokenType.Asterix);
            RegisterToken(",", Token.TokenType.Comma);
            RegisterToken(".", Token.TokenType.Dot);
            RegisterToken(":", Token.TokenType.DoubleDot);
            RegisterToken("-", Token.TokenType.Dash);
            RegisterToken(">", Token.TokenType.Greater);
            RegisterToken("<", Token.TokenType.Less);
            RegisterToken("[", Token.TokenType.LeftBrace);
            RegisterToken("]", Token.TokenType.RightBrace);
            RegisterToken("(", Token.TokenType.LeftParen);
            RegisterToken(")", Token.TokenType.RightParen);
            RegisterToken("MATCH", Token.TokenType.Match);
            RegisterToken("SELECT", Token.TokenType.Select);
        }

    }

    static class Parser
    {
        static int position;
        static Parser() { position = 0; }

        static public int GetPosition() { return position; }

        static public void ResetPosition() { position = 0; }
        static private void IncrementPosition() { position++; }
        static private void IncrementPositionBy(int p) { position += p; }

        static public SelectNode ParseSelectExpr(List<Token> tokens)
        {
            SelectNode selectNode = new SelectNode();

            if (position > 0 || tokens[position].type != Token.TokenType.Select)
                throw new ArgumentException("SelectParser, Could not find a Select token, or position is not set at 0.");
            else
            {
                IncrementPosition();
                Node node = ParseVariableExpr(tokens);
                if (node == null) throw new ArgumentException("Failed to parse Select Expresion.");
                selectNode.AddNext(node);
            }

            return selectNode;
        }
        static public MatchNode ParseMatchExpr(List<Token> tokens)
        {
            MatchNode matchNode = new MatchNode();
            //IncrementPosition(); //Becuase we always expect we read Select and then we must increase position
            if (!CheckToken(position, Token.TokenType.Match, tokens))
                throw new ArgumentException("SelectParser, position is not set at Match Token.");
            else
            {
                IncrementPosition();
                Node node = ParseVertexExpr(tokens);
                if (node == null) throw new ArgumentException("Failed to parse Match Expresion.");
                matchNode.AddNext(node);
            }
            return matchNode;

        }


        static private Node ParseVariableExpr(List<Token> tokens)
        {
            VariableNode variableNode = new VariableNode();

            // (*)
            if (CheckToken(position, Token.TokenType.Asterix, tokens))
            {
                variableNode.AddName(new IdentifierNode("*"));
            }
            else
            {
                //Expecting identifier.
                Node name = ParseIdentifierExrp(tokens);
                if (name == null) return null;
                else variableNode.AddName(name);
            }

            IncrementPosition();
            //Case of property name .PropName , if there is dot, there must follow identifier.
            if ((CheckToken(position, Token.TokenType.Dot, tokens)))
            {
                IncrementPosition();
                Node identifierNode = ParseIdentifierExrp(tokens);
                if (identifierNode == null) throw new ArgumentException("VariableParser, exprected Indentifier after dot.");
                else variableNode.AddProperty(identifierNode);
                IncrementPosition();
            }

            //Comma signals there is another variable, next variablenode must follow.
            if (CheckToken(position, Token.TokenType.Comma, tokens))
            {
                IncrementPosition();
                Node nextVariableNode = ParseVariableExpr(tokens);
                if (nextVariableNode == null) throw new ArgumentException("VariableParser, exprected Indentifier after comma.");
                else variableNode.AddNext(nextVariableNode);
            }
            return variableNode;
        }
        static private Node ParseVarForMatchExpr(List<Token> tokens)
        {
            VariableNode variableNode = new VariableNode();

            //Expecting identifier. Can be empty.
            Node name = ParseIdentifierExrp(tokens);
            if (name != null) { IncrementPosition(); }
            variableNode.AddName(name);

            //
            if (CheckToken(position, Token.TokenType.DoubleDot, tokens))
            {
                IncrementPosition();
                Node identifierNode = ParseIdentifierExrp(tokens);
                if (identifierNode == null) throw new ArgumentException("VariableForMatchParser, exprected Indentifier after dot.");
                else variableNode.AddProperty(identifierNode);
                IncrementPosition();
            }
            if (variableNode.IsEmpty()) return null;
            else return variableNode;
        }
        static private Node ParseIdentifierExrp(List<Token> tokens)
        {
            if (CheckToken(position, Token.TokenType.Identifier, tokens))
                return new IdentifierNode(tokens[position].strValue);
            else return null;
        }



        static private Node ParseVertexExpr(List<Token> tokens)
        {
            VertexNode vertexNode = new VertexNode();

            // (
            if (CheckToken(position, Token.TokenType.LeftParen, tokens)) IncrementPosition();
            else return null;

            //Parse Values of the variable.
            Node variableNode = ParseVarForMatchExpr(tokens);
            vertexNode.AddVariable(variableNode);

            // )
            if (CheckToken(position, Token.TokenType.RightParen, tokens)) IncrementPosition();
            else return null;

            //Position incremented from leaving function PsrseVariable.
            //Parse Edge.
            Node edgeNode = ParseEdgeExpr(tokens);
            if (edgeNode != null)
            {
                vertexNode.AddNext(edgeNode);
                return vertexNode;
            }

            //Try Parse another pattern, divided by comma.
            Node newPattern = ParseNewPatternExpr(tokens);
            if (newPattern != null) vertexNode.AddNext(newPattern);
            //Always must return valid vertex.
            return vertexNode;
        }
        static private Node ParseEdgeExpr(List<Token> tokens)
        {
            EdgeNode edgeNode = new EdgeNode();

            Node anonymousEdge = ParseAnonymousEdge(tokens);
            if (anonymousEdge != null) edgeNode = (EdgeNode)anonymousEdge;
            else
            {
                Node normalEdge = ParseEdge(tokens);
                if (normalEdge == null) return null;
                else edgeNode = (EdgeNode)normalEdge;
            }

            //Next must be vertex.
            Node vertexNode = ParseVertexExpr(tokens);
            if (vertexNode != null) edgeNode.AddNext(vertexNode);
            else throw new ArgumentException("ParseEdge, expected vertex.");

            return edgeNode;
        }
      
        static private Node ParseNewPatternExpr(List<Token> tokens)
        {
            if (!CheckToken(position, Token.TokenType.Comma, tokens)) return null;
            IncrementPosition();

            MatchDivider matchDivider = new MatchDivider();

            Node newPattern = ParseVertexExpr(tokens);
            if (newPattern == null) throw new ArgumentException("ParseNewPatern, expected new pattern.");
            matchDivider.AddNext(newPattern);
            return matchDivider;
        }


        static private Node ParseAnonymousEdge(List<Token> tokens)
        {
            EdgeNode edgeNode = new EdgeNode();

            if (CheckOutEdgeHead(position, tokens))
            {
                edgeNode.SetEdgeType(EdgeType.OutEdge);
                IncrementPositionBy(2);
            }
            else if (CheckAnyEdgeHead(position, tokens) &&
                    (!CheckToken(position + 1, Token.TokenType.LeftBrace, tokens)))
            {
                edgeNode.SetEdgeType(EdgeType.AnyEdge);
                IncrementPosition();
            }
            else if (CheckInEdgeHead(position, tokens) &&
                    !CheckToken(position + 2, Token.TokenType.LeftBrace, tokens))
            {
                edgeNode.SetEdgeType(EdgeType.InEdge);
                IncrementPositionBy(2);
            }

            if (edgeNode.GetEdgeType() != default(EdgeType)) return edgeNode;
            else return null;

        }
        static private Node ParseEdge(List<Token> tokens)
        {
            EdgeNode edgeNode = new EdgeNode();
            //Define type of edge.  in <...-, out -...>, any -...-
            EdgeType type = DefineEdgeType(tokens);
            if (type == EdgeType.NotEdge) return null;
            else
            {
                edgeNode.SetEdgeType(type);
                if (type == EdgeType.InEdge) IncrementPositionBy(2);
                else IncrementPosition();
            }

            // [
            if (CheckToken(position, Token.TokenType.LeftBrace, tokens)) IncrementPosition();
            else throw new ArgumentException("ParseEdge, expected Leftbrace.");

            //Parse variable of edge.
            Node variableNode = ParseVarForMatchExpr(tokens);
            if (variableNode == null) throw new ArgumentException("ParseEdge, expected variable.");
            else edgeNode.AddVariable(variableNode);

            // ]
            if (CheckToken(position, Token.TokenType.RightBrace, tokens)) IncrementPosition();
            else throw new ArgumentException("ParseEdge, expected rightbrace.");


            //Skip end character of edge.  ->,-
            if (type == EdgeType.OutEdge) IncrementPositionBy(2);
            else IncrementPosition();

            return edgeNode;
        }
        static private EdgeType DefineEdgeType(List<Token> tokens)
        {
            //The order of checks matter. We first must refute out edge,
            //else there could be any edge instead of out edge.
            //Out edge -[..]->
            if (PredictEdgeTypeOut(tokens))
                return EdgeType.OutEdge;
            //In edge <-[..]-
            else if (PredictEdgeTypeIn(tokens))
                return EdgeType.InEdge;
            //Any edge -[..]-
            else if (PredictEgeTypeAny(tokens))
                return EdgeType.AnyEdge;
            else return EdgeType.NotEdge;
        }

        static private bool PredictEgeTypeAny(List<Token> tokens)
        {
            // -[e]-
            int pOne = position + 4;
            // -[:prop]-
            int pTwo = position + 5;
            // -[e:x]-
            int pThree = position + 6;
            if (CheckAnyEdgeHead(position, tokens) && CheckDashForward(pOne, pTwo, pThree, tokens))

                return true;
            else return false;
        }

        static private bool PredictEdgeTypeIn(List<Token> tokens)
        {
            // <-[e]-
            int pOne = position + 5;
            // <-[:prop]-
            int pTwo = position + 6;
            // <-[e:x]-
            int pThree = position + 7;
            if (CheckInEdgeHead(position, tokens) && CheckDashForward(pOne, pTwo, pThree, tokens))
                return true;
            else return false;
        }

        static private bool PredictEdgeTypeOut(List<Token> tokens)
        {
            // -[e]->
            int pOne = position + 4;
            // -[:prop]->
            int pTwo = position + 5;
            // -[e:x]->
            int pThree = position + 6;
            if (CheckAnyEdgeHead(position, tokens) && CheckOutEdgeHeadForward(pOne, pTwo, pThree, tokens))
                return true;
            else return false;
        }

        static private bool CheckDashForward(int first, int second, int third, List<Token> tokens)
        {
            if (CheckToken(first, Token.TokenType.Dash, tokens) ||
             CheckToken(second, Token.TokenType.Dash, tokens) ||
             CheckToken(third, Token.TokenType.Dash, tokens)) return true;
            else return false;
        }

        static private bool CheckOutEdgeHeadForward(int first, int second, int third, List<Token> tokens)
        {
            if (CheckOutEdgeHead(first, tokens) ||
                CheckOutEdgeHead(second, tokens) ||
                (CheckOutEdgeHead(third, tokens)))
                return true;
            else return false;
        }

        static private bool CheckInEdgeHead(int p, List<Token> tokens)
        {
            if (CheckToken(p, Token.TokenType.Less, tokens) &&
                    CheckToken(p + 1, Token.TokenType.Dash, tokens))
                return true;
            else return false;
        }

        static private bool CheckOutEdgeHead(int p, List<Token> tokens)
        {
            if (CheckToken(p, Token.TokenType.Dash, tokens) &&
                     (CheckToken(p + 1, Token.TokenType.Greater, tokens)))
                return true;
            else return false;
        }

        static private bool CheckAnyEdgeHead(int p, List<Token> tokens)
        {
            if (CheckToken(p, Token.TokenType.Dash, tokens))
                return true;
            else return false;
        }




        //Check for token on position given.
        static private bool CheckToken(int p, Token.TokenType type, List<Token> tokens)
        {
            if (p < tokens.Count && tokens[p].type == type)
            {
                return true;
            }
            return false;

        }
    }




    interface IVisitor<T>
    {
        T GetResult();
        void Visit(SelectNode node);
        void Visit(MatchNode node);
        void Visit(MatchDivider node);
        void Visit(VertexNode node);
        void Visit(EdgeNode node);
        void Visit(VariableNode node);
        void Visit(IdentifierNode node);
    }


    //Creates list of variables to print for select from query.
    class SelectVisitor : IVisitor<List<SelectVariable>>
    {
        List<SelectVariable> result;
        bool addingName;

        public SelectVisitor()
        {
            result = new List<SelectVariable>();
            addingName = true;
        }

        public List<SelectVariable> GetResult()
        { return this.result; }
        public void Visit(SelectNode node)
        {
            node.next.Accept(this);
            if (result.Count < 1)
                throw new ArgumentException("SelectVisitor, failed to parse select expr.");
        }

        //Create new variable and try parse its name and propname.
        //Name shall never be null.
        //Jump to next variable node.
        public void Visit(VariableNode node)
        {
            addingName = true;
            result.Add(new SelectVariable());
            if (node.name == null)
                throw new ArgumentException("SelectVisitor, could not parse variable name.");
            else
            {
                node.name.Accept(this);
                addingName = false;
                if (node.propName != null) node.propName.Accept(this);
            }

            if (node.next == null) return;
            else node.next.Accept(this);

        }
        public void Visit(IdentifierNode node)
        {
            //If adding name it must be successful, otherwise it is failed parsing.
            //There is always one object in results, count -1 can never undergo limit.
            if (addingName)
            {
                if (!result[result.Count - 1].TrySetName(node.value))
                    throw new ArgumentException("SelectVisitor, could not set name to variable.");
            }
            //If it try assign propname, it also must always be success, 
            //because it could not be assigned before this.
            else
            {
                if (!result[result.Count - 1].TrySetPropName(node.value))
                    throw new ArgumentException("SelectVisitor, could not set propname to variable.");
            }

        }
        public void Visit(MatchDivider node)
        {
            throw new NotImplementedException();
        }

        //Can never appear.  
        public void Visit(MatchNode node)
        {
            throw new NotImplementedException();
        }
        public void Visit(VertexNode node)
        {
            throw new NotImplementedException();
        }
        public void Visit(EdgeNode node)
        {
            throw new NotImplementedException();
        }
    }


    //Creates pattern for match from query.
    //We create new pattern for each division of a comma {viewed as MatchDivider node} 
    class MatchVisitor : IVisitor<List<List<BaseMatch>>>
    {
        List<List<BaseMatch>> result;
        List<BaseMatch> currentPattern;
        Dictionary<string, int> scope;
        Dictionary<string, Table> vTables;
        Dictionary<string, Table> eTables;
        bool readingName;
        bool readingVertex;
        int patternCount;


        public MatchVisitor(Scope s,
            Dictionary<string, Table> v, Dictionary<string, Table> e)
        {
            this.currentPattern = new List<BaseMatch>();
            this.result = new List<List<BaseMatch>>();
            this.scope = s.GetScopeVariables();
            this.vTables = v;
            this.eTables = e;
            this.readingName = true;
            this.readingVertex = true;
            this.patternCount = 0;
        }


        public List<List<BaseMatch>> GetResult()
        { return this.result; }

        public void Visit(MatchNode node)
        {
            //Create new pattern and start its parsing.
            result.Add(currentPattern);
            node.next.Accept(this);

            for (int i = 0; i < result.Count; i++)
            {
                if (result[i].Count <= 0)
                    throw new ArgumentException("MatchVisitor, failed to parse match expr.");
            }
        }

        public void Visit(VertexNode node)
        {
            this.readingVertex = true;
            VertexMatch vm = new VertexMatch();
            currentPattern.Add(vm);
            if (node.variable != null) node.variable.Accept(this);
            if (node.next != null) node.next.Accept(this);
        }

        public void Visit(EdgeNode node)
        {
            this.readingVertex = false;
            EdgeMatch em = new EdgeMatch();
            em.SetEdgeType(node.GetEdgeType());
            currentPattern.Add(em);
            if (node.variable != null) node.variable.Accept(this);
            if (node.next == null)
                throw new ArgumentException("MatchVisitor, missing end vertex from edge.");
            else node.next.Accept(this);

        }

        public void Visit(VariableNode node)
        {
            readingName = true;
            //It is not anonnymous field.
            if (node.name != null)
            {
                node.name.Accept(this);
            }
            //It has set type.
            if (node.propName != null)
            {
                readingName = false;
                node.propName.Accept(this);
            }

        }

        public void Visit(IdentifierNode node)
        {
            int relativePosition = currentPattern.Count - 1;
            if (readingName)
            {
                //It has name, it can not be anonnymous.
                currentPattern[relativePosition].SetAnnonymous(false);
                //Try if the variable is alredy in the scope.
                if (scope.TryGetValue(node.value, out int positionOfRepeated))
                {
                    //If it is, set the repeated status.
                    currentPattern[relativePosition].SetRepeated(true);
                    currentPattern[relativePosition].SetPositionOfRepeatedField(positionOfRepeated);
                }
                //It is new variable, then add it to scope.
                else scope.Add(node.value, GetAbsolutePosition());
            }
            else
            {
                if (readingVertex) ProcessType(relativePosition, node, vTables);
                else ProcessType(relativePosition, node, eTables);
            }
        }

        private void ProcessType(int p, IdentifierNode node, Dictionary<string, Table> d)
        {
            //Try find the table of the variable, it has to be always valid table name.
            if (!d.TryGetValue(node.value, out Table table))
                throw new ArgumentException("MatchVisitor, could not parse Table name.");
            else currentPattern[p].SetType(table);
        }

        private int GetAbsolutePosition()
        {
            int c = 0;
            for (int i = 0; i < result.Count; i++)
                c += result[i].Count;
            return (c - 1);
        }

        public void Visit(MatchDivider node)
        {
            //Create new pattern and start its parsing.
            currentPattern = new List<BaseMatch>();
            result.Add(currentPattern);
            patternCount++;
            node.next.Accept(this);
        }

        //Should never occur.
        public void Visit(SelectNode node)
        {
            throw new NotImplementedException();
        }
    }






    //Parent to every node.
    //Gives Visit method.
    abstract class Node
    {
        public abstract void Accept<T>(IVisitor<T> visitor);
    }

    abstract class QueryNode : Node
    {
        public Node next;
        public void AddNext(Node next)
        {
            this.next = next;
        }
    }

    //Only vertices and edges inherit from this class.
    abstract class CommomMatchNode : QueryNode
    {
        public Node variable;

        public void AddVariable(Node v)
        {
            this.variable = v;
        }
    }


    class MatchNode : QueryNode
    {
        public MatchNode() { }

        public override void Accept<T>(IVisitor<T> visitor)
        {
            visitor.Visit(this);
        }
    }
    class SelectNode : QueryNode
    {
        public SelectNode() { }

        public override void Accept<T>(IVisitor<T> visitor)
        {
            visitor.Visit(this);
        }
    }


    enum EdgeType { NotEdge, InEdge, OutEdge, AnyEdge };
    class EdgeNode : CommomMatchNode
    {
        EdgeType type;
        public override void Accept<T>(IVisitor<T> visitor)
        {
            visitor.Visit(this);
        }
        public void SetEdgeType(EdgeType type)
        {
            this.type = type;
        }
        public EdgeType GetEdgeType()
        {
            return this.type;
        }

    }
    class VertexNode : CommomMatchNode
    {
        public override void Accept<T>(IVisitor<T> visitor)
        {
            visitor.Visit(this);
        }
    }
    class MatchDivider : QueryNode
    {
        public override void Accept<T>(IVisitor<T> visitor)
        {
            visitor.Visit(this);
        }
    }

    class VariableNode : QueryNode
    {
        public Node name;
        public Node propName;

        public void AddName(Node n)
        {
            this.name = n;
        }

        public void AddProperty(Node p)
        {
            this.propName = p;
        }

        public bool IsEmpty()
        {
            if ((name == null) && (propName == null))
            {
                return true;
            }
            else return false;
        }

        public override void Accept<T>(IVisitor<T> visitor)
        {
            visitor.Visit(this);
        }
    }
    class IdentifierNode : Node
    {
        public string value { get; private set; }

        public IdentifierNode(string v) { this.value = v; }

        public void AddValue(string v) { this.value = v; }

        public override void Accept<T>(IVisitor<T> visitor)
        {
            visitor.Visit(this);
        }
    }
}
