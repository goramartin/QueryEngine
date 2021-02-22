using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    public enum GrouperAlias
    {
        RefB,
        RefL,
        GlobalB,
        GlobalL,
        LocalB,
        LocalL,
        TwowayB,
        TwowayL,

        TwowayHSB,
        TwowayHSL,

        GlobalS
    }

    public enum SorterAlias
    {
        MergeSort,
        AbtreeHS,
        AbtreeS
    }

    public static class Aliases
    {
        public static HashSet<GrouperAlias> NormalGroupers = new HashSet<GrouperAlias>
        {
              GrouperAlias.TwowayB,
              GrouperAlias.RefB,
              GrouperAlias.RefL,
              GrouperAlias.GlobalB,
              GrouperAlias.GlobalL,
              GrouperAlias.LocalB,
              GrouperAlias.LocalL,
              GrouperAlias.TwowayL,
        };

        public static HashSet<GrouperAlias> HalfStreamedGroupers = new HashSet<GrouperAlias>
        {
            GrouperAlias.TwowayHSB,
            GrouperAlias.TwowayHSL,
        };

        public static HashSet<GrouperAlias> StreamedGroupers = new HashSet<GrouperAlias>
        {
            GrouperAlias.GlobalS
        };

        public static HashSet<SorterAlias> StreamedSorters = new HashSet<SorterAlias>
        {
            SorterAlias.AbtreeS
        };
        public static HashSet<SorterAlias> HalfStreamedSorters = new HashSet<SorterAlias>
        {
            SorterAlias.AbtreeHS
        };
        public static HashSet<SorterAlias> NormalSorters = new HashSet<SorterAlias>
        {
            SorterAlias.MergeSort
        };
    }
}