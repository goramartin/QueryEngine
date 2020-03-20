
/**
 * Contains definition of creators from file.
 * Takes a file and creates given type from the file.
 * Reading and building of the type is done in states.
 * The class creator takes reader (reads given file) and processor.
 * Processor is given words from a file and builds the class.
 * When reading is finished the processor is halted and the creator can
 * return desired object.
 * 
 *  There are three creators, for tables (same for edges and node tables),
 *  one that creates vertices from a data file and the third that creates list of 
 *  edges (in/out).
 * 
 * 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{

    /// <summary>
    /// Interface for state oriented processing of incoming files.
    /// </summary>
    interface IProcessor<T>
    {
        bool Finished();
        void Process(string param);
        T GetResult();

        void PassParameters(params object[] prms);    
    }
    
    interface ICreator<T>
    {
        T Create();
    }

    /// <summary>
    /// Class takes reader and processor that proccesses words from reader.
    /// Main purpose is to have a general way to create classes from files.
    /// </summary>
    class CreatorFromFile<T> : ICreator<T>
    {
        IReader reader;
        IProcessor<T> processor;

        public CreatorFromFile(IReader reader, IProcessor<T> processor)
        {
            this.reader = reader;
            this.processor = processor;
        }

        /// <summary>
        /// Processes file. Reading until reached end point of a processor.
        /// </summary>
        /// <returns> Value based on template. </returns>
        public T Create()
        {
            while (!this.processor.Finished())
            {
                string wd = this.reader.Read();
                this.processor.Process(wd);

            }
            this.reader.Dispose();
            return this.processor.GetResult();
        }

    }

    /// <summary>
    /// Creates distionary/map from data scheme with specific nodes in the graph.
    /// </summary>
    class TableDictProcessor : IProcessor<Dictionary<string, Table>>
    {
        Dictionary<string, Table> dict;
        Table newTable;
        string newPropName;
        bool finished;
        enum State { Kind, Name, LeftBracket, PropName, PropType,
                     DoubleDot,Comma, CommaS, RightBracket, RightSquaredBrace, LeftSquareBrace,
                       LeftMark, RightMark};
        State state;
        State lastState;

        public TableDictProcessor()
        {
            this.dict = new Dictionary<string, Table>();
            this.finished = false;
            this.state = State.LeftSquareBrace;
            this.newTable = null;
            this.newPropName = null;
        }

        public bool Finished()
        {
            return this.finished;
        }

        public Dictionary<string, Table> GetResult()
        {
            return this.dict;
        }

        //Based on actual state process incoming word.
        public void Process(string param)
        {
            switch (state)
            {
                case State.Kind:
                    ProcessKind(param);
                    break;
                case State.Name:
                    ProcessName(param);
                    break;
                case State.LeftBracket:
                    ProcessLeftBracket(param);
                    break;
                case State.PropName:
                    ProcessPropName(param);
                    break;
                case State.PropType:
                    ProcessPropType(param);
                    break;
                case State.DoubleDot:
                    ProcessDoubleDot(param);
                    break;
                case State.Comma:
                    ProcessCommaAfterProp(param);
                    break;
                case State.CommaS:
                    ProcessCommaAfterBracket(param);
                    break;
                case State.LeftSquareBrace:
                    ProcessLeftSquaredBrace(param);
                    break;
                case State.LeftMark:
                    ProcessLeftMark(param);
                    break;
                case State.RightMark:
                    ProcessRightMark(param);
                    break;
                default:
                    throw new ArgumentException("TableCreator, Expected different state.");
            }
        }


        public void PassParameters(params object[] prms)
        {
            this.dict = (Dictionary<string, Table>)prms[0];
        }


        private void ProcessLeftSquaredBrace(string param)
        {
            if (param != "[") throw new ArgumentException("Failed to parse types of table, expected [.");
            this.state = State.LeftBracket;
        }

        private void ProcessLeftBracket(string param)
        { 
            if (param != "{") throw new ArgumentException($"{this.GetType()} Expected left Bracket");
            this.lastState = State.LeftBracket;
            this.state = State.LeftMark;
        }

        private void ProcessLeftMark(string param)
        {
            if (param != "\"") throw new ArgumentException(($"{this.GetType()} Expected left quotations."));

            if (this.lastState == State.LeftBracket) this.state = State.Kind;
            else if (this.lastState == State.Kind) this.state = State.Name;
            else if (this.lastState == State.Name) this.state = State.PropName;
            else if (this.lastState == State.PropName) this.state = State.PropType;
            else if (this.lastState == State.PropType) this.state = State.PropName;
        }

        private void ProcessKind(string param)
        {
            if (param != "Kind") 
                throw new ArgumentException($"{this.GetType()} Expected Kind");
            this.lastState = State.Kind;
            this.state = State.RightMark;
        }


        private void ProcessRightMark(string param)
        {
            if (param != "\"")
                throw new ArgumentException($"{this.GetType()} Expected \"");
            if ((lastState == State.Kind) || (lastState == State.PropName)) this.state = State.DoubleDot;
            else this.state = State.Comma;
        }


        private void ProcessDoubleDot(string param)
        {
            if (param != ":") throw new ArgumentException($"{this.GetType()} Expected :");
            this.state = State.LeftMark;
        }


        /// <summary>
        /// Processes name of the table. Call for creating of a table.
        /// </summary>
        private void ProcessName(string param) 
        {
            this.newTable = new Table(param);
            if (this.dict.ContainsKey(param)) 
                throw new ArgumentException($"{this.GetType()} Adding table that exists.");
            else this.dict.Add(param, this.newTable);
            this.lastState = State.Name;
            this.state = State.RightMark;
        }


        private void ProcessCommaAfterProp(string param)
        {
            if (param == ",") this.state = State.LeftMark;
            else if (param == "}") this.state = State.CommaS;
        }

        private void ProcessCommaAfterBracket(string param)
        {
            if (param == ",") this.state = State.LeftBracket;
            else if (param == "]")
            {
                this.finished = true;
                return;
            }
        }

        private void ProcessPropName(string param)
        {
            this.newPropName = param;
            this.lastState = State.PropName;
            this.state = State.RightMark;
        }


        /// <summary>
        /// Processes property name.
        /// Creates new proprty based on type.
        /// </summary>
        private void ProcessPropType(string param)
        {
            Property newProp = PropertyFactory.CreateProperty(param, this.newPropName);
            this.newTable.AddNewProperty(newProp);


            this.lastState = State.PropType;
            this.state = State.Comma;
        }
    }

    /// <summary>
    /// Creates edge lists from data file.
    /// We suppose vertices in datafile are stored based on their id in ascending order.
    /// We suppose edges in datafile are stored based on id of the from vertex in ascending order.
    /// That is to say, having three vertices with ids 1, 2, 3... first all edges are from vertex 1, then edges from vertex 2 etc. 
    /// </summary>
    class EdgeListProcessor : IProcessor<EdgeListHolder>
    {
        EdgeListHolder holder = new EdgeListHolder();

        List<Vertex> vertices;
        List<OutEdge> outEdges;
        List<InEdge> inEdges;
        
        Dictionary<string, Table> edgeTables;
        List<InEdge>[] incomingEdgesTable; // each vertex has a set of inwards edges
        
        bool finished;
        InEdge incomingEdge;
        OutEdge outEdge;

        enum State { ID, Type, Parameters, EdgeFromID, EdgeToID };
        State state;
        int paramsToReadLeft;

        public EdgeListProcessor()
        {
            this.outEdges = new List<OutEdge>();
            this.inEdges = new List<InEdge>();
            this.finished = false;
            this.state = State.ID;
            this.paramsToReadLeft = 0;
        }

        public bool Finished()
        {
            return this.finished;
        }

        public EdgeListHolder GetResult()
        {
            var tmp = new EdgeListHolder();
            tmp.outEdges = this.outEdges;
            tmp.inEdges = this.inEdges;
            return tmp;
        }

        public void PassParameters(params object[] prms)
        {
            this.edgeTables = (Dictionary<string, Table>)prms[1];
            this.vertices = (List<Vertex>)prms[2];
            InicialiseInEdgesTables();
        }

        public void Process(string param)
        {
          
                switch (state)
                {
                    case State.ID:
                        ProcessEdgeID(param);
                        break;
                    case State.Type:
                        ProcessEdgeType(param); 
                        break;
                    case State.Parameters:
                        ProcessParams(param, this.outEdge.table);
                        break;
                    case State.EdgeFromID:
                        ProcessEdgeFromID(param);
                        break;
                    case State.EdgeToID:
                        ProcessEdgeToID(param);
                        break;
                    default:
                       throw new ArgumentException($"{this.GetType()} Expected different state.");
                }
        }


        /// <summary>
        /// Processes id of an edge.
        /// Creates new outgoing edge. Next state is processing of a type.
        /// </summary>
        /// <param name="param"> ID of an edge</param>
        private void ProcessEdgeID(string param) 
        {
            if (param == null) { FinalizeInEdges(); FinalizeVertices(); this.finished = true; return; }

            int id = 0;
            if (!int.TryParse(param, out id))
                throw new ArgumentException($"{this.GetType()} Reading wrong node ID. ID is not a number.");

            this.outEdge = new OutEdge();
            this.outEdge.SetPositionInEdges(outEdges.Count);
            this.outEdge.AddID(id);
            this.state = State.Type;
        }
        
        /// <summary>
        /// Finds table assiciated with the edge and inserts the edge inside.
        /// Also sets the table for the edge.
        /// </summary>
        /// <param name="param"> Edge table.</param>
        private void ProcessEdgeType(string param)
        {
            Table table;
            edgeTables.TryGetValue(param, out table);
            this.outEdge.AddTable(table);
            this.outEdge.table.AddID(this.outEdge.id);
            this.state = State.EdgeFromID;
        }
        
        /// <summary>
        /// Find vertex the edge starts from. If edge processed is first edge of vertex, set edge position.
        /// Note the Count is pointing to the empty space where the processed edge will be added in FinishParams.
        /// Also sets values to the opposite edge.
        /// </summary>
        /// <param name="param"> ID of a start vertex.</param>
        private void ProcessEdgeFromID(string param) 
        {
            Vertex fromVertex = FindVertex(param);
            if (!fromVertex.HasOutEdges()) fromVertex.SetOutEdgesStartPosition(outEdges.Count);
            this.incomingEdge = new InEdge();
            this.incomingEdge.AddEndVertex(fromVertex);
            this.state = State.EdgeToID;
        }

        /// <summary>
        /// Finds end vertex of an edge and sets him to the end position of out edge.
        /// Finishes processing of in edge and adds it to the appropriate table of the vertex.
        /// </summary>
        /// <param name="param">ID of a end vertex.</param>
        private void ProcessEdgeToID(string param)
        {
            Vertex endVertex = FindVertex(param);
            this.outEdge.AddEndVertex(endVertex);

            this.incomingEdge.AddTable(this.outEdge.table);
            this.incomingEdge.AddID(this.outEdge.id);
            this.incomingEdgesTable[endVertex.GetPositionInVertices()].Add(this.incomingEdge);
            
            this.paramsToReadLeft = this.outEdge.table.GetPropertyCount();
            FinishParams();

        }

        /// <summary>
        ///Get the position of property where adding the parameter.
        ///Add the parameter there.
        /// </summary>
        /// <param name="param"> Property value to be parsed.</param>
        /// <param name="table"> Table of the edge.</param>
        private void ProcessParams(string param, Table table)
        {
            int accessedPropertyPosition = table.GetPropertyCount() - this.paramsToReadLeft;
            table.properties[accessedPropertyPosition].ParsePropFromStringToList(param);

            this.paramsToReadLeft--;
            FinishParams();
        }


        /// <summary>
        /// Either continues reading properties of the edge or starts reading new edge.
        /// </summary>
        private void FinishParams()
        {
            //For no more parameters to parse left
            if (this.paramsToReadLeft == 0)
            {
                this.outEdges.Add(this.outEdge);
                this.state = State.ID;
            }
            //continue parsing parameters
            else this.state = State.Parameters;
        }

        /// <summary>
        /// Finds vertex in a list based on a given ID.
        /// </summary>
        /// <param name="param"> ID of a vertex to be found</param>
        /// <returns> Vertex with given parameter.</returns>
        private Vertex FindVertex(string param)
        {
            int id = 0;
            if (!int.TryParse(param, out id))
                throw new ArgumentException($"{this.GetType()} Reading wrong node ID. ID is not a number.");
            Vertex vertex = this.vertices.Find(x => x.id == id);
            if (vertex == null) throw new ArgumentException($"{this.GetType()} ID is not found in vertices.");
            return vertex;
        }

        
        /// <summary>
        ///Merge results from inedges tables into one. -> Creates inEdges list.
        ///Set positions for inEdges field in vertices.
        ///Set positions for inEdges in their own list.
        /// </summary>
        private void FinalizeInEdges()
        {
            int count = 0;

            for (int i = 0; i < incomingEdgesTable.Length; i++)
            {
                int c = incomingEdgesTable[i].Count;
                if (c == 0) continue;
                vertices[i].SetInEdgesStartPosition(count);
                for (int k = 0; k < c; k++)
                    inEdges.Add(incomingEdgesTable[i][k]);
                count += c;
            }

            SetPositionsInListforInEdges();
        }



        /// <summary>
        /// For each edge from in edges, set its position in a list.
        /// </summary>
        private void SetPositionsInListforInEdges()
        {
            for (int i = 0; i < inEdges.Count; i++)
            {
                inEdges[i].SetPositionInEdges(i);
            }
        }


        /// <summary>
        /// Creates list for each vertex. Each table will include incoming edges.
        /// </summary>
        private void InicialiseInEdgesTables()
        {
            this.incomingEdgesTable = new List<InEdge>[vertices.Count];
            for (int i = 0; i < incomingEdgesTable.Length; i++)
            {
                incomingEdgesTable[i] = new List<InEdge>();
            }
        }

        /// <summary>
        /// Set count on in/out edges.
        /// </summary>
        private void FinalizeVertices()
        {
            for (int i = 0; i < vertices.Count; i++)
            {
                vertices[i].outEdgesEndPosition = FindEndPositionOfEdges(isOut: true, i);
                vertices[i].inEdgesEndPosition = FindEndPositionOfEdges(isOut: false, i);
            }
        }
       
        
        /// <summary>
        /// Based on a given vertex we set ending positions of in/out edges in their lists.
        /// </summary>
        /// <param name="isOut"> Wheter we are setting in or out edges.</param>
        /// <param name="p"> Position of a processed vertex.</param>
        /// <returns></returns>
        private int FindEndPositionOfEdges(bool isOut, int p)
        {
            if ((isOut)&&(!vertices[p].HasOutEdges())) return -1;
            else if((!isOut)&&(!vertices[p].HasInEdges())) return -1;
            
            for (int k = p + 1; k < vertices.Count; k++)
            {
                Vertex v = vertices[k];
                int t = isOut ? v.outEdgesStartPosition : v.inEdgesStartPosition;
                if (t != -1) return t;
            }

            if (isOut) return outEdges.Count;
            else return inEdges.Count;
        }
    }


    /// <summary>
    /// Creates vertices list from a file.
    /// </summary>
    class VerticesListProcessor : IProcessor<List<Vertex>>
    {
        List<Vertex> vertices;
        Dictionary<string, Table> nodeTables;
        bool finished;

        Vertex vertex;
        enum State { ID, Type, Parameters};
        State state;
        int paramsToReadLeft;

        public VerticesListProcessor()
        {
            this.vertices = new List<Vertex>();
            this.finished = false;
            this.state = State.ID;
            this.paramsToReadLeft = 0;
        }

        public bool Finished()
        {
            return this.finished;
        }

        public List<Vertex> GetResult()
        {
            return this.vertices;
        }

        public void PassParameters(params object[] prms)
        {
            this.nodeTables = (Dictionary<string, Table>)prms[0];
        }

        public void Process(string param)
        {

            switch (state)
            {
                case State.ID:
                    ProcessNodeID(param);
                    break;
                case State.Type:
                    ProcessNodeType(param);
                    break;
                case State.Parameters:
                    ProcessParams(param, this.vertex.table);
                    break;
                default:
                    throw new ArgumentException($"{this.GetType()} Expected different state.");
            }
        }


        /// <summary>
        /// First state of processor. Tries to parse ID of a node and inits a new vertex.
        /// After parsing ID, the type of node is a next state.
        /// </summary>
        /// <param name="param">ID of a node.</param>
        private void ProcessNodeID(string param)
        {
            if (param == null)
            {
                this.finished = true; 
                return;
            };

            int id = 0;
            if (!int.TryParse(param, out id))
                throw new ArgumentException($"{this.GetType()} Reading wrong node ID. ID is not a number.");

            this.vertex = new Vertex();
            this.vertex.SetPositionInVertices(vertices.Count);
            this.vertex.AddID(id);
            this.state = State.Type;
        }


        /// <summary>
        /// Finds table based on a parameter and set it to a node.
        /// Also inserts ID of the node into the table.
        /// Next state should parse data of the node.
        /// </summary>
        /// <param name="param"> Type of a node. Reffers to a table.</param>
        private void ProcessNodeType(string param)
        {
                
            Table table;
            nodeTables.TryGetValue(param, out table);
            this.vertex.AddTable(table);
            this.vertex.table.AddID(this.vertex.id);

            this.paramsToReadLeft = this.vertex.table.GetPropertyCount();
            FinishParams();

        }

        /// <summary>
        /// Gets position of accessed property and parses its value to its list.
        /// </summary>
        /// <param name="param"> Parameter of a node.</param>
        /// <param name="table"> Table of a proccessed node.</param>
        private void ProcessParams(string param, Table table)
        {
            // Get position of accessed property and insert given parameter to appropriate list.
            int accessedPropertyPosition = table.GetPropertyCount() - this.paramsToReadLeft;
            table.properties[accessedPropertyPosition].ParsePropFromStringToList(param);

            this.paramsToReadLeft--;
            FinishParams();
        }

        /// <summary>
        /// If reading of paramters of the node was finished next state if ID, that is reading a new node.
        /// Otherwise, we continue reading next parameters.
        /// </summary>
        private void FinishParams()
        {
            // For no more parameters to parse left
            if (this.paramsToReadLeft == 0)
            {
                this.vertices.Add(this.vertex);
                this.state = State.ID;
            }
            // Continue parsing parameters
            else this.state = State.Parameters;
        }
    }

}
