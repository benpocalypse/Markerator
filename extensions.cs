using System;

namespace com.github.benpocalypse
{
    public static class MarkeratorExtensions
    {
       public static void IfTrue(this bool val, Action then)
        {
            if (val == true)
                then();
        }
    }
}