﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    internal abstract class Sorter
    {
        public abstract ITableResults Sort();
    }
}
