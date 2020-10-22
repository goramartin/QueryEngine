using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// Class serves as a information collector for aggregate clasuse.
    /// If the group by clause is defined. In the query, there can be referenced only
    /// the same expressions as in the group clause or an aggregate functions.
    /// How ever, if the group by clause is not in the query but aggregates are inputed.
    /// it still collects the aggregates. 
    /// 
    /// When collecting the aggregations and expressions, this class provides data to check
    /// against the correctness of the other expressions in the query.
    /// </summary>
    internal class AggregationInfo
    {
        public List<ExpressionHolder> hashExpr = new List<ExpressionHolder>();
        public List<Aggregate> aggregates = new List<Aggregate>();
        public bool IsSetGroupBy { get; private set; } = false;
        public bool FoundNonAggregate { get; private set; } = false;
    
        public bool IsExprValid(ExpressionHolder holder)
        {
            throw new NotImplementedException();
        }

        public bool AddAggregate(Aggregate aggregate)
        {
            throw new NotImplementedException();
        }


    }
}
