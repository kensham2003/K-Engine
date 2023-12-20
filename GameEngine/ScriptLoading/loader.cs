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

        //シーンオブジェクトをシリアライズした文字列
        public string m_serializeStr;

        //ロードしたアセンブリ
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

        //デシリアライズして全ゲームオブジェクトを生成
        public void LoadGameObjects(string str)
        {
            m_gameObjects = new List<GameObject>();
            Deserialize(str);
            foreach (GameObject gameObject in m_gameObjects)
            {
                //GameScriptのリストで再構築する
                for (int i = 0; i < gameObject.GameScriptName.Count; i++)
                {
                    //インスタンス生成
                    var typeName = m_nameAssemblyDict[gameObject.GameScriptName[i]].GetType("GameEngine.GameEntity." + gameObject.GameScriptName[i]);
                    var instance = Activator.CreateInstance(typeName, null);
                    dynamic ins = Convert.ChangeType(instance, typeName);

                    //プロパティ情報再構築
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

                    //フィールド情報再構築
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

                    gameObject.AddScript(ins, gameObject.GameScriptName[i]);
                }
            }
            m_game.m_gameObjects[1] = m_gameObjects;
        }

        //.dllファイルをロード
        public void LoadAssembly(string dll)
        {
            string fileName = System.IO.Path.GetFileNameWithoutExtension(dll);
            string className = fileName[0].ToString().ToUpper() + fileName.Substring(1);
            Assembly a = Assembly.LoadFile(dll);
            m_nameAssemblyDict.Add(className, a);
        }

        //文字ストリームからアセンブリロード（.dllでロードするため使われていない）
        public void LoadAssembly(byte[] stream, string className)
        {
            Assembly a = Assembly.Load(stream);
            m_nameAssemblyDict.Add(className, a);
        }

        //シリアライズ
        public string Serialize()
        {

            JsonSerializerOptions options = new JsonSerializerOptions()
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All),
                WriteIndented = true,
                IncludeFields = true,
            };

            return JsonSerializer.Serialize(m_game.m_gameObjects[1], options);
        }

        //デシリアライズ
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

        public void RenameObject(string oldName, string newName)
        {
            m_game.FindGameObject(oldName).Name = newName;
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

        //スクリプトをゲームオブジェクトに追加
        public void AddScriptToGameObject(string objectName, string classTypeName, string scriptPath)
        {
            GameObject gameObject = m_game.FindGameObject(objectName);

            //対象タイプのインスタンスを生成
            var typeName = m_nameAssemblyDict[classTypeName].GetType("GameEngine.GameEntity." + classTypeName);
            var instance = Activator.CreateInstance(typeName, null);
            dynamic ins = Convert.ChangeType(instance, typeName);

            //スクリプト情報をゲームオブジェクトに追加（シリアライズ、エンジン側との情報交換用）
            gameObject.GameScriptName.Add(classTypeName);
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

        /// <summary>
        /// プロパティ／フィールドの値を変更
        /// </summary>
        /// <param name="isProperty">変更タイプ（プロパティ：true　フィールド：false）</param>
        public string SetPropertyOrFieldValue(bool isProperty, string gameObjectName, string scriptName, string changedName, string changedValue)
        {
            GameObject gameObject = FindGameObject(gameObjectName);
            for (int i = 0; i < gameObject.GameScriptName.Count; i++)
            {
                //対象のスクリプトを探す
                if (gameObject.GameScriptName[i] != scriptName) { continue; }

                //新しいインスタンスを生成
                var typeName = m_nameAssemblyDict[gameObject.GameScriptName[i]].GetType("GameEngine.GameEntity." + gameObject.GameScriptName[i]);
                var instance = Activator.CreateInstance(typeName, null);
                dynamic ins = Convert.ChangeType(instance, typeName);

                //プロパティの値を代入（変更した値以外はそのまま）
                GameScriptPropInfo propInfo = gameObject.GameScriptPropInfos[i];
                int propAmount = propInfo.PropAmount;
                for (int j = 0; j < propAmount; j++)
                {
                    string propName = propInfo.PropNames[j];
                    string propTypeName = propInfo.PropTypes[j];
                    Type type = Type.GetType(propTypeName);
                    string propValue;
                    if (isProperty)
                    {
                        //対象のプロパティであれば、変更後の値にする
                        if (propName == changedName)
                        {
                            propValue = changedValue;
                            var prop = typeName.GetProperty(propName);

                            //型変換を試す
                            //成功した場合はnullを返し、エラーの場合は元の値を返す（入力間違いなど）
                            try
                            {
                                object newValue = Convert.ChangeType(propValue, type);
                                prop.SetValue(ins, newValue);
                                propInfo.PropValues[j] = changedValue;
                            }
                            catch
                            {
                                return propInfo.PropValues[j];
                            }
                        }
                    }
                    else
                    {
                        propValue = propInfo.PropValues[j];
                        var prop = typeName.GetProperty(propName);
                        prop.SetValue(ins, Convert.ChangeType(propValue, type));
                    }
                }

                //フィールドの値を代入（変更した値以外はそのまま）
                GameScriptPropInfo fieldInfo = gameObject.GameScriptFieldInfos[i];
                int fieldAmount = fieldInfo.PropAmount;
                for (int k = 0; k < fieldAmount; k++)
                {
                    string fieldName = fieldInfo.PropNames[k];
                    string fieldTypeName = fieldInfo.PropTypes[k];
                    Type type = Type.GetType(fieldTypeName);
                    string fieldValue;
                    if (!isProperty)
                    {
                        //対象のフィールドであれば、変更後の値にする
                        if (fieldName == changedName)
                        {
                            fieldValue = changedValue;
                            var field = typeName.GetField(fieldName);

                            //型変換を試す
                            //成功した場合はnullを返し、エラーの場合は元の値を返す（入力間違いなど）
                            try
                            {
                                object newValue = Convert.ChangeType(fieldValue, type);
                                field.SetValue(ins, newValue);
                                fieldInfo.PropValues[k] = changedValue;
                            }
                            catch
                            {
                                return fieldInfo.PropValues[k];
                            }
                        }
                    }
                    else
                    {
                        fieldValue = fieldInfo.PropValues[k];
                        var field = typeName.GetField(fieldName);
                        field.SetValue(ins, Convert.ChangeType(fieldValue, type));
                    }

                }
                //新しいインスタンスに替える
                gameObject.ReplaceScript(ins, i);
            }
            return null;
        }
    }
}
