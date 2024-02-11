using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using System.Runtime.Serialization;

using GameEngine.Detail;

namespace GameEngine.GameEntity
{
    [Serializable]
    public class GameObject
    {
        public bool Active { get; set; } = true;
        
        public SVector3 Position { get; set; } = new Vector3(0.0f, 0.0f, 0.0f);
        public SVector3 Rotation { get; set; } = new Vector3(0.0f, 0.0f, 0.0f);
        public SVector3 Scale { get; set; } = new Vector3(1.0f, 1.0f, 1.0f);

        public SVector3 OldPosition { get; set; } = new Vector3(0.0f, 0.0f, 0.0f);
        public SVector3 OldRotation { get; set; } = new Vector3(0.0f, 0.0f, 0.0f);
        public SVector3 OldScale { get; set; } = new Vector3(1.0f, 1.0f, 1.0f);

        public int Layer { get; set; }

        public string ModelName { get; set; }

        public bool HasLighting { get; set; } = true;

        public string Name { get; set; }

        public string Script { get; set; }

        public bool HasCollider { get; set; }

        public Collider Collider { get; set; }

        public List<string> ObjectsColliding = new List<string>();

        public readonly ObservableCollection<Component> Components = new ObservableCollection<Component>();

        public readonly ObservableCollection<GameScript> GameScripts = new ObservableCollection<GameScript>();

        public List<string> ComponentName = new List<string>();

        public List<string> ComponentPath = new List<string>();

        //public List<int> GameScriptPropertyAmount = new List<int>();

        //public List<string> GameScriptPropertyName = new List<string>();

        //public List<Type> GameScriptPropertyType = new List<Type>();

        //public List<string> GameScriptPropertyValue = new List<string>();

        public List<ComponentPropInfo> ComponentPropInfos = new List<ComponentPropInfo>();

        public List<ComponentPropInfo> ComponentFieldInfos = new List<ComponentPropInfo>();



        //public ReadOnlyObservableCollection<Component> Components { get; }

        public GameObject() { }
        public GameObject(string content)
        {
            Name = content;
        }

        public override string ToString()
        {
            return Name.ToString();
        }

        public void AddModel(string modelName)
        {
            Model model = new Model(this);
            model.ModelName = modelName;
            Components.Add(model);
        }

        public void BeginPlay()
        {
            foreach (GameScript s in GameScripts)
            {
                s.BeginPlay();
            }
            foreach (Component c in Components)
            {
                c.BeginPlay();
            }
        }

        public void Update(TimeSpan gameTime)
        {
            SaveTransform();
            foreach (GameScript s in GameScripts)
            {
                s.Update(gameTime);
            }
            foreach (Component c in Components)
            {
                c.Update(gameTime);
            }
        }

        public void Destroy()
        {
            Active = false;
            DestroyList.Add(this);
        }

        public void SaveTransform()
        {
            OldPosition = Position;
            OldRotation = Rotation;
            OldScale = Scale;
        }

        public void RevertTransform()
        {
            Position = OldPosition;
            Rotation = OldRotation;
            Scale = OldScale;
        }

        public void AddComponent(Component component)
        {
            Components.Add(component);
            component.SetParent(this);
        }

        public void AddComponent(Component component, string name)
        {
            Components.Add(component);
            component.SetParent(this);
            component.Name = name;
            ComponentPath.Add("");
        }

        public void AddScript(GameScript gameScript, string filePath, string name)
        {
            //Components.Add(gameScript);
            GameScripts.Add(gameScript);
            gameScript.SetParent(this);
            gameScript.Name = name;
            gameScript.FilePath = filePath;
            ComponentPath.Add(filePath);
        }

        public void AddScript(GameScript gameScript, string name)
        {
            //Components.Add(gameScript);
            GameScripts.Add(gameScript);
            gameScript.SetParent(this);
            gameScript.Name = name;
        }

        public void RemoveScriptAtIndex(int index)
        {
            GameScripts.RemoveAt(index);
            //Components.RemoveAt(index);
        }

        public void ReplaceComponent(Component component, int index)
        {
            //string name = GameScripts[index].Name;
            string name = Components[index].Name;
            Components[index] = component;
            component.SetParent(this);
            component.Name = name;
        }

        public void ReplaceScript(GameScript gameScript, int index)
        {
            string name = GameScripts[index].Name;
            //string name = Components[index].Name;
            GameScripts[index] = gameScript;
            gameScript.SetParent(this);
            gameScript.Name = name;
        }

        public void ReplaceCollider(Collider collider)
        {
            int index = FindColliderIndex();
            ReplaceComponent(collider, index);
            Collider = collider;
        }

        private int FindColliderIndex()
        {
            int index = 0;
            foreach (Component c in Components)
            {
                if (c.Name == null)
                {
                    index++;
                    continue;
                }
                foreach (string s in Define.preDefinedColliders)
                {
                    if (s == c.Name.Replace("GameEngine.GameEntity.", ""))
                    {
                        return index;
                    }
                }
                index++;
            }
            return -1;
        }

        public bool RemoveComponent(string name)
        {
            foreach (Component component in Components)
            {
                if (component.Name == name)
                {
                    Components.Remove(component);
                    return true;
                }
            }

            return false;
        }

        //public T GetScript<T>()
        //{
        //    foreach (GameScript gameScript in GameScripts)
        //    {
        //        if (gameScript.GetType().AssemblyQualifiedName == typeof(T).AssemblyQualifiedName)
        //        {
        //            return (T)Convert.ChangeType(gameScript, typeof(T));
        //        }
        //    }
        //    return default(T);
        //}

        public T GetComponent<T>()
        {
            foreach (Component component in Components)
            {
                if (component.GetType().AssemblyQualifiedName == typeof(T).AssemblyQualifiedName)
                {
                    return (T)Convert.ChangeType(component, typeof(T));
                }
            }
            return default(T);
        }
    }
}
