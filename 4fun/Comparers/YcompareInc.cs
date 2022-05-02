using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _4fun.Comparers
{
    public class YcompareInc : Comparer<(int, int)>
    {
        public override int Compare((int, int) x, (int, int) y)
        {
            return x.Item2.CompareTo(y.Item2);
        }
    }
}
