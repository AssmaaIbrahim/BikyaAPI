using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikya.Services.Services.Helpers
{
    public static class StringSimilarity
    {
        public static int LevenshteinDistance(string s, string t)
        {
            if (string.IsNullOrEmpty(s)) return t?.Length ?? 0;
            if (string.IsNullOrEmpty(t)) return s.Length;

            var d = new int[s.Length + 1, t.Length + 1];
            for (int i = 0; i <= s.Length; i++) d[i, 0] = i;
            for (int j = 0; j <= t.Length; j++) d[0, j] = j;

            for (int i = 1; i <= s.Length; i++)
            {
                for (int j = 1; j <= t.Length; j++)
                {
                    int cost = s[i - 1] == t[j - 1] ? 0 : 1;
                    d[i, j] = new[] {
                    d[i - 1, j] + 1,
                    d[i, j - 1] + 1,
                    d[i - 1, j - 1] + cost
                }.Min();
                }
            }

            return d[s.Length, t.Length];
        }

        public static double Similarity(string s1, string s2)
        {
            int distance = LevenshteinDistance(s1.ToLower(), s2.ToLower());
            int maxLen = Math.Max(s1.Length, s2.Length);
            return maxLen == 0 ? 1.0 : 1.0 - (double)distance / maxLen;
        }
    }

}
