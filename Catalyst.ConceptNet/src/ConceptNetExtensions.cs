using Mosaik.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UID;


namespace Catalyst
{
    public static class ConceptNetExtensions

    {
        public static IEnumerable<(string word, int lexId)> ConceptNetSynonyms(this IToken token, Language language)
        {
            switch (token.POS)
            {
                default:
                    return Enumerable.Empty<(string, int)>();
            }
        }
    }
}
