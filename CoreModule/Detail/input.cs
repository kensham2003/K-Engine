////////////////////////////////////////
///
///  Inputクラス
///  
///  機能：ユーザのキー入力を管理するクラス
/// 
////////////////////////////////////////
using System;
using System.Collections.Generic;
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

            //別のAppDomainでも使える（なぜか）
            [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
            private static extern short GetKeyState(int keyCode);

            //なぜか別のAppDomainではキー入力取れないので使わない
            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool GetKeyboardState(byte[] lpKeyState);

            //今フレームのキー状態
            private static Dictionary<Keys, bool> m_keyState = new Dictionary<Keys, bool>();

            //前フレームのキー状態
            private static Dictionary<Keys, bool> m_oldKeyState = new Dictionary<Keys, bool>();


            /// <summary>
            /// キーの状態を更新
            /// </summary>
            public static void UpdateKeyState()
            {
                m_oldKeyState = new Dictionary<Keys, bool>(m_keyState);
                foreach(Keys key in Enum.GetValues(typeof(Keys)))
                {
                    m_keyState[key] = IsKeyDown(key);
                }
            }


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
                //byte keyCode = GetVirtualKeyCode(key);
                //return ((m_keyState[keyCode] & 0x80) != 0);
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


            /// <summary>
            /// キーが今フレーム押され始めたか
            /// </summary>
            /// <param name="key">キー名</param>
            /// <returns></returns>
            public static bool IsKeyTriggered(Keys key)
            {
                return (m_keyState[key] && !m_oldKeyState[key]);
            }


            /// <summary>
            /// キーが今フレーム離され始めたか
            /// </summary>
            /// <param name="key">キー名</param>
            /// <returns></returns>
            public static bool IsKeyReleased(Keys key)
            {
                return (!m_keyState[key] && m_oldKeyState[key]);
            }
        }
    }

}
