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
using System.Windows;
using System.Windows.Controls;
using GameEngine;
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
                    GameScriptPropInfo oldGameScriptPropInfo = gameObject.GameScriptPropInfos[i];
                    GameScriptPropInfo newGameScriptPropInfo = new GameScriptPropInfo();
                    PropertyInfo[] newPropInfos = typeName.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    int propAmount = oldGameScriptPropInfo.PropAmount;
                    newGameScriptPropInfo.PropAmount = newPropInfos.Count();
                    foreach (PropertyInfo newInfo in newPropInfos)
                    {
                        bool isSet = false;
                        for (int m = 0; m < oldGameScriptPropInfo.PropAmount; m++)
                        {
                            //変数が減った場合
                            if (newPropInfos.Count() < m) break;

                            //変数名前が同じ（タイプを含む）の場合
                            if (newInfo.Name == oldGameScriptPropInfo.PropNames[m] && newInfo.PropertyType.AssemblyQualifiedName == oldGameScriptPropInfo.PropTypes[m])
                            {
                                //前の値をそのままで代入
                                string propName = oldGameScriptPropInfo.PropNames[m];
                                string propTypeName = oldGameScriptPropInfo.PropTypes[m];
                                Type type = Type.GetType(propTypeName);
                                string propValue = oldGameScriptPropInfo.PropValues[m];
                                var prop = typeName.GetProperty(propName);
                                prop.SetValue(ins, Convert.ChangeType(propValue, type));

                                newGameScriptPropInfo.PropNames.Add(propName);
                                newGameScriptPropInfo.PropTypes.Add(propTypeName);
                                newGameScriptPropInfo.PropValues.Add(propValue);

                                isSet = true;
                                break;
                            }
                        }

                        //元のリストにない変数である場合（dllの値をそのまま使う）
                        if (!isSet)
                        {
                            newGameScriptPropInfo.PropTypes.Add(newInfo.PropertyType.AssemblyQualifiedName);
                            newGameScriptPropInfo.PropNames.Add(newInfo.Name);
                            newGameScriptPropInfo.PropValues.Add(Convert.ToString(newInfo.GetValue(ins)));
                        }
                    }
                    gameObject.GameScriptPropInfos[i] = newGameScriptPropInfo;

                    //for (int j = 0; j < propAmount; j++)
                    //{
                    //    string propName = propInfo.PropNames[j];
                    //    string propTypeName = propInfo.PropTypes[j];
                    //    Type type = Type.GetType(propTypeName);
                    //    string propValue = propInfo.PropValues[j];
                    //    var prop = typeName.GetProperty(propName);
                    //    prop.SetValue(ins, Convert.ChangeType(propValue, type));
                    //}
                    GameScriptPropInfo oldGameScriptFieldInfo = gameObject.GameScriptFieldInfos[i];
                    GameScriptPropInfo newGameScriptFieldInfo = new GameScriptPropInfo();
                    FieldInfo[] newFieldInfos = typeName.GetFields();
                    int fieldAmount = oldGameScriptFieldInfo.PropAmount;
                    newGameScriptFieldInfo.PropAmount = newFieldInfos.Count();
                    foreach(FieldInfo newInfo in newFieldInfos)
                    {
                        bool isSet = false;
                        for(int m = 0; m < oldGameScriptFieldInfo.PropAmount; m++)
                        {
                            //変数が減った場合
                            if (newFieldInfos.Count() < m) break;

                            //変数名前が同じ（タイプを含む）の場合
                            if(newInfo.Name == oldGameScriptFieldInfo.PropNames[m] && newInfo.FieldType.AssemblyQualifiedName == oldGameScriptFieldInfo.PropTypes[m])
                            {
                                //前の値をそのままで代入
                                string fieldName = oldGameScriptFieldInfo.PropNames[m];
                                string fieldTypeName = oldGameScriptFieldInfo.PropTypes[m];
                                Type type = Type.GetType(fieldTypeName);
                                string propValue = oldGameScriptFieldInfo.PropValues[m];
                                var field = typeName.GetField(fieldName);
                                field.SetValue(ins, Convert.ChangeType(propValue, type));

                                newGameScriptFieldInfo.PropNames.Add(fieldName);
                                newGameScriptFieldInfo.PropTypes.Add(fieldTypeName);
                                newGameScriptFieldInfo.PropValues.Add(propValue);

                                isSet = true;
                                break;
                            }
                        }

                        //元のリストにない変数である場合（dllの値をそのまま使う）
                        if (!isSet)
                        {
                            newGameScriptFieldInfo.PropTypes.Add(newInfo.FieldType.AssemblyQualifiedName);
                            newGameScriptFieldInfo.PropNames.Add(newInfo.Name);
                            newGameScriptFieldInfo.PropValues.Add(Convert.ToString(newInfo.GetValue(ins)));
                        }
                    }
                    gameObject.GameScriptFieldInfos[i] = newGameScriptFieldInfo;
                    //if (fieldAmount == newFieldInfos.Count())
                    //{
                    //    for (int k = 0; k < fieldAmount; k++)
                    //    {
                            
                    //        string fieldName = oldGameScriptFieldInfo.PropNames[k];
                    //        string fieldTypeName = oldGameScriptFieldInfo.PropTypes[k];
                    //        Type type = Type.GetType(fieldTypeName);
                    //        string propValue = oldGameScriptFieldInfo.PropValues[k];
                    //        var field = typeName.GetField(fieldName);
                    //        field.SetValue(ins, Convert.ChangeType(propValue, type));
                    //    }
                    //}

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

        public GameObject FindGameObject(string name)
        {
            foreach(GameObject gameObject in m_gameObjects)
            {
                if(gameObject.Name == name) { return gameObject; }
            }
            return null;
        }

        //public void LoadComponents(string gameObjectName)
        //{
        //    GameObject gameObject = FindGameObject(gameObjectName);
        //    StackPanel component_Panel = ((MainWindow)Application.Current.MainWindow).Component_Panel;
        //    component_Panel.Children.Clear();

        //    foreach(GameScript gameScript in gameObject.GameScripts)
        //    {
        //        var stackPanelTemp = new StackPanel { Orientation = Orientation.Vertical };
        //        stackPanelTemp.Children.Add(new Label { Content = gameScript.Name });
        //        Button ComponentButton = new Button();
        //        ComponentButton.Content = "Open Script";
        //        ComponentButton.Width = 180;
        //        ComponentButton.Click += (object ss, RoutedEventArgs ee) => { System.Diagnostics.Process.Start(gameScript.FilePath); };
        //        stackPanelTemp.Children.Add(ComponentButton);
        //        component_Panel.Children.Add(stackPanelTemp);
        //    }
        //}

        public List<string> GetScriptsName(string gameObjectName)
        {
            GameObject gameObject = FindGameObject(gameObjectName);
            return gameObject.GameScriptName;
        }

        public List<string> GetScriptsPath(string gameObjectName)
        {
            GameObject gameObject = FindGameObject(gameObjectName);
            return gameObject.GameScriptPath;
        }

        public List<GameScriptPropInfo> GetScriptsPropInfos(string gameObjectName)
        {
            GameObject gameObject = FindGameObject(gameObjectName);
            return gameObject.GameScriptPropInfos;
        }

        public List<GameScriptPropInfo> GetScriptsFieldInfos(string gameObjectName)
        {
            GameObject gameObject = FindGameObject(gameObjectName);
            return gameObject.GameScriptFieldInfos;
        }

        public void SetPropValue(string gameObjectName, string scriptName, string changedPropName, string changedPropValue)
        {
            GameObject gameObject = FindGameObject(gameObjectName);
            for (int i = 0; i < gameObject.GameScriptName.Count; i++)
            {
                if (gameObject.GameScriptName[i] != scriptName) { continue; }

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
                    string propValue;
                    //変更後の値にする
                    if (propName == changedPropName)
                    {
                        propValue = changedPropValue;
                        propInfo.PropValues[j] = changedPropValue;
                    }
                    else { propValue = propInfo.PropValues[j]; }
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
                    field.SetValue(ins, Convert.ChangeType(propValue, type));
                }

                gameObject.ReplaceScript(ins, i);
            }
        }

        public void SetFieldValue(string gameObjectName, string scriptName, string changedFieldName, string changedFieldValue)
        {
            GameObject gameObject = FindGameObject(gameObjectName);
            for (int i = 0; i < gameObject.GameScriptName.Count; i++)
            {
                if (gameObject.GameScriptName[i] != scriptName) { continue; }

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
                    string fieldValue;
                    //変更後の値にする
                    if (fieldName == changedFieldName)
                    {
                        fieldValue = changedFieldValue;
                        fieldInfo.PropValues[k] = changedFieldValue;
                    }
                    else { fieldValue = fieldInfo.PropValues[k]; }
                    var field = typeName.GetField(fieldName);
                    field.SetValue(ins, Convert.ChangeType(fieldValue, type));
                }

                gameObject.ReplaceScript(ins, i);
            }
        }
    }
}
