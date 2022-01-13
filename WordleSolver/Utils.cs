using System;
using System.Collections.Generic;
using System.Text;

namespace WordleSolver
{
    public static class Utils
    {
        static Random random = new Random();

        public static T SelectRandom<T> (this IList<T> list)
        {
            return list[random.Next(list.Count)];
        }
    }
}
