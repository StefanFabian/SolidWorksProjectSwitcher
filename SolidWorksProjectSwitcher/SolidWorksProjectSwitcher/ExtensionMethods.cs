using System.Collections.Specialized;

namespace SolidWorksProjectSwitcher
{
    public static class ExtensionMethods
    {
        public static string LastOrDefault(this StringCollection collection)
            => collection == null || collection.Count == 0 ? null : collection[collection.Count - 1];
    }
}
