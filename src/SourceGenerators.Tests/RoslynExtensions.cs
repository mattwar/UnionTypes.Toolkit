using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnionTests
{
    internal static class RoslynExtensions
    {
        public static SourceText InsertAt(this SourceText text, int index, string insert)
        {
            return text.WithChanges(new TextChange(new TextSpan(index, 0), insert));
        }

        public static SourceText Append(this SourceText text, string append)
        {
            return text.WithChanges(new TextChange(new TextSpan(text.Length, 0), append));
        }

        public static SourceText InsertAfter(this SourceText text, string find, string insert)
        {
            var index = text.ToString().IndexOf(find);
            if (index >= 0)
            {
                return text.WithChanges(new TextChange(new TextSpan(index + find.Length, 0), insert));
            }
            return text;
        }

        public static SourceText InsertBefore(this SourceText text, string find, string insert)
        {
            var index = text.ToString().IndexOf(find);
            if (index >= 0)
            {
                return text.WithChanges(new TextChange(new TextSpan(index, 0), insert));
            }
            return text;
        }

        public static SourceText ReplaceOne(this SourceText text, string find, string replacement)
        {
            var index = text.ToString().IndexOf(find);
            if (index >= 0)
            {
                return text.WithChanges(new TextChange(new TextSpan(index, find.Length), replacement));
            }

            return text;
        }

        public static SourceText ReplaceAll(this SourceText text, string find, string replacement)
        {
            var locations = new List<int>();
            var str = text.ToString();
            var index = str.IndexOf(find);
            while (index >= 0)
            {
                locations.Add(index);
                index = str.IndexOf(find, index + find.Length);
            }

            if (locations.Count > 0)
            {
                return text.WithChanges(
                    locations.Select(loc => new TextChange(new TextSpan(loc, find.Length), replacement))
                    );
            }

            return text;
        }
    }
}
