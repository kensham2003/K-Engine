using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameEngine.GameLoop
{
    class GameLoop
    {
        private Game m_game;

        public bool m_running { get; private set; }

        private bool m_simulate { get; set; }

        public void Load(Game game) { m_game = game; }

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

        public void Play()
        {
            m_simulate = true;
            m_game.m_firstFrame = true;
        }

        public void Stop()
        {
            m_simulate = false;
        }

        public void Quit()
        {
            m_running = false;
            m_game?.Uninit();
        }
    }
}
