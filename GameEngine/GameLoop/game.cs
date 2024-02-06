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
        //シミュレートの初フレームなのか
        public bool m_firstFrame = true;

        //ゲーム内にあるゲームオブジェクトのリスト
        public List<GameObject>[] m_gameObjects = new List<GameObject>[5];


        /// <summary>
        /// ゲームを初期化
        /// </summary>
        public void Init() 
        {
            Debug.ClearLog();

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
            //最初のフレームならBeginPlay()を呼ぶ
            if (m_firstFrame)
            {
                for (int i = 0; i < Define.NUM_LAYER; i++)
                {
                    foreach (GameObject gameObject in m_gameObjects[i])
                    {
                        gameObject.BeginPlay();
                    }
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
                        gameObject.Update(gameTime);
                    }
                }
            }

            //当たり判定の処理
            for (int i = 0; i < Define.NUM_LAYER; i++)
            {
                foreach (GameObject gameObjectA in m_gameObjects[i])
                {
                    //コライダーを持ってない場合は飛ばす
                    if (!gameObjectA.HasCollider) { continue; }

                    for (int j = 0; j < Define.NUM_LAYER; j++)
                    {
                        foreach (GameObject gameObjectB in m_gameObjects[j])
                        {
                            if(gameObjectA == gameObjectB) { continue; }

                            //コライダーを持ってない場合は飛ばす
                            if (!gameObjectB.HasCollider) { continue; }

                            //当たっている
                            if(Collision.CheckCollision(gameObjectA, gameObjectB))
                            {
                                foreach(GameScript script in gameObjectA.GameScripts)
                                {
                                    script.OnCollision(gameObjectB.Collider);
                                }
                            }
                        }
                    }
                }
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
