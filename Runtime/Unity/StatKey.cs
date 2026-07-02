using System.Text;

namespace Stats
{
    public static class StatKey
    {
        public static bool IsValid(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;
            if (key[0] == '_' || key[key.Length - 1] == '_') return false;
            bool previousUnderscore = false;
            bool hasAlphanumeric = false;
            for (int i = 0; i < key.Length; i++)
            {
                char c = key[i];
                if (c == '_')
                {
                    if (previousUnderscore) return false;
                    previousUnderscore = true;
                    continue;
                }
                previousUnderscore = false;
                bool valid = (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9');
                if (!valid) return false;
                hasAlphanumeric = true;
            }
            return hasAlphanumeric;
        }

        public static string Normalize(string source)
        {
            if (string.IsNullOrEmpty(source)) return string.Empty;
            var builder = new StringBuilder(source.Length);
            bool previousUnderscore = false;
            foreach (char raw in source)
            {
                char c = char.ToLowerInvariant(raw);
                if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'))
                {
                    builder.Append(c);
                    previousUnderscore = false;
                }
                else if (builder.Length > 0 && !previousUnderscore)
                {
                    builder.Append('_');
                    previousUnderscore = true;
                }
            }
            int end = builder.Length;
            while (end > 0 && builder[end - 1] == '_') end--;
            return builder.ToString(0, end);
        }
    }
}
