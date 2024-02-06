using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using System.Text.Json.Serialization;
using System.Runtime.Serialization;

using GameEngine.Detail;

namespace GameEngine.GameEntity
{
    [JsonDerivedType(typeof(Model))]
    [JsonDerivedType(typeof(testComponent))]
    [JsonDerivedType(typeof(GameScript))]
    [JsonDerivedType(typeof(Collider))]
    [JsonDerivedType(typeof(BoxCollider))]
    [Serializable]
    public class Component
    {
        public string Name { get; set; }

        protected GameObject Parent;

        public Component() { }

        public Component(GameObject gameObject)
        {
            Parent = gameObject;
        }

        public Component(GameObject gameObject, string name)
        {
            Parent = gameObject;
            Name = name;
        }

        public GameObject GetParent() { return Parent; }

        public void SetParent(GameObject gameObject) { Parent = gameObject; }

        public virtual void BeginPlay() { }

        public virtual void Update(TimeSpan gameTime) { }

    }
}
