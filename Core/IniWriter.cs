#nullable disable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ContextMenuManager.Core
{
    /// <summary>INI 写入</summary>
    public sealed class IniWriter
    {
        public IniWriter() { }

        public IniWriter(string filePath) => FilePath = filePath;

        public string FilePath { get; set; }

        public bool DeleteFileWhenEmpty { get; set; }

        private List<string> GetLines()
        {
            List<string> lines = new List<string>();
            if(!File.Exists(FilePath)) return lines;
            using(StreamReader reader = new StreamReader(FilePath, IniReader.GetEncoding(FilePath)))
            {
                while(!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if(line != null) lines.Add(line.Trim());
                }
            }
            return lines;
        }

        private void SetValue(string section, string key, ref string value, bool isGetValue)
        {
            if(section == null) return;
            List<string> lines = GetLines();
            string sectionLine = $"[{section}]";
            string keyLine = $"{key}={value}";
            int sectionRow = -1, keyRow = -1, nextSectionRow = -1;
            for(int i = 0; i < lines.Count; i++)
            {
                if(lines[i].StartsWith(sectionLine, StringComparison.OrdinalIgnoreCase))
                {
                    sectionRow = i; break;
                }
            }
            if(sectionRow >= 0)
            {
                for(int i = sectionRow + 1; i < lines.Count; i++)
                {
                    if(lines[i].StartsWith(";") || lines[i].StartsWith("#")) continue;
                    if(lines[i].StartsWith("["))
                    {
                        nextSectionRow = i; break;
                    }
                    if(key != null && keyRow == -1)
                    {
                        int index = lines[i].IndexOf('=');
                        if(index < 0) continue;
                        string str = lines[i].Substring(0, index).TrimEnd();
                        if(str.Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            if(isGetValue)
                            {
                                value = lines[i].Substring(index + 1).Trim();
                                return;
                            }
                            keyRow = i; continue;
                        }
                    }
                }
            }

            if(isGetValue) return;

            if(sectionRow == -1)
            {
                if(key != null && value != null)
                {
                    lines.Add(string.Empty);
                    lines.Add(sectionLine);
                    lines.Add(keyLine);
                }
            }
            else
            {
                if(keyRow == -1)
                {
                    if(key != null)
                    {
                        if(nextSectionRow != -1)
                        {
                            keyRow = nextSectionRow;
                            lines.Insert(keyRow, keyLine);
                        }
                        else lines.Add(keyLine);
                    }
                    else
                    {
                        int count = nextSectionRow == -1 ? lines.Count - sectionRow : nextSectionRow - sectionRow;
                        lines.RemoveRange(sectionRow, count);
                    }
                }
                else
                {
                    if(value != null) lines[keyRow] = keyLine;
                    else lines.RemoveAt(keyRow);
                }
            }

            Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
            Encoding encoding = IniReader.GetEncoding(FilePath);
            File.WriteAllLines(FilePath, lines.ToArray(), encoding);

            if(DeleteFileWhenEmpty && lines.TrueForAll(line => line.IsNullOrWhiteSpace()))
                File.Delete(FilePath);
        }

        public void SetValue(string section, string key, object value) => SetValue(section, key, value.ToString());

        public void SetValue(string section, string key, string value) => SetValue(section, key, ref value, false);

        public void DeleteKey(string section, string key) => SetValue(section, key, null);

        public void DeleteSection(string section) => SetValue(section, null, null);

        public string GetValue(string section, string key)
        {
            string value = string.Empty;
            SetValue(section, key, ref value, true);
            return value;
        }
    }
}
