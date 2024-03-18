//////////////////////////////////////////////////////////
///
///  Gameクラス
///  
///  機能：シミュレート用ゲーム環境の内部を制御するクラス
/// 
//////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;
using GameEngine.GameEntity;
using GameEngine.Detail;
using GameEngine.Physics;

namespace GameEngine.GameLoop
{
    public class Game
    {
        /// <summary>
        /// シミュレートの初フレームなのか
        /// </summary>
        public bool m_firstFrame = true;

        /// <summary>
        /// ゲーム内にあるゲームオブジェクトのリスト
        /// </summary>
        public List<GameObject>[] m_gameObjects = new List<GameObject>[5];

        private Camera m_mainCamera;
        private bool m_mainCameraHasTarget;


        /// <summary>
        /// ゲームを初期化
        /// </summary>
        public void Init() 
        {
            Debug.ClearLog();
            DestroyList.ClearList();

            for(int i = 0; i < Detail.Define.NUM_LAYER; i++)
            {
                m_gameObjects[i] = new List<GameObject>();
            }
        }


        /// <summary>
        /// ゲームの終了処理（現在は特になし）
        /// </summary>
        public void Uninit() { }


        /// <summary>
        /// ゲームの更新処理
        /// </summary>
        /// <param name="gameTime">進む時間の量</param>
        public void Update(TimeSpan gameTime) 
        {
            DestroyList.ClearList();

            Input.Keyboard.UpdateKeyState();

            //最初のフレームならBeginPlay()を呼ぶ
            if (m_firstFrame)
            {
                m_mainCamera = FindGameObject("MainCamera").GetComponent<Camera>();
                if(m_mainCamera.LookAtTarget != null)
                {
                    m_mainCameraHasTarget = true;
                }
                else
                {
                    m_mainCameraHasTarget = false;
                }
                MainWindow.NativeMethods.InvokeWithDllProtection(() => MainWindow.NativeMethods.SetCameraFocusTarget(m_mainCameraHasTarget));
                for (int i = 0; i < Define.NUM_LAYER; i++)
                {
                    foreach (GameObject gameObject in m_gameObjects[i])
                    {
                        if (!gameObject.Active) { continue; }
                        gameObject.ObjectsColliding.Clear();
                        gameObject.BeginPlay();
                    }
                }

                if (m_mainCameraHasTarget)
                {
                    MainWindow.NativeMethods.InvokeWithDllProtection(() => MainWindow.NativeMethods.SetCameraTargetPosition(m_mainCamera.LookAtTarget.Position + m_mainCamera.LookAtOffset));
                }
                m_firstFrame = false;
            }
            //なければUpdate(gameTime)を呼ぶ
            else
            {
                for (int i = 0; i < Define.NUM_LAYER; i++)
                {
                    foreach (GameObject gameObject in m_gameObjects[i])
                    {
                        if (!gameObject.Active) { continue; }
                        gameObject.Update(gameTime);
                    }
                }
            }

            //当たり判定の処理
            for (int i = 0; i < Define.NUM_LAYER; i++)
            {
                foreach (GameObject gameObjectA in m_gameObjects[i])
                {
                    //Destroyされた場合は飛ばす
                    if (!gameObjectA.Active) { continue; }

                    //コライダーを持ってない場合は飛ばす
                    if (!gameObjectA.HasCollider) { continue; }

                    List<GameObject> ObjectCollidingThisFrame = new List<GameObject>();
                    for (int j = 0; j < Define.NUM_LAYER; j++)
                    {
                        foreach (GameObject gameObjectB in m_gameObjects[j])
                        {
                            if(gameObjectA == gameObjectB) { continue; }

                            //Destroyされた場合は飛ばす
                            if (!gameObjectB.Active) { continue; }

                            //コライダーを持ってない場合は飛ばす
                            if (!gameObjectB.HasCollider) { continue; }

                            //当たっている
                            if(Collision.CheckCollision(gameObjectA, gameObjectB))
                            {
                                ObjectCollidingThisFrame.Add(gameObjectB);
                            }
                        }
                    }

                    foreach(GameObject b in ObjectCollidingThisFrame)
                    {
                        //前のフレームも当たっていた場合（Stay）
                        if (gameObjectA.ObjectsColliding.Contains(b.Name))
                        {
                            //OnTrigger/OnTriggerStay
                            if (gameObjectA.Collider.IsTrigger)
                            {
                                foreach (GameScript script in gameObjectA.GameScripts)
                                {
                                    script.OnTrigger(b.Collider);
                                    script.OnTriggerStay(b.Collider);
                                }
                            }
                            //OnCollision/OnCollisionStay
                            else
                            {
                                foreach (GameScript script in gameObjectA.GameScripts)
                                {
                                    script.OnCollision(b.Collider);
                                    script.OnCollisionStay(b.Collider);
                                }
                            }
                        }
                        //前のフレームが当たっていなかった場合(Enter)
                        else
                        {
                            //OnTrigger/OnTriggerEnter
                            if (gameObjectA.Collider.IsTrigger)
                            {
                                foreach (GameScript script in gameObjectA.GameScripts)
                                {
                                    script.OnTrigger(b.Collider);
                                    script.OnTriggerEnter(b.Collider);
                                }
                            }
                            //OnCollision/OnCollisionEnter
                            else
                            {
                                foreach (GameScript script in gameObjectA.GameScripts)
                                {
                                    script.OnCollision(b.Collider);
                                    script.OnCollisionEnter(b.Collider);
                                }
                            }
                        }
                    }

                    foreach(string name in gameObjectA.ObjectsColliding)
                    {
                        GameObject collidingObject = ObjectCollidingThisFrame.Find(gameObject => gameObject.Name == name);
                        //前のフレームが当たっていて、今は当たっていない場合(Exit)
                        if(collidingObject == null)
                        {
                            collidingObject = FindGameObject(name);
                            //nullチェック
                            if(collidingObject == null) { continue; }

                            //OnTriggerExit(当たっていないのでOnTriggerを呼ばない)
                            if (gameObjectA.Collider.IsTrigger)
                            {
                                foreach (GameScript script in gameObjectA.GameScripts)
                                {
                                    script.OnTriggerExit(collidingObject.Collider);
                                }
                            }
                            //OnCollisionExit(当たっていないのでOnCollisionを呼ばない)
                            else
                            {
                                foreach (GameScript script in gameObjectA.GameScripts)
                                {
                                    script.OnCollisionExit(collidingObject.Collider);
                                }
                            }
                        }
                    }

                    //isTriggerではない場合何かに当たった場合
                    //座標を前のフレームの座標に戻す
                    if (!gameObjectA.Collider.IsTrigger)
                    {
                        if(ObjectCollidingThisFrame.Count > 0)
                        {
                            gameObjectA.RevertTransform();
                        }
                    }

                    //当たり対象のリストの更新
                    gameObjectA.ObjectsColliding.Clear();
                    foreach(GameObject gameObject in ObjectCollidingThisFrame)
                    {
                        gameObjectA.ObjectsColliding.Add(gameObject.Name);
                    }
                }
            }

            //Destroy()の処理
            foreach(GameObject gameObject in DestroyList.m_list)
            {
                MainWindow.NativeMethods.InvokeWithDllProtection(() => MainWindow.NativeMethods.SetObjectDrawFlag(gameObject.Name, false));
            }

            if (m_mainCameraHasTarget)
            {
                if(m_mainCamera.LookAtTarget == null)
                {
                    m_mainCameraHasTarget = false;
                    MainWindow.NativeMethods.InvokeWithDllProtection(() => MainWindow.NativeMethods.SetCameraFocusTarget(m_mainCameraHasTarget));
                }
            }
            else
            {
                if(m_mainCamera.LookAtTarget != null)
                {
                    m_mainCameraHasTarget = true;
                    MainWindow.NativeMethods.InvokeWithDllProtection(() => MainWindow.NativeMethods.SetCameraFocusTarget(m_mainCameraHasTarget));
                }
            }

            if (m_mainCameraHasTarget)
            {
                MainWindow.NativeMethods.InvokeWithDllProtection(() => MainWindow.NativeMethods.SetCameraTargetPosition(m_mainCamera.LookAtTarget.Position + m_mainCamera.LookAtOffset));
            }

            //更新が終わったオブジェクトの位置情報などを描画側（C++）に送る
            for (int i = 0; i < Define.NUM_LAYER; i++)
            {
                foreach (GameObject gameObject in m_gameObjects[i])
                {
                    MainWindow.NativeMethods.InvokeWithDllProtection(() => MainWindow.NativeMethods.SetObjectTransform(gameObject.Name, gameObject.Position, gameObject.Rotation, gameObject.Scale));
                }
            }
        }


        /// <summary>
        /// ゲームオブジェクトを追加
        /// </summary>
        /// <param name="gameObject">オブジェクトのインスタンス</param>
        /// <param name="layer">レイヤーの番号</param>
        public void AddGameObject(GameObject gameObject, int layer)
        {
            gameObject.Layer = layer;
            m_gameObjects[layer].Add(gameObject);
        }


        /// <summary>
        /// ゲームオブジェクトを削除
        /// </summary>
        /// <param name="gameObject">オブジェクトのインスタンス</param>
        public void RemoveGameObject(GameObject gameObject)
        {
            m_gameObjects[gameObject.Layer].Remove(gameObject);
            MainWindow.NativeMethods.InvokeWithDllProtection(() => MainWindow.NativeMethods.RemoveObject(gameObject.Name));
        }


        /// <summary>
        /// 対象レイヤーの全オブジェクトを削除
        /// </summary>
        /// <param name="layer">指定したレイヤー</param>
        public void RemoveAllGameObjects(int layer)
        {
            foreach(GameObject gameObject in m_gameObjects[layer])
            {
                MainWindow.NativeMethods.InvokeWithDllProtection(() => MainWindow.NativeMethods.RemoveObject(gameObject.Name));
            }

            m_gameObjects[layer].Clear();
        }


        /// <summary>
        /// シーン内のオブジェクトを全部描画するように（シミュレート停止する時Destroyされた物を復活）
        /// </summary>
        public void RedrawGameObjects()
        {
            for (int i = 0; i < Define.NUM_LAYER; i++)
            {
                foreach (GameObject gameObject in m_gameObjects[i])
                {
                    gameObject.Active = true;
                    MainWindow.NativeMethods.InvokeWithDllProtection(() => MainWindow.NativeMethods.SetObjectDrawFlag(gameObject.Name, true));
                }
            }
        }


        /// <summary>
        /// シミュレート時カメラのアイコンを非表示
        /// </summary>
        public void SetCameraModelVisibility(bool visibility)
        {
            foreach(GameObject gameObject in m_gameObjects[Define.LAYER_CAMERA])
            {
                MainWindow.NativeMethods.InvokeWithDllProtection(() => MainWindow.NativeMethods.SetObjectDrawFlag(gameObject.Name, visibility));
            }
        }


        /// <summary>
        /// 名前に対応しているゲームオブジェクトを取得
        /// </summary>
        /// <param name="name">オブジェクトの名前</param>
        /// <returns>対象のゲームオブジェクトのインスタンス</returns>
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
