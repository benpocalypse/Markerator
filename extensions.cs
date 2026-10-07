namespace Markerator;

/// <summary>
/// These are some quality-of-life and readability extension functions that are levraged to make the code
/// more explicit in its intenions.
/// </summary>
public static class MarkeratorExtensions
{
    /// <summary>
    /// Returns the value for the given key if present, otherwise the supplied
    /// default. Useful for dictionary-based lookups where a default is needed.
    /// </summary>
    /// <typeparam name="TKey">The dictionary key type.</typeparam>
    /// <typeparam name="TValue">The dictionary value type.</typeparam>
    /// <param name="dictionary">The source dictionary.</param>
    /// <param name="key">The key to look up.</param>
    /// <param name="defaultValue">The value to return if the key is absent.</param>
    /// <returns>The stored value, or <paramref name="defaultValue"/>.</returns>
    public static TValue GetValueOrDefault<TKey, TValue>(
        this IDictionary<TKey, TValue> dictionary,
        TKey key,
        TValue defaultValue)
    {
        return dictionary.TryGetValue(key, out var value) ? value : defaultValue;
    }
    
    /// <summary>
    /// An extension method for boolean variables that makes
    /// checking for a True value more explicit.
    /// </summary>
    /// <param name="val">A boolean variable</param>
    /// <param name="then">The action to be performed if the value is True</param>
    public static bool IfTrue(this bool val, Action then)
    {
        if (val)
        {
            then();
            return true;
        }

        return false;
    }

    /// <summary>
    /// An extension method for boolean variables that will return true if the value is true, or false otherwise.
    /// </summary>
    /// <param name="val">The boolean value</param>
    /// <returns>the same as the input</returns>
    public static bool IfTrue(this bool val) => val;

    /// <summary>
    /// An extension method for boolean variables that makes
    /// checking for a False value more explicit.
    /// </summary>
    /// <param name="val">A boolean variable</param>
    /// <param name="then">The action to be performed if the value is False</param>
    public static bool IfFalse(this bool val, Action then)
    {
        if (!val)
        {
            then();
            return true;
        }

        return false;
    }

    /// <summary>
    /// An extension method in that performs an action no matter what the value. This is intended to be chained with
    /// a call to either .IfTrue() or .IfFalse()
    /// </summary>
    /// <param name="val">The boolean value to be evaluated</param>
    /// <param name="then">The action to be performed no matter if val is true or false</param>
    public static bool OrElse(this bool val, Action then)
    {
        then();
        return val;
    }

    /// <summary>
    /// An extension method intended for any List-type variable that
    /// performs an action if the List.Count() > 0.
    /// </summary>
    /// <param name="list">The List variable</param>
    /// <param name="then">The action to be taken if the List is not empty</param>
    /// <typeparam name="T">A generic that allows this extension to work with any IReadOnlyList type</typeparam>
    public static void IfNotEmpty<T>(this IReadOnlyList<T> list, Action then)
    {
        if (list.Count > 0)
        {
            then();
        }
    }

    /// <summary>
    /// Another extension method for any nullable List-type variable that perfors an action
    /// if the List is both not null and not empty. 
    /// </summary>
    /// <param name="list">The List variable</param>
    /// <param name="then">The action to be taken if the List is not null and not empty</param>
    /// <typeparam name="T">A generic that allows this extension to work with any IList type</typeparam>
    public static void IsNullOrEmpty<T>(this IList<T>? list, Action then)
    {
        if (list != null || list?.Count != 0)
        {
            then();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="val"></param>
    /// <param name="then"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static bool IsNotNull<T>(this IEnumerable<T> val, Action then)
    {
        if (val != null)
        {
            then();
            return true;
        }
        else
        {
            return false;
        }
    }
}
