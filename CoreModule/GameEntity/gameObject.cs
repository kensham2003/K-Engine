using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace GameEngine.GameEntity
{
    public class GameObject
    {

        public Vector3 Position { get; set; } = new Vector3(0.0f, 0.0f, 0.0f);
        public Vector3 Rotation { get; set; } = new Vector3(0.0f, 0.0f, 0.0f);
        public Vector3 Scale { get; set; } = new Vector3(1.0f, 1.0f, 1.0f);

        public int Layer { get; set; }

        public string ModelName { get; set; }

        public string Name { get; set; }

        public string Script { get; set; }

        public readonly ObservableCollection<Component> Components = new ObservableCollection<Component>();

        //public ReadOnlyObservableCollection<Component> Components { get; }

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
            foreach (Component c in Components)
            {
                c.BeginPlay();
            }
        }

        public void Update(TimeSpan gameTime)
        {
            foreach(Component c in Components)
            {
                c.Update(gameTime);
            }
        }

        public void AddComponent(Component component)
        {
            Components.Add(component);
            component.SetParent(this);
        }

        public void AddScript(GameScript gameScript, string filePath)
        {
            Components.Add(gameScript);
            gameScript.SetParent(this);
            gameScript.FilePath = filePath;
        }
    }
}
