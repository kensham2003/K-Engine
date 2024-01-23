////////////////////////////////////////////////////////////
///
///  Settingsクラス
///  
///  機能：エンジンのユーザ設定の保存、読み込みをするクラス
/// 
////////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace GameEngine
{
    public class Settings
    {
        public Dictionary<string, string> m_settingDict = new Dictionary<string, string>();

        JsonSerializerOptions options = new JsonSerializerOptions()
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All),
            WriteIndented = true,
            IncludeFields = true,
        };

        //設定をファイルに出力
        public void Save()
        {


            string fileName = "Settings.json";
            string jsonString = JsonSerializer.Serialize(m_settingDict, options);
            File.WriteAllText(fileName, jsonString);
        }

        //設定をファイルから読み込む
        public void Read()
        {
            string fileName = "Settings.json";
            string jsonString;

            try
            {
                jsonString = File.ReadAllText(fileName);
                m_settingDict = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonString, options);
            }
            catch (Exception ex) //ファイルが存在しない場合
            {
                File.WriteAllText(fileName, "");
            }


        }

        //key valueペアを保存
        public void SaveString(string key, string value)
        {
            m_settingDict[key] = value;
            Save();
        }

        //keyが存在しているかチェック
        public bool Contains(string key)
        {
            return m_settingDict.ContainsKey(key);
        }

        //keyに対応している値を取り出す
        public string GetValue(string key)
        {
            return m_settingDict[key];
        }
    }
}
