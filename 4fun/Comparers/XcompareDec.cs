using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _4fun.Comparers
{
    public class XcompareDec : Comparer<(int, int)>
    {
        public override int Compare((int, int) x, (int, int) y)
        {
            return -x.Item1.CompareTo(y.Item1);
        }
    }
}
