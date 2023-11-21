using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameEngine.Component;
using GameEngine.Detail;
using GameEngine;

namespace GameEngine.GameLoop
{
    public class Game
    {
        private bool m_firstFrame = true;

        public List<GameObject>[] m_gameObjects = new List<GameObject>[5];

        //private string m_jsonString

        public void Init() 
        {
            for(int i = 0; i < Detail.Define.NUM_LAYER; i++)
            {
                m_gameObjects[i] = new List<GameObject>();
            }
        }

        public void Uninit() { }

        public void Update(TimeSpan gameTime) 
        {
            if (m_firstFrame)
            {
                for (int i = 0; i < Detail.Define.NUM_LAYER; i++)
                {
                    foreach (GameObject gameObject in m_gameObjects[i])
                    {
                        gameObject.BeginPlay();
                    }
                }
                m_firstFrame = false;
            }
            else
            {
                for (int i = 0; i < Detail.Define.NUM_LAYER; i++)
                {
                    foreach (GameObject gameObject in m_gameObjects[i])
                    {
                        gameObject.Update(gameTime);
                    }
                }
            }

            for (int i = 0; i < Detail.Define.NUM_LAYER; i++)
            {
                foreach (GameObject gameObject in m_gameObjects[i])
                {
                    GameEngine.MainWindow.NativeMethods.InvokeWithDllProtection(() => GameEngine.MainWindow.NativeMethods.SetObjectTransform(gameObject.Name, gameObject.Position, gameObject.Rotation, gameObject.Scale));
                }
            }
        }

        public void AddGameObject(GameObject gameObject, int layer)
        {
            gameObject.Layer = layer;
            m_gameObjects[layer].Add(gameObject);
        }

        public void RemoveGameObject(GameObject gameObject)
        {
            m_gameObjects[gameObject.Layer].Remove(gameObject);
        }

        public GameObject FindGameObject(string name)
        {
            for(int i = 0; i < Detail.Define.NUM_LAYER; i++)
            {
                foreach(GameObject gameObject in m_gameObjects[i])
                {
                    if(gameObject.Name == name)
                    {
                        return gameObject;
                    }
                }
            }
            return null;
        }
    }
}
