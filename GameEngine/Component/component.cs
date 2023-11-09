using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using System.Text.Json.Serialization;

namespace GameEngine.Component
{
    [JsonDerivedType(typeof(Model))]
    public class Component
    {
        public string Name { get; set; }

        private GameObject Parent;

        public Component(GameObject gameObject)
        {
            Parent = gameObject;
        }

        public GameObject GetParent() { return Parent; }
    }
}
