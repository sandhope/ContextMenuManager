#nullable disable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ContextMenuManager.Core
{
    /// <summary>INI 读取（移植自原 WinForms 版 IniReader）</summary>
    public sealed class IniReader
    {
        public IniReader() { }

        public IniReader(StringBuilder sb) => LoadStringBuilder(sb);

        public IniReader(string filePath) => LoadFile(filePath);

        private readonly Dictionary<string, Dictionary<string, string>> RootDic
            = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        public string[] Sections => RootDic.Keys.ToArray();

        public void LoadStringBuilder(StringBuilder sb)
        {
            RootDic.Clear();
            if(sb == null || sb.ToString().IsNullOrWhiteSpace()) return;
            List<string> lines = sb.ToString().Split(new[] { "\r\n", "\n" },
                StringSplitOptions.RemoveEmptyEntries).ToList();
            ReadLines(lines);
        }

        public void LoadFile(string filePath)
        {
            RootDic.Clear();
            if(!File.Exists(filePath)) return;
            List<string> lines = new List<string>();
            using(StreamReader reader = new StreamReader(filePath, GetEncoding(filePath)))
            {
                while(!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if(line == null) continue;
                    line = line.Trim();
                    if(line != string.Empty) lines.Add(line);
                }
            }
            ReadLines(lines);
        }

        private void ReadLines(List<string> lines)
        {
            lines.RemoveAll(
                line => line.StartsWith(";") || line.StartsWith("#")
                || (!line.StartsWith("[") && !line.Contains("=")));

            if(lines.Count == 0) return;

            List<int> indexs = new List<int> { 0 };
            for(int i = 1; i < lines.Count; i++)
            {
                if(lines[i].StartsWith("[")) indexs.Add(i);
            }
            indexs.Add(lines.Count);

            for(int i = 0; i < indexs.Count - 1; i++)
            {
                string section = lines[indexs[i]];
                int m = section.IndexOf(']') - 1;
                if(m < 0) continue;
                section = section.Substring(1, m);
                if(RootDic.ContainsKey(section)) continue;
                var keyValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                RootDic.Add(section, keyValues);

                for(int j = indexs[i] + 1; j < indexs[i + 1]; j++)
                {
                    int k = lines[j].IndexOf('=');
                    if(k < 0) continue;
                    string key = lines[j].Substring(0, k).TrimEnd();
                    string value = lines[j].Substring(k + 1).TrimStart();
                    if(keyValues.ContainsKey(key)) continue;
                    keyValues.Add(key, value);
                }
            }
        }

        public string GetValue(string section, string key)
        {
            if(RootDic.TryGetValue(section, out Dictionary<string, string> sectionDic))
                if(sectionDic.TryGetValue(key, out string value))
                    return value;
            return string.Empty;
        }

        public bool TryGetValue(string section, string key, out string value)
        {
            value = GetValue(section, key);
            return value != string.Empty;
        }

        public string[] GetSectionKeys(string section)
        {
            if(!RootDic.ContainsKey(section)) return null;
            return RootDic[section].Keys.ToArray();
        }

        public bool RemoveSection(string section) => RootDic.Remove(section);

        public bool RemoveKey(string section, string key)
        {
            if(RootDic.ContainsKey(section)) return RootDic[section].Remove(key);
            return false;
        }

        public void AddValue(string section, string key, string value)
        {
            if(RootDic.ContainsKey(section))
            {
                if(RootDic[section].ContainsKey(key)) RootDic[section][key] = value;
                else RootDic[section].Add(key, value);
            }
            else
            {
                var dic = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                RootDic.Add(section, dic);
                dic.Add(key, value);
            }
        }

        public void SaveFile(string filePath)
        {
            List<string> lines = new List<string>();
            foreach(var item in RootDic)
            {
                lines.Add("[" + item.Key + "]");
                foreach(var key in item.Value)
                    lines.Add(key.Key + " = " + key.Value);
                lines.Add("");
            }
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            File.WriteAllLines(filePath, lines.ToArray(), Encoding.Unicode);
        }

        internal static Encoding GetEncoding(string filePath)
        {
            if(!File.Exists(filePath)) return Encoding.Unicode;
            byte[] bytes = File.ReadAllBytes(filePath);
            if(bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE) return Encoding.Unicode;
            if(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF) return Encoding.UTF8;
            if(bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF) return Encoding.BigEndianUnicode;
            return Encoding.Default;
        }
    }
}
