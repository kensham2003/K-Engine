////////////////////////////////////////
///
///  Loaderクラス
///  
///  機能：ゲームループ管理用クラス
/// 
////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using GameEngine.Detail;
using GameEngine.GameEntity;
using GameEngine.GameLoop;

namespace GameEngine.ScriptLoading
{
    public class Loader : MarshalByRefObject
    {
        public List<GameObject> m_gameObjects = new List<GameObject>();
        GameEngine.GameLoop.GameLoop m_gameLoop;
        Game m_game;



        public string m_serializeStr;

        private Dictionary<string, Assembly> m_nameAssemblyDict;

        public void InitDomain()
        {
            m_nameAssemblyDict = new Dictionary<string, Assembly>();

            //ゲームループ
            m_gameLoop = new GameEngine.GameLoop.GameLoop();
            m_game = new Game();
            m_gameLoop.Load(m_game);
            m_gameLoop.Start();
        }

        public string UninitDomain()
        {
            m_serializeStr = Serialize();
            m_gameLoop.Quit();
            return m_serializeStr;
        }

        public void LoadGameObjects(string str)
        {
            m_gameObjects = new List<GameObject>();
            Deserialize(str);
            foreach (GameObject gameObject in m_gameObjects)
            {
                //GameScriptのリストで再構築する
                //int nowPropIndex = 0;
                for (int i = 0; i < gameObject.GameScriptName.Count; i++)
                {
                    var typeName = m_nameAssemblyDict[gameObject.GameScriptName[i]].GetType("GameEngine.GameEntity." + gameObject.GameScriptName[i]);
                    var instance = Activator.CreateInstance(typeName, null);
                    dynamic ins = Convert.ChangeType(instance, typeName);
                    GameScriptPropInfo propInfo = gameObject.GameScriptPropInfos[i];
                    int propAmount = propInfo.PropAmount;
                    for (int j = 0; j < propAmount; j++)
                    {
                        string propName = propInfo.PropNames[j];
                        string propTypeName = propInfo.PropTypes[j];
                        Type type = Type.GetType(propTypeName);
                        string propValue = propInfo.PropValues[j];
                        var prop = typeName.GetProperty(propName);
                        prop.SetValue(ins, Convert.ChangeType(propValue, type));
                    }
                    GameScriptPropInfo fieldInfo = gameObject.GameScriptFieldInfos[i];
                    int fieldAmount = fieldInfo.PropAmount;
                    for (int k = 0; k < fieldAmount; k++)
                    {
                        string fieldName = fieldInfo.PropNames[k];
                        string fieldTypeName = fieldInfo.PropTypes[k];
                        Type type = Type.GetType(fieldTypeName);
                        string propValue = fieldInfo.PropValues[k];
                        var field = typeName.GetField(fieldName);
                        //field.SetValue(ins, Convert.ChangeType(propValue, type));
                    }
                    gameObject.AddScript(ins, gameObject.GameScriptName[i]);
                }
            }
            m_game.m_gameObjects[1] = m_gameObjects;
        }

        public void LoadAssembly(string dll)
        {
            string fileName = System.IO.Path.GetFileNameWithoutExtension(dll);
            string className = fileName[0].ToString().ToUpper() + fileName.Substring(1);
            Assembly a = Assembly.LoadFile(dll);
            m_nameAssemblyDict.Add(className, a);
        }

        public void LoadAssembly(byte[] stream, string className)
        {
            Assembly a = Assembly.Load(stream);
            m_nameAssemblyDict.Add(className, a);
        }

        public string Serialize()
        {
            //foreach(GameObject gameObject in game.m_gameObjects[1])
            //{
            //    for(int i = 0; i < gameObject.GameScripts.Count; i++)
            //    {
            //        gameObject.GameScriptName.Add(gameObject.GameScripts[i].Name);
            //        var typeName = nameAssemblyDict[gameObject.GameScripts[i].Name].GetType("GameEngine.GameEntity." + gameObject.GameScripts[i].Name);

            //    }
            //}

            JsonSerializerOptions options = new JsonSerializerOptions()
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All),
                WriteIndented = true,
                IncludeFields = true,
            };

            return JsonSerializer.Serialize(m_game.m_gameObjects[1], options);
        }

        public void Deserialize(string str)
        {
            JsonSerializerOptions options = new JsonSerializerOptions()
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All),
                WriteIndented = true,
                IncludeFields = true,
            };
            m_gameObjects = JsonSerializer.Deserialize<List<GameObject>>(str, options);
        }

        public void AddGameObject(string objectName, string modelFileName)
        {
            GameObject gameObject = new GameObject(objectName);
            gameObject.ModelName = modelFileName;
            gameObject.AddModel(modelFileName);
            //gameObjects.Add(gameObject);
            m_game.AddGameObject(gameObject, Define.LAYER_3D_OBJECT);
        }

        public void Play()
        {
            m_gameLoop.Play();
        }

        public void Stop()
        {
            m_gameLoop.Stop();
        }

        public void SetGameObjectPosition(string name, float x, float y, float z)
        {
            Vector3 pos = new Vector3(x, y, z);
            m_game.FindGameObject(name).Position = pos;
        }

        public void SetGameObjectRotation(string name, float x, float y, float z)
        {
            Vector3 rot = new Vector3(x, y, z);
            m_game.FindGameObject(name).Rotation = rot;
        }
        public void SetGameObjectScale(string name, float x, float y, float z)
        {
            Vector3 scl = new Vector3(x, y, z);
            m_game.FindGameObject(name).Scale = scl;
        }

        public void AddScriptToGameObject(string objectName, string classTypeName, string scriptPath)
        {
            GameObject gameObject = m_game.FindGameObject(objectName);
            //var typeName = Assembly.GetExecutingAssembly().GetType("GameEngine.GameEntity." + classTypeName);
            var typeName = m_nameAssemblyDict[classTypeName].GetType("GameEngine.GameEntity." + classTypeName);
            var instance = Activator.CreateInstance(typeName, null);
            dynamic ins = Convert.ChangeType(instance, typeName);

            gameObject.GameScriptName.Add(classTypeName);
            //PropertyDescriptorCollection props = TypeDescriptor.GetProperties(ins);
            FieldInfo[] fieldInfos = typeName.GetFields();
            PropertyInfo[] propInfos = typeName.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            GameScriptPropInfo gameScriptPropInfo = new GameScriptPropInfo();
            int propAmount = propInfos.Count();
            gameScriptPropInfo.PropAmount = propAmount;
            for (int i = 0; i < propAmount; i++)
            {
                gameScriptPropInfo.PropTypes.Add(propInfos[i].PropertyType.AssemblyQualifiedName);
                gameScriptPropInfo.PropNames.Add(propInfos[i].Name);
                gameScriptPropInfo.PropValues.Add(Convert.ToString(propInfos[i].GetValue(ins)));
            }
            gameObject.GameScriptPropInfos.Add(gameScriptPropInfo);

            GameScriptPropInfo gameScriptFieldInfo = new GameScriptPropInfo();
            int fieldAmount = fieldInfos.Count();
            gameScriptFieldInfo.PropAmount = fieldAmount;
            for (int i = 0; i < fieldAmount; i++)
            {
                gameScriptFieldInfo.PropTypes.Add(fieldInfos[i].FieldType.AssemblyQualifiedName);
                gameScriptFieldInfo.PropNames.Add(fieldInfos[i].Name);
                gameScriptFieldInfo.PropValues.Add(Convert.ToString(fieldInfos[i].GetValue(ins)));
            }
            gameObject.GameScriptFieldInfos.Add(gameScriptFieldInfo);

            gameObject.AddScript(ins, scriptPath, classTypeName);
        }
    }
}
