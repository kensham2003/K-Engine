﻿////////////////////////////////////////
///
///  Componentクラス
///  
///  機能：各種コンポーネントの親クラス
/// 
////////////////////////////////////////
using System;
using System.Text.Json.Serialization;

namespace GameEngine.GameEntity
{
    [JsonDerivedType(typeof(Model))]
    [JsonDerivedType(typeof(testComponent))]
    [JsonDerivedType(typeof(GameScript))]
    [JsonDerivedType(typeof(Collider))]
    [JsonDerivedType(typeof(BoxCollider))]
    [JsonDerivedType(typeof(SphereCollider))]
    [JsonDerivedType(typeof(Camera))]
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
