/*! \file 
The file contains aliases of the used solutions to group by and order by.
Each mode of the engine contains a hash table of valid group by/order by solutions.
 */

using System.Collections.Generic;

namespace QueryEngine
{
    /// <summary>
    /// Aliases of solutions for group by.
    /// </summary>
    public enum GrouperAlias
    {
        // Normal

        /// <summary>
        /// Reference single thread solution using Buckets.
        /// </summary>
        RefB,
        /// <summary>
        /// Reference Single thread solution using Lists.
        /// </summary>
        RefL,
        /// <summary>
        /// A parallel solution grouping results into a global dictionary using Buckets.
        /// </summary>
        GlobalB,
        /// <summary>
        /// A parallel solution grouping results into a global dictionary using Lists.
        /// </summary>
        GlobalL,
        /// <summary>
        /// A parallel solution grouping results locally and subsequently merging the results in pairs, again, locally using Buckets.
        /// </summary>
        LocalB,
        /// <summary>
        /// A parallel solution grouping results locally and subsequently merging the results in pairs, again, locally using Lists.
        /// </summary>
        LocalL,
        /// <summary>
        /// A parallel solution grouping results locally and subsequently merging the results into global dictionary using Buckets.
        /// </summary>
        TwoStepB,
        /// <summary>
        /// A parallel solution grouping results locally and subsequently merging the results into global dictionary using Lists.
        /// </summary>
        TwoStepL,

        // Half-Streamed

        /// <summary>
        /// A half streamed version of the TwoStepB version. Both parallel and single thread.
        /// </summary>
        TwoStepHSB,
        /// <summary>
        /// A half streamed version of the TwoStepL version. Both parallel and single thread.
        /// </summary>
        TwoStepHSL,

        // Streamed

        /// <summary>
        /// A half streamed version of the GlobalB version. Both parallel and single thread.
        /// </summary>
        GlobalS
    }

    /// <summary>
    /// Aliases of solutions for order by.
    /// </summary>
    public enum SorterAlias
    {
        // Normal

        /// <summary>
        /// Both parallel and single thread.
        /// </summary>
        MergeSort,

        // Half-Streamed

        /// <summary>
        /// Both parallel and single thread.
        /// A sort algortihm utilizing AB trees. Sorting is done locally and subsequently merged in pairs using parallel merge.
        /// </summary>
        AbtreeHS,

        /// <summary>
        /// Both parallel and single thread.
        /// Same as the AbtreeHS, except that instead of using general ab tree it accumulates values that are repetitive into a List intead of inserting it into the tree.
        /// </summary>
        AbtreeAccumHS,

        // Streamed

        /// <summary>
        /// Both parallel and single thread.
        /// The first key of the sort is devided into ranges, each range represents a bucket. The results in the bucket are sorted using AB trees.
        /// </summary>
        AbtreeS,

        /// <summary>
        /// Both parallel and single thread.
        /// Same as the AbtreeS, except that instead of using general ab tree it accumulates values that are repetitive into a List intead of inserting it into the tree.
        /// </summary>
        AbtreeAccumS

    }

    /// <summary>
    /// A class containing group alises for each mode.
    /// </summary>
    public static class Aliases
    {
        public static HashSet<GrouperAlias> NormalGroupers = new HashSet<GrouperAlias>
        {
              GrouperAlias.RefB,
              GrouperAlias.RefL,
              GrouperAlias.LocalB,
              GrouperAlias.LocalL,
              GrouperAlias.TwoStepB,
              GrouperAlias.TwoStepL,
              GrouperAlias.GlobalB,
              GrouperAlias.GlobalL
        };

        public static HashSet<GrouperAlias> HalfStreamedGroupers = new HashSet<GrouperAlias>
        {
            GrouperAlias.TwoStepHSB,
            GrouperAlias.TwoStepHSL
        };

        public static HashSet<GrouperAlias> StreamedGroupers = new HashSet<GrouperAlias>
        {
            GrouperAlias.GlobalS
        };

        public static HashSet<SorterAlias> StreamedSorters = new HashSet<SorterAlias>
        {
            SorterAlias.AbtreeS,
            SorterAlias.AbtreeAccumS
        };
        public static HashSet<SorterAlias> HalfStreamedSorters = new HashSet<SorterAlias>
        {
            SorterAlias.AbtreeHS,
            SorterAlias.AbtreeAccumHS

        };
        public static HashSet<SorterAlias> NormalSorters = new HashSet<SorterAlias>
        {
            SorterAlias.MergeSort
        };
    }
}