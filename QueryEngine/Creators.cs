using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{

    //Interface for state oriented processing of incoming words from file.
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

    //Class takes reader and processor that proccesses words from reader.
    //Main purpose is to have a general way to create classes from files.
    class CreatorFromFile<T> : ICreator<T>
    {
        IReader reader;
        IProcessor<T> processor;

        public CreatorFromFile(IReader reader, IProcessor<T> processor)
        {
            this.reader = reader;
            this.processor = processor;
        }

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


    //Creates distionary/map from data scheme with specific nodes in the graph.
    class TableDictProcessor : IProcessor<Dictionary<string, Table>>
    {
        Dictionary<string, Table> dict;
        Table newTable;
        string newPropName;
        bool finished;
        bool readingName;
        enum State { Kind, Name, LeftBracket, PropName, PropType, DoubleDot };
        State state;

        public TableDictProcessor()
        {
            this.dict = new Dictionary<string, Table>();
            this.finished = false;
            this.state = State.LeftBracket;
            this.newTable = null;
            this.newPropName = null;
            this.readingName = false;
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
                case State.LeftBracket:
                    ProcessLeftBracket(param);
                    break;
                case State.Kind:
                    ProcessKind(param);
                    break;
                case State.Name:
                    ProcessName(param);
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
                default:
                    throw new ArgumentException($"{this.GetType()} Expected different state.");
            }
        }


        public void PassParameters(params object[] prms)
        {
            this.dict = (Dictionary<string, Table>)prms[0];
        }



        private void ProcessLeftBracket(string param)
        { 
            if (param == null) 
            {
                this.finished = true; 
                return;
            };
            
            if (param != "{") throw new ArgumentException($"{this.GetType()} Expected left Bracket");
            this.state = State.Kind;
        }

        private void ProcessKind(string param)
        {
            if (param != "Kind") 
                throw new ArgumentException($"{this.GetType()} Expected Kind");
            this.readingName = true;
            this.state = State.DoubleDot;
        }

        //If it is null we finished reading the file.
        //Else it creates new table with the param name and add it to dictionary.
        private void ProcessName(string param) 
        {
            this.newTable = new Table(param);
            if (this.dict.ContainsKey(param)) 
                throw new ArgumentException($"{this.GetType()} Adding table that exists.");
            else this.dict.Add(param, this.newTable);
            this.state = State.PropName;
        }

        //Expected to read name or }
        private void ProcessPropName(string param)
        {
            if (param == "}")
            {
                this.state = State.LeftBracket;
                return;
            }
            else 
            {
                this.newPropName = param;
                this.state = State.DoubleDot;
            }
        }

        //Expected to get character :.
        private void ProcessDoubleDot(string param)
        {
            if (param != ":") throw new ArgumentException($"{this.GetType()} Expected DoubleDot.");
            if (this.readingName)
            { 
                this.state = State.Name;
                this.readingName = false;
                return;
            }
            else this.state = State.PropType;
        }

        //Based on the incoming parameter, create new instance of property through PropertyFactory.
        //Add it to the tables list.
        private void ProcessPropType(string param)
        {
            Property newProp = PropertyFactory.CreateProperty(param, this.newPropName);
            this.newTable.AddNewProperty(newProp);
            this.state = State.PropName;
        }
    }

    //Creates edge list from data file.
    //We suppose vertices in datafile are stored based on their id in ascending order.
    //We suppose edges in datafile are stored based on id of the from vertex in ascending order.
    //That is to say, having three vertices with ids 1, 2, 3... first all edges are from vertex 1, then edges from vertex 2 etc. 
    class EdgeListProcessor : IProcessor<EdgeListHolder>
    {
        List<Vertex> vertices;
        List<Edge> outEdges;
        List<Edge> inEdges;
        Dictionary<string, Table> nodeTables;
        Dictionary<string, Table> edgeTables;
        bool finished;
        bool readingNodes;
        Edge incomingEdge;


        Vertex vertex;
        Edge edge;
        enum State { ID, Type, Parameters, EdgeFromID, EdgeToID };
        State state;
        int paramsToReadLeft;


        public EdgeListProcessor()
        {
            this.vertices = new List<Vertex>();
            this.outEdges = new List<Edge>();
            this.inEdges = new List<Edge>();
            this.finished = false;
            this.readingNodes = true;
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
            tmp.vertices = this.vertices;
            tmp.outEdges = this.outEdges;
            tmp.inEdges = this.inEdges;
            return tmp;
        }

        public void PassParameters(params object[] prms)
        {
            this.nodeTables = (Dictionary<string, Table>)prms[0];
            this.edgeTables = (Dictionary<string, Table>)prms[1];
        }

        public void Process(string param)
        {
          
                switch (state)
                {
                    case State.ID:
                        if (this.readingNodes) ProcessNodeID(param); else { ProcessEdgeID(param); }
                        break;
                    case State.Type:
                        if (this.readingNodes) ProcessNodeType(param); else { ProcessEdgeType(param); }
                        break;
                    case State.Parameters:
                        if (this.readingNodes) ProcessParams(param, this.vertex.table); 
                        else { ProcessParams(param, this.edge.table); }
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

        //Parse id. Create new Vertex Instance. Ad id into the instance.
        private void ProcessNodeID(string param)
        {
            if (param == ":") { this.readingNodes = false; return; };

            int id = 0;
            if (!int.TryParse(param, out id)) 
                throw new ArgumentException($"{this.GetType()} Reading wrong node ID. ID is not a number.");

            this.vertex = new Vertex();
            this.vertex.SetPositionInVertices(vertices.Count);
            this.vertex.AddID(id);
            this.state = State.Type;
        }

        //Parse table name. Get the table name. Add table from dictionary into vertex.
        //Add vertex id into the table IDs and actualise parameter count for next reading.
        private void ProcessNodeType(string param)
        {
            Table table;
            nodeTables.TryGetValue(param, out table);
            this.vertex.AddTable(table);
            this.vertex.table.AddID(this.vertex.id);
                       
            this.paramsToReadLeft = this.vertex.table.GetPropertyCount();
            FinishParams();
           
        }


        //Reading file after :.
        //Null means end of file.
        //Parse id of the edge and create new edge with this id.
        private void ProcessEdgeID(string param) 
        {
            if (param == null) { FinalizeInEdges(); this.finished = true; return; }

            int id = 0;
            if (!int.TryParse(param, out id))
                throw new ArgumentException($"{this.GetType()} Reading wrong node ID. ID is not a number.");

            this.edge = new Edge();
            this.edge.AddID(id);
            this.state = State.Type;
        }
        
        //Find correct table of the edge based on input type.
        //Add this table to the edge and acualise IDs in the table with the edge id.
        private void ProcessEdgeType(string param)
        {
            Table table;
            edgeTables.TryGetValue(param, out table);
            this.edge.AddTable(table);
            this.edge.table.AddID(this.edge.id);
            this.state = State.EdgeFromID;
        }
        //Find vertex the edge starts from. If edge processed is first edge of vertex, set edge position.
        //Note the Count is pointing to the empty space where the processed edge will be added in FinishParams.
        private void ProcessEdgeFromID(string param) 
        {
            Vertex fromVertex = FindVertex(param);
            if (!fromVertex.HasEdges()) fromVertex.SetOutEdgePosition(outEdges.Count);
            this.incomingEdge = new Edge();
            this.incomingEdge.AddEndVertex(fromVertex);
            this.state = State.EdgeToID;
        }

        //Parse Id of the end vertex.
        //Find the vertex in the list vertices.
        //Add found vertex to the edge.
        //Create incoming edge
        //Actualise if more parameters are needed for the edge.
        private void ProcessEdgeToID(string param)
        {
            Vertex endVertex = FindVertex(param);
            this.edge.AddEndVertex(endVertex);

            this.incomingEdge.AddTable(this.edge.table);
            this.incomingEdge.AddID(this.edge.id);
            inEdges.Add(this.incomingEdge);
            
            this.paramsToReadLeft = this.edge.table.GetPropertyCount();
            FinishParams();

        }

        //Get the position of property where adding the parameter.
        //Add the parameter there.
        private void ProcessParams(string param, Table table)
        {

            int accessedPropertyPosition = table.GetPropertyCount() - this.paramsToReadLeft;
            table.properties[accessedPropertyPosition].ParsePropFromStringToList(param);

            this.paramsToReadLeft--;
            FinishParams();
        }

        private void FinishParams()
        {
            //For no more parameters to parse left
            if (this.paramsToReadLeft == 0)
            {
                if (this.readingNodes) this.vertices.Add(this.vertex);
                else this.outEdges.Add(this.edge);
                this.state = State.ID;
            }
            //continue parsing parameters
            else this.state = State.Parameters;
        }

        private Vertex FindVertex(string param)
        {
            int id = 0;
            if (!int.TryParse(param, out id))
                throw new ArgumentException($"{this.GetType()} Reading wrong node ID. ID is not a number.");
            Vertex vertex = this.vertices.Find(x => x.id == id);
            if (vertex == null) throw new ArgumentException($"{this.GetType()} ID is not found in vertices.");
            return vertex;
        }

        //List InEdges is unsorted, we sort it based on endvertices, because vertices in list vertices are ordered based on their id
        //which makes them ascending.
        //Add in edge position to each vertex.
        private void FinalizeInEdges()
        {
            this.inEdges.Sort((x, y) => (x.endVertex.id.CompareTo(y.endVertex.id)));
            int tmpId = -1;
            for (int i = 0; i < inEdges.Count; i++)
            {
                Vertex v = inEdges[i].GetEndVertex();
                if (tmpId != v.GetID())
                {
                    tmpId = v.GetID();
                    v.SetInEdgePosition(i);
                }
            }


        }

    }




}
