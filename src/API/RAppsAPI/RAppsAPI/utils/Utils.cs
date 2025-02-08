namespace RAppsAPI.utils
{
    public class Utils
    {
        public static string ReplaceLastOccurrence(string text, string find, string replace)
        {
            var lastIndex = text.LastIndexOf(find);
            if (lastIndex == -1)
            {
                return text;
            }
            return text.Remove(lastIndex, find.Length).Insert(lastIndex, replace);
        }
    }
}
