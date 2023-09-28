using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace GameEngine.Component
{
    class Component
    {
        public string Name { get; set; }

        public GameObject Parent { get; private set; }
    }
}
