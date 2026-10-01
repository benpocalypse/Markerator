namespace Markerator.Helpers;

public sealed class DuplicateKeyComparer<TKey> : IComparer<TKey> where TKey : IComparable
{
    public int Compare(TKey? x, TKey? y)
    {
        int result = x!.CompareTo(y);

        return result == 0 ? 1 : // Handle equality as being greater than
            result;
    }
}
