using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameEngine.GameEntity
{
    public class GameScript : Component
    {
        public string FilePath { get; set; }

        public GameScript() { }

        public GameScript(GameObject gameObject) : base(gameObject) { }
    }
}
