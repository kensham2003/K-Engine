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

        public SVector3 Position { get; set; } = new Vector3(0.0f, 0.0f, 0.0f);
        public SVector3 Rotation { get; set; } = new Vector3(0.0f, 0.0f, 0.0f);
        public SVector3 Scale { get; set; } = new Vector3(1.0f, 1.0f, 1.0f);

        public int Layer { get; set; }

        public string ModelName { get; set; }

        public string Name { get; set; }

        public string Script { get; set; }

        public readonly ObservableCollection<Component> Components = new ObservableCollection<Component>();

        public readonly ObservableCollection<GameScript> GameScripts = new ObservableCollection<GameScript>();

        public List<string> GameScriptName = new List<string>();

        public List<string> GameScriptPath = new List<string>();

        //public List<int> GameScriptPropertyAmount = new List<int>();

        //public List<string> GameScriptPropertyName = new List<string>();

        //public List<Type> GameScriptPropertyType = new List<Type>();

        //public List<string> GameScriptPropertyValue = new List<string>();

        public List<GameScriptPropInfo> GameScriptPropInfos = new List<GameScriptPropInfo>();

        public List<GameScriptPropInfo> GameScriptFieldInfos = new List<GameScriptPropInfo>();



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
            foreach (GameScript s in GameScripts)
            {
                s.Update(gameTime);
            }
            foreach (Component c in Components)
            {
                c.Update(gameTime);
            }
        }

        public void AddComponent(Component component)
        {
            Components.Add(component);
            component.SetParent(this);
        }

        public void AddScript(GameScript gameScript, string filePath, string name)
        {
            GameScripts.Add(gameScript);
            gameScript.SetParent(this);
            gameScript.Name = name;
            gameScript.FilePath = filePath;
            GameScriptPath.Add(filePath);
        }

        public void AddScript(GameScript gameScript, string name)
        {
            GameScripts.Add(gameScript);
            gameScript.SetParent(this);
            gameScript.Name = name;
        }

        public void ReplaceScript(GameScript gameScript, int index)
        {
            string name = GameScripts[index].Name;
            GameScripts[index] = gameScript;
            gameScript.SetParent(this);
            gameScript.Name = name;
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

        public T GetScript<T>()
        {
            foreach (GameScript gameScript in GameScripts)
            {
                if (gameScript.GetType().AssemblyQualifiedName == typeof(T).AssemblyQualifiedName)
                {
                    return (T)Convert.ChangeType(gameScript, typeof(T));
                }
            }
            return default(T);
        }
    }
}
