using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameEngine.GameLoop
{
    class gameLoop
    {
        private Game m_game;

        public bool m_running { get; private set; }

        public void Load(Game game) { m_game = game; }

        public async void Start()
        {
            if (m_game == null)
                throw new ArgumentException("Game not loaded");

            m_game.Init();

            m_running = true;

            DateTime prevTime = DateTime.Now;

            while (m_running)
            {
                TimeSpan gameTime = DateTime.Now - prevTime;

                prevTime = prevTime + gameTime;

                m_game.Update(gameTime);

                await Task.Delay(8);
            }
        }

        public void Stop()
        {
            m_running = false;
            m_game?.Uninit();
        }
    }
}
