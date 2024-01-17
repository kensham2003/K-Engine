//////////////////////////////////////////////////////////
///
///  Loaderクラス
///  
///  機能：シミュレート用ゲーム環境全般を管理するクラス
/// 
/////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text.Json;
using GameEngine.Detail;
using GameEngine.GameEntity;
using GameEngine.GameLoop;

namespace GameEngine.ScriptLoading
{
    public class Loader : MarshalByRefObject
    {
        public List<GameObject> m_gameObjects = new List<GameObject>();
        GameLoop.GameLoop m_gameLoop;
        Game m_game;

        //シーンオブジェクトをシリアライズした文字列
        public string m_serializeStr;

        //ロードしたアセンブリ
        private Dictionary<string, Assembly> m_nameAssemblyDict;

        private Assembly m_Assembly;


        /// <summary>
        /// Loaderの初期化
        /// </summary>
        public void InitDomain()
        {
            m_nameAssemblyDict = new Dictionary<string, Assembly>();

            //ゲームループ
            m_gameLoop = new GameEngine.GameLoop.GameLoop();
            m_game = new Game();
            m_gameLoop.Load(m_game);
            m_gameLoop.Start();
        }


        /// <summary>
        /// Loaderの終了処理
        /// </summary>
        /// <returns>シリアライズされたゲームオブジェクト情報</returns>
        public string UninitDomain()
        {
            m_serializeStr = Serialize();
            m_gameLoop.Quit();
            return m_serializeStr;
        }

        
        /// <summary>
        /// デシリアライズして全ゲームオブジェクトを生成
        /// </summary>
        /// <param name="str">シリアライズ文字列</param>
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
                    //var typeName = m_nameAssemblyDict[gameObject.GameScriptName[i]].GetType("GameEngine.GameEntity." + gameObject.GameScriptName[i]);
                    var typeName = m_Assembly.GetType("GameEngine.GameEntity." + gameObject.GameScriptName[i]);
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

        
        /// <summary>
        /// .dllファイルをロード
        /// </summary>
        /// <param name="dll">dllファイルのパス</param>
        public void LoadAssembly(string dll)
        {
            string fileName = System.IO.Path.GetFileNameWithoutExtension(dll);
            string className = fileName[0].ToString().ToUpper() + fileName.Substring(1);
            Assembly a = Assembly.LoadFile(dll);
            m_nameAssemblyDict.Add(className, a);
            m_Assembly = Assembly.LoadFile(dll);
        }


        /// <summary>
        /// 文字ストリームからアセンブリロード（.dllでロードするため使われていない）
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="className">クラス名</param>
        public void LoadAssembly(byte[] stream, string className)
        {
            Assembly a = Assembly.Load(stream);
            m_nameAssemblyDict.Add(className, a);
        }

        
        /// <summary>
        /// オブジェクトの状態をシリアライズして一時保存
        /// </summary>
        /// <returns>シリアライズされた文字列</returns>
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


        /// <summary>
        /// オブジェクト状態をデシリアライズ
        /// </summary>
        /// <param name="str">シリアライズされた文字列</param>
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


        /// <summary>
        /// ゲームオブジェクトを追加
        /// </summary>
        /// <param name="objectName">オブジェクト名</param>
        /// <param name="modelFileName">モデルのファイル名</param>
        public void AddGameObject(string objectName, string modelFileName)
        {
            GameObject gameObject = new GameObject(objectName);
            gameObject.ModelName = modelFileName;
            gameObject.AddModel(modelFileName);
            m_game.AddGameObject(gameObject, Define.LAYER_3D_OBJECT);
        }


        /// <summary>
        /// ゲームシミュレーションを開始
        /// </summary>
        public void Play()
        {
            m_gameLoop.Play();
        }


        /// <summary>
        /// ゲームシミュレーションを停止
        /// </summary>
        public void Stop()
        {
            m_gameLoop.Stop();
        }


        /// <summary>
        /// オブジェクトの名前を変える
        /// </summary>
        /// <param name="oldName">オブジェクトの元名前</param>
        /// <param name="newName">オブジェクトの新しい名前</param>
        public void RenameObject(string oldName, string newName)
        {
            m_game.FindGameObject(oldName).Name = newName;
        }


        /// <summary>
        /// オブジェクトの位置を取得
        /// </summary>
        /// <param name="name">オブジェクト名</param>
        /// <returns>オブジェクトの位置ベクトル</returns>
        public SVector3 GetGameObjectPosition(string name)
        {
            GameObject gameObject = m_game.FindGameObject(name);
            if(gameObject != null)
            {
                return gameObject.Position;
            }
            return new SVector3(0.0f, 0.0f, 0.0f);
        }


        /// <summary>
        /// オブジェクトの回転を取得
        /// </summary>
        /// <param name="name">オブジェクト名</param>
        /// <returns>オブジェクトの回転ベクトル</returns>
        public SVector3 GetGameObjectRotation(string name)
        {
            GameObject gameObject = m_game.FindGameObject(name);
            if (gameObject != null)
            {
                return gameObject.Rotation;
            }
            return new SVector3(0.0f, 0.0f, 0.0f);
        }


        /// <summary>
        /// オブジェクトのスケールを取得
        /// </summary>
        /// <param name="name">オブジェクト名</param>
        /// <returns>オブジェクトのスケールベクトル</returns>
        public SVector3 GetGameObjectScale(string name)
        {
            GameObject gameObject = m_game.FindGameObject(name);
            if (gameObject != null)
            {
                return gameObject.Scale;
            }
            return new SVector3(1.0f, 1.0f, 1.0f);
        }


        /// <summary>
        /// オブジェクトの位置を設定
        /// </summary>
        /// <param name="name">オブジェクト名</param>
        /// <param name="x">position.X</param>
        /// <param name="y">position.Y</param>
        /// <param name="z">position.Z</param>
        public void SetGameObjectPosition(string name, float x, float y, float z)
        {
            Vector3 pos = new Vector3(x, y, z);
            m_game.FindGameObject(name).Position = pos;
        }


        /// <summary>
        /// オブジェクトの回転を設定
        /// </summary>
        /// <param name="name">オブジェクト名</param>
        /// <param name="x">rotation.X</param>
        /// <param name="y">rotation.Y</param>
        /// <param name="z">rotation.Z</param>
        public void SetGameObjectRotation(string name, float x, float y, float z)
        {
            Vector3 rot = new Vector3(x, y, z);
            m_game.FindGameObject(name).Rotation = rot;
        }


        /// <summary>
        /// オブジェクトのスケールを設定
        /// </summary>
        /// <param name="name">オブジェクト名</param>
        /// <param name="x">scale.X</param>
        /// <param name="y">scale.Y</param>
        /// <param name="z">scale.Z</param>
        public void SetGameObjectScale(string name, float x, float y, float z)
        {
            Vector3 scl = new Vector3(x, y, z);
            m_game.FindGameObject(name).Scale = scl;
        }


        /// <summary>
        /// スクリプトをゲームオブジェクトに追加
        /// </summary>
        /// <param name="objectName">オブジェクト名</param>
        /// <param name="classTypeName">スクリプト名</param>
        /// <param name="scriptPath">スクリプトのパス</param>
        public void AddScriptToGameObject(string objectName, string classTypeName, string scriptPath)
        {
            GameObject gameObject = m_game.FindGameObject(objectName);


            //対象タイプのインスタンスを生成
            var typeName = m_Assembly.GetType("GameEngine.GameEntity." + classTypeName);
            var instance = Activator.CreateInstance(typeName, null);
            dynamic ins = Convert.ChangeType(instance, typeName);

            //スクリプト情報をゲームオブジェクトに追加（シリアライズ、エンジン側との情報交換用）
            gameObject.GameScriptName.Add(classTypeName);
            FieldInfo[] fieldInfos = typeName.GetFields();
            PropertyInfo[] propInfos = typeName.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            //プロパティ情報
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

            //フィールド情報
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


        /// <summary>
        /// オブジェクトから指定スクリプトを削除
        /// ※使用先にはindexが既にあるため下の...ByIndexが代わりに使用されている
        /// </summary>
        /// <param name="objectName">オブジェクト名</param>
        /// <param name="scriptName">スクリプト名</param>
        public void RemoveScriptFromGameObject(string objectName, string scriptName)
        {
            GameObject gameObject = m_game.FindGameObject(objectName);

            int scriptIndex = gameObject.GameScriptName.IndexOf(scriptName);
            gameObject.GameScriptName.RemoveAt(scriptIndex);
            gameObject.GameScriptPath.RemoveAt(scriptIndex);
            gameObject.GameScriptPropInfos.RemoveAt(scriptIndex);
            gameObject.GameScriptFieldInfos.RemoveAt(scriptIndex);

            gameObject.RemoveScriptAtIndex(scriptIndex);
        }


        /// <summary>
        /// オブジェクトのindex個目のスクリプトを（オブジェクトから）削除
        /// </summary>
        /// <param name="objectName">オブジェクト名</param>
        /// <param name="index">スクリプトの何番目</param>
        public void RemoveScriptFromGameObjectByIndex(string objectName, int index)
        {
            GameObject gameObject = m_game.FindGameObject(objectName);

            gameObject.GameScriptName.RemoveAt(index);
            gameObject.GameScriptPath.RemoveAt(index);
            gameObject.GameScriptPropInfos.RemoveAt(index);
            gameObject.GameScriptFieldInfos.RemoveAt(index);

            gameObject.RemoveScriptAtIndex(index);
        }

        /// <summary>
        /// 名前に対応しているオブジェクトを取得
        /// </summary>
        /// <param name="name">オブジェクト名</param>
        /// <returns>指定されたオブジェクト</returns>
        public GameObject FindGameObject(string name)
        {
            foreach(GameObject gameObject in m_gameObjects)
            {
                if(gameObject.Name == name) { return gameObject; }
            }
            return null;
        }


        /// <summary>
        /// オブジェクトが入っているスクリプトの名前を取得
        /// </summary>
        /// <param name="gameObjectName">オブジェクト名</param>
        /// <returns>オブジェクトが入っているスクリプトのリスト</returns>
        public List<string> GetScriptsName(string gameObjectName)
        {
            GameObject gameObject = FindGameObject(gameObjectName);
            return gameObject.GameScriptName;
        }


        /// <summary>
        /// オブジェクトが入っているスクリプトのパスを取得
        /// </summary>
        /// <param name="gameObjectName">オブジェクト名</param>
        /// <returns>オブジェクトが入っているスクリプトパスのリスト</returns>
        public List<string> GetScriptsPath(string gameObjectName)
        {
            GameObject gameObject = FindGameObject(gameObjectName);
            return gameObject.GameScriptPath;
        }


        /// <summary>
        /// オブジェクトが入っているスクリプトのプロパティ情報を取得
        /// </summary>
        /// <param name="gameObjectName">オブジェクト名</param>
        /// <returns>オブジェクトが入っているスクリプトのプロパティ情報のリスト</returns>
        public List<GameScriptPropInfo> GetScriptsPropInfos(string gameObjectName)
        {
            GameObject gameObject = FindGameObject(gameObjectName);
            return gameObject.GameScriptPropInfos;
        }


        /// <summary>
        /// オブジェクトが入っているスクリプトのフィールド情報を取得
        /// </summary>
        /// <param name="gameObjectName">オブジェクト名</param>
        /// <returns>オブジェクトが入っているスクリプトのフィールド情報のリスト</returns>
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
                var typeName = m_Assembly.GetType("GameEngine.GameEntity." + gameObject.GameScriptName[i]);
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


        /// <summary>
        /// 追加されているスクリプト名のリストを取得
        /// </summary>
        /// <returns>追加されているスクリプト名のリスト</returns>
        public List<string> GetScriptsList()
        {
            //タイプ名が「GameEngine.GameEntity.スクリプト名」になっているので前半は不要
            const string remove = "GameEngine.GameEntity.";
            Type[] type = m_Assembly.GetTypes();
            List<string> output = new List<string>();
            for(int i = 0; i < type.Length; i++)
            {
                string typeString = type[i].ToString();
                int removeIndex = typeString.IndexOf(remove);
                //アウトプット文字列の「GameEngine.GameEntity.」部分を消す
                string classString = (removeIndex < 0)
                    ? typeString
                    : typeString.Remove(removeIndex, remove.Length);
                output.Add(classString);
            }
            return output;
        }


        /// <summary>
        /// オブジェクトが指定されたスクリプトを入っているがをチェック
        /// </summary>
        /// <param name="gameObjectName">オブジェクト名</param>
        /// <param name="targetScriptName">対象スクリプト名</param>
        /// <returns>入っているならTrue、ないならFalse</returns>
        public bool IsObjectContainingScript(string gameObjectName, string targetScriptName)
        {
            GameObject gameObject = FindGameObject(gameObjectName);
            foreach(string scriptName in gameObject.GameScriptName)
            {
                if(targetScriptName == scriptName)
                {
                    return true;
                }
            }
            return false;
        }


        /// <summary>
        /// デバッグログを取得
        /// </summary>
        /// <returns>デバッグログのリスト</returns>
        public List<string> GetDebugMessage()
        {
            return Debug.m_log;
        }
    }
}
