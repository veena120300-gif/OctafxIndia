namespace OctafxIndia.Helpers
{
    public static class SqlHelper
    {
        public static string RemoveBeginCommit(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
                return sql;

            var lines = sql.Split('\n', StringSplitOptions.None);
            var filtered = new List<string>();

            foreach (var l in lines)
            {
                var t = l.Trim().TrimEnd(';').Trim();
                if (string.IsNullOrEmpty(t)) continue;

                var up = t.ToUpperInvariant();
                if (up == "BEGIN" || up == "COMMIT") continue;
                if (up.StartsWith("BEGIN ") || up.StartsWith("COMMIT ")) continue;

                filtered.Add(l);
            }

            return string.Join("\n", filtered);
        }
    }
}
