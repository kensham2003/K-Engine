using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameEngine.GameEntity
{
    [Serializable]
    public class GameScriptPropInfo
    {
        public int PropAmount { get; set; }

        //need to convert to type during serialization/deserialization
        public List<string> PropTypes = new List<string>();

        public List<string> PropNames = new List<string>();

        public List<string> PropValues = new List<string>();
    }
}
