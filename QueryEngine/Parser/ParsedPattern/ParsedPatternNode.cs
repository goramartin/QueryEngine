/*! \file
This file includes definitions of parsed pattern nodes.
Parsed pattern nodes represent single elements that will be matched during matching algorithm.
For example if the user inputs pattern (a) -> (q) - (p)
The parsed pattern nodes inside parsed pattern class will be : vertex parsed node, out edge parsed node etc..
These nodes are subsequently used for creating appropriate base matches during the final pattern creation.
 */



using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// Represents a single element to match when parsing match expression.
    /// </summary>
    internal abstract class ParsedPatternNode
    {
        public bool IsAnonymous { get; set; }
        /// <summary>
        /// A type of the element from the Labeled-property graph..
        /// </summary>
        public Table Table { get; set; }
        /// <summary>
        /// If is set, then this node represents a variable.
        /// </summary>
        public string Name { get; set; }

        public ParsedPatternNode()
        {
            this.Table = null;
            this.Name = null;
            this.IsAnonymous = true;
        }

        /// <summary>
        /// Creates a copy of an instance with reversed edges.
        /// </summary>
        public ParsedPatternNode CloneReverse()
        {
            ParsedPatternNode clone = ParsedPatternNode.ParsedPatternNodeFactoryReverse(this.GetType());
            clone.Name = this.Name;
            clone.Table = this.Table;
            clone.IsAnonymous = this.IsAnonymous;
            return clone;
        }

        public override bool Equals(object obj)
        {
            if (obj is ParsedPatternNode)
            {
                var o = obj as ParsedPatternNode;
                if (this.IsAnonymous && o.IsAnonymous) return false;
                else if (this.Name != o.Name) return false;
                else if (this.Table != o.Table) return false;
                else return true;
            }
            return false;
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Create a parsed pattern node with reversed edges. Other nodes are let same as before.
        /// Used only during clone reverse method.
        /// </summary>
        /// <param name="type"> A type of the parsed pattern node. </param>
        /// <returns> A reverse clone of the passed type. </returns>
        private static ParsedPatternNode ParsedPatternNodeFactoryReverse(Type type)
        {
            if (type == typeof(VertexParsedPatternNode)) return new VertexParsedPatternNode();
            else if (type == typeof(InEdgeParsedPatternNode)) return new OutEdgeParsedPatternNode();  // reversing in edge to out edge
            else if (type == typeof(OutEdgeParsedPatternNode)) return new InEdgeParsedPatternNode();  // reversing out edge to in edge
            else if (type == typeof(AnyEdgeParsedPatternNode)) return new AnyEdgeParsedPatternNode(); // reversing any edge produces the same edge type
            else throw new TypeAccessException($"ParsedPatternFactoryReverse, factory method does not contain accesing type. Type = {type}.");
        }

        /// <summary>
        /// A factory method for parsed pattern nodes.
        /// </summary>
        /// <param name="type"> A type of a parsed pattern node to create reverse of.</param>
        /// <returns> A parsed pattern node based on specified type. </returns>
        public static ParsedPatternNode ParsedPatternNodeFactory(Type type)
        {
            if (type == typeof(VertexParsedPatternNode)) return new VertexParsedPatternNode();
            else if (type == typeof(InEdgeParsedPatternNode)) return new InEdgeParsedPatternNode();
            else if (type == typeof(OutEdgeParsedPatternNode)) return new OutEdgeParsedPatternNode();
            else if (type == typeof(AnyEdgeParsedPatternNode)) return new AnyEdgeParsedPatternNode();
            else throw new TypeAccessException($"ParsedPatternFactory, factory method does not contain accesing type. Type = {type}.");
        }
    }


    internal sealed class VertexParsedPatternNode : ParsedPatternNode
    {
        public override bool Equals(object obj)
        {
            VertexParsedPatternNode tmp = obj as VertexParsedPatternNode;
            if (tmp == null) return false;
            else return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }

    }

    internal abstract class EdgeParsedPatternNode : ParsedPatternNode 
    {
        public override bool Equals(object obj)
        {
            EdgeParsedPatternNode tmp = obj as EdgeParsedPatternNode;
            if (tmp == null) return false;
            else return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }
    }

    internal sealed class InEdgeParsedPatternNode : EdgeParsedPatternNode
    {
        public override bool Equals(object obj)
        {
            InEdgeParsedPatternNode tmp = obj as InEdgeParsedPatternNode;
            if (tmp == null) return false;
            else return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }
    }

    internal sealed class OutEdgeParsedPatternNode : EdgeParsedPatternNode
    {
        public override bool Equals(object obj)
        {
            OutEdgeParsedPatternNode tmp = obj as OutEdgeParsedPatternNode;
            if (tmp == null) return false;
            else return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }
    }

    internal sealed class AnyEdgeParsedPatternNode : EdgeParsedPatternNode
    {
        public override bool Equals(object obj)
        {
            AnyEdgeParsedPatternNode tmp = obj as AnyEdgeParsedPatternNode;
            if (tmp == null) return false;
            else return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }
    }
}
