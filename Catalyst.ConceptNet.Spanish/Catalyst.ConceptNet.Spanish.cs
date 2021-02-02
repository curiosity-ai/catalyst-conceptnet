using System;
using System.Collections.Generic;
using System.Text;

using Catalyst;
using Mosaik.Core;

namespace Catalyst.ConceptNet
{
    public static class Spanish
    {
        public static void Register()
        {
            Loader.RegisterFromAssembly(typeof(Spanish).Assembly, Language.Spanish);
        }
    }
}
