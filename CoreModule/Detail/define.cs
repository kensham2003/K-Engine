////////////////////////////////////////
///
///  Detailクラス
///  
///  機能：各種定数を定義するクラス
/// 
////////////////////////////////////////
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System;

namespace GameEngine.Detail
{
    public static class Define
    {
        public const int NUM_LAYER = 5;

        public const int LAYER_CAMERA = 0;
        public const int LAYER_3D_OBJECT = 1;
        public const int LAYER_EFFECT = 2;
        public const int LAYER_2D_OBJECT = 3;
        public const int LAYER_FADE = 4;

        public static readonly IList<string> preDefinedComponents = new ReadOnlyCollection<string>(
            new List<string> {
                "BoxCollider",
                "SphereCollider",
                "Camera",
                "MainCamera",
                "Field"
            }
            );

        public static readonly IList<string> preDefinedColliders = new ReadOnlyCollection<string>(
            new List<string>
            {
                "BoxCollider",
                "SphereCollider"
            }
            );

        public static string AddSpacesToString(string input)
        {
            return string.Concat(input.Select(x => Char.IsUpper(x) ? " " + x : x.ToString())).TrimStart(' ');
        }
    }
}
