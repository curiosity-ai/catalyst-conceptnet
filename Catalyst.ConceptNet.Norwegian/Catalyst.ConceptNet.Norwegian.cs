﻿using System;
using System.Collections.Generic;
using System.Text;

using Catalyst;
using Mosaik.Core;

namespace Catalyst.ConceptNet
{
    public static class Norwegian
    {
        public static void Register()
        {
            Loader.RegisterFromAssembly(typeof(Norwegian).Assembly, Language.Norwegian);
        }
    }
}
