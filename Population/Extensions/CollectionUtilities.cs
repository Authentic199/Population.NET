namespace Population.Extensions;

//
// Summary:
//     A class which contains useful methods for processing collections.
public static class CollectionUtilities
{
    //
    // Summary:
    //     Checks whether enumerable is null or empty.
    //
    // Parameters:
    //   enumerable:
    //     The System.Collections.Generic.IEnumerable`1 to be checked.
    //
    // Type parameters:
    //   T:
    //     The type of the enumerable.
    //
    // Returns:
    //     True if enumerable is null or empty, false otherwise.
    public static bool IsNullOrEmpty<T>(this IEnumerable<T>? enumerable)
    {
        if (enumerable != null)
        {
            return !enumerable.Any();
        }

        return true;
    }
}
