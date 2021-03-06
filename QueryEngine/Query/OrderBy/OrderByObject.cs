﻿/*! \file 
This file includes a definition of a order by object.
It's purpose is to contain information about sorting of results from a query.
It contains a list of comparers that will be used during sorting.

Sorting is done with the help of HPC sharp library Merge sort in both parallel and single thread cases.
Merge sort is chosen because it does the least amount of comparisons. The comparisons are really expensive
because the database needs to be accessed and expressions must be computed in order to compare the results rows.

The ordering works as follows.
Firstly, the array of integers is created where each index represents a index to the results table (just like pointers).
The array is then passed to the sorter with comparer. Each time the comparer compares the indeces the actual rows are
compared instead. It saves a lot of time because moving rows in a table is very time consuming.
 */


using System;
using System.Collections.Generic;

namespace QueryEngine
{
    /// <summary>
    /// A class represents order by part of a query.
    /// It sorts given results with defined comparers.
    /// </summary>
    internal sealed class OrderByObject : QueryObject
    {
        private ExpressionComparer[] comparers;
        private IOrderByExecutionHelper helper;

        /// <summary>
        /// Creates an order by object.
        /// </summary>
        /// <param name="graph"> A graph the query is computed on. </param>
        /// <param name="variableMap"> A map of query variables. </param>
        /// <param name="executionHelper"> An order by execution helper. </param>
        /// <param name="orderByNode"> A parse tree of order by expression. </param>
        /// <param name="exprInfo"> A query expression information. </param>
        public OrderByObject(Graph graph, VariableMap variableMap, IOrderByExecutionHelper executionHelper, OrderByNode orderByNode, QueryExpressionInfo exprInfo)
        {
            if (executionHelper == null || orderByNode == null || variableMap == null || graph == null || exprInfo == null)
                throw new ArgumentNullException($"{this.GetType()}, passing null arguments to the constructor.");
            
            this.helper = executionHelper;

            var orderByVisitor = new OrderByVisitor(graph.labels, variableMap, exprInfo);
            orderByVisitor.Visit(orderByNode);
            var comps = orderByVisitor.GetResult();

            executionHelper.IsSetOrderBy = true;
            this.comparers = comps.ToArray();
        }

        public override void Compute(out ITableResults resTable, out GroupByResults groupByResults)
        {
            if (this.next != null)
            {
                this.next.Compute(out resTable, out groupByResults);
                this.next = null;
                if (resTable == null || groupByResults != null) 
                    throw new ArgumentNullException($"{this.GetType()}, table results are set to null or groupByResults are not null.");

                if (this.helper.IsStoringResult) this.Sort(resTable);
            }
            else throw new NullReferenceException($"{this.GetType()}, next is set to null.");
        }

        /// <summary>
        /// Sorts given data.
        /// </summary>
        /// <param name="resTable"> Query reults to be sorted. </param>
        /// <returns> The sorted data. </returns>
        private ITableResults Sort(ITableResults resTable)
        {
             ISorter sorter = new MultiColumnTableSorter(resTable, this.comparers, this.helper.InParallel);
             var sortedResults =  sorter.Sort();
             return sortedResults;
        }
    }
}
