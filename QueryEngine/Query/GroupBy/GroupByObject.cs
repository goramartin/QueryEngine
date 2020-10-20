/*! \file

This file contains definition of a group by object.


*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    internal sealed class GroupByObject : QueryObject
    {
        // TODO add exuality comparer, hshar and result tables


        public override void Compute(out ITableResults results)
        {
            throw new NotImplementedException();
        }
    }
}
