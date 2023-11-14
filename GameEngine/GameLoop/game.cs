using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameEngine.Component;

namespace GameEngine.GameLoop
{
    public class Game
    {
        public List<GameObject> m_gameObjects;

        public void Init() { }

        public void Uninit() { }

        public void Update(TimeSpan gameTime) { }

        public void AddGameObject(GameObject gameObject)
        {
            m_gameObjects.Add(gameObject);
        }

        public void RemoveGameObject(GameObject gameObject)
        {
            m_gameObjects.Remove(gameObject);
        }
    }
}
