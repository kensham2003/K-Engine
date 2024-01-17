////////////////////////////////////////
///
///  Inputクラス
///  
///  機能：ユーザのキー入力を管理するクラス
/// 
////////////////////////////////////////
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace GameEngine.Detail
{
    public class Input
    {
        public abstract class Keyboard
        {
            [Flags]
            private enum KeyStates
            {
                None = 0,
                Down = 1,
                Toggled = 2
            }

            [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
            private static extern short GetKeyState(int keyCode);


            /// <summary>
            /// キーの状態を取得
            /// </summary>
            /// <param name="key">キー名</param>
            /// <returns></returns>
            private static KeyStates GetKeyState(Keys key)
            {
                KeyStates state = KeyStates.None;

                short retVal = GetKeyState((int)key);

                //If the high-order bit is 1, the key is down
                //otherwise, it is up.
                if ((retVal & 0x8000) == 0x8000)
                    state |= KeyStates.Down;

                //If the low-order bit is 1, the key is toggled.
                if ((retVal & 1) == 1)
                    state |= KeyStates.Toggled;

                return state;
            }


            /// <summary>
            /// キーが押されているかをチェック
            /// </summary>
            /// <param name="key">キー名</param>
            /// <returns></returns>
            public static bool IsKeyDown(Keys key)
            {
                return KeyStates.Down == (GetKeyState(key) & KeyStates.Down);
            }


            /// <summary>
            /// キーの今フレームの状態が変わったか
            /// </summary>
            /// <param name="key">キー名</param>
            /// <returns></returns>
            public static bool IsKeyToggled(Keys key)
            {
                return KeyStates.Toggled == (GetKeyState(key) & KeyStates.Toggled);
            }
        }
    }

}
