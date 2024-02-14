////////////////////////////////////////
///
///  GameLoopクラス
///  
///  機能：ゲームループを制御するクラス
/// 
////////////////////////////////////////
using System;
using System.Threading.Tasks;

namespace GameEngine.GameLoop
{
    class GameLoop
    {
        private Game m_game;

        public bool m_running { get; private set; }

        private bool m_simulate { get; set; }


        /// <summary>
        /// シミュレート用ゲーム環境をロード
        /// </summary>
        /// <param name="game">ゲームのインスタンス</param>
        public void Load(Game game) { m_game = game; }


        /// <summary>
        /// ゲームループを初期化
        /// </summary>
        public async void Start()
        {
            if (m_game == null)
                throw new ArgumentException("Game not loaded");

            m_game.Init();

            m_running = true;

            m_simulate = false;

            DateTime prevTime = DateTime.Now;

            while (m_running)
            {
                if (m_simulate)
                {
                    TimeSpan gameTime = DateTime.Now - prevTime;
                    prevTime = prevTime + gameTime;
                    m_game.Update(gameTime);
                }

                await Task.Delay(8);
            }
        }


        /// <summary>
        /// シミュレートを開始させる
        /// </summary>
        public void Play()
        {
            m_game.SetCameraModelVisibility(false);
            MainWindow.NativeMethods.InvokeWithDllProtection(() => MainWindow.NativeMethods.ChangeActiveCamera());
            m_simulate = true;
            m_game.m_firstFrame = true;
        }


        /// <summary>
        /// シミュレートを停止させる
        /// </summary>
        public void Stop()
        {
            m_simulate = false;
            m_game.RedrawGameObjects();
            m_game.SetCameraModelVisibility(true);
            MainWindow.NativeMethods.InvokeWithDllProtection(() => MainWindow.NativeMethods.ChangeActiveCamera());
        }


        /// <summary>
        /// ゲームループを終了
        /// </summary>
        public void Quit()
        {
            m_running = false;
            m_game?.Uninit();
        }
    }
}
