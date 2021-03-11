using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QueryEngine;

namespace Benchmark
{
    class Mode
    {
        public HashSet<GrouperAlias> groupers;
        public HashSet<SorterAlias> sorters;
        public QueryMode modeType;
        public GrouperAlias baseGrouper;
        public SorterAlias baseSorter;
    }

    class Normal : Mode
    {
        public Normal()
        {
            groupers = Aliases.NormalGroupers;
            sorters = Aliases.NormalSorters;
            modeType = QueryMode.Normal;

            baseGrouper = GrouperAlias.RefB;
            baseSorter = SorterAlias.MergeSort;
        }
    }

    class HalfStreamed : Mode
    {
        public HalfStreamed()
        {
            groupers = Aliases.HalfStreamedGroupers;
            sorters = Aliases.HalfStreamedSorters;
            modeType = QueryMode.HalfStreamed;

            baseGrouper = GrouperAlias.TwoStepHSB;
            baseSorter = SorterAlias.AbtreeHS;
        }
    }

    class Streamed : Mode
    {
        public Streamed()
        {
            groupers = Aliases.StreamedGroupers;
            sorters = Aliases.StreamedSorters;
            modeType = QueryMode.Streamed;

            baseGrouper = GrouperAlias.GlobalS;
            baseSorter = SorterAlias.AbtreeS;
        }
    }
}
