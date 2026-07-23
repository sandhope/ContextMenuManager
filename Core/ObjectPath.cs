#nullable disable
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ContextMenuManager.Core
{
    /// <summary>文件名/路径/注册表项 工具</summary>
    static class ObjectPath
    {
        public enum PathType { File, Directory, Registry }

        private const string RegAppPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths";
        private const string ShellExecuteCommand = "mshta vbscript:createobject(\"shell.application\").shellexecute(\"";

        private static readonly char[] IllegalChars = { '/', '*', '?', '\"', '<', '>', '|' };
        private static readonly List<string> IgnoreCommandParts = new List<string> { "", "%1", "%v" };

        public static bool GetFullFilePath(string fileName, out string fullPath)
        {
            fullPath = null;
            if(fileName.IsNullOrWhiteSpace()) return false;

            foreach(string name in new[] { fileName, $"{fileName}.exe" })
            {
                foreach(string dir in new[] { "", @"%SystemRoot%\System32\", @"%SystemRoot%\" })
                {
                    if(dir != "" && (name.Contains('\\') || name.Contains(':'))) return false;
                    fullPath = Environment.ExpandEnvironmentVariables($@"{dir}{name}");
                    if(File.Exists(fullPath)) return true;
                }

                fullPath = Registry.GetValue($@"{RegAppPath}\{name}", "", null)?.ToString();
                if(File.Exists(fullPath)) return true;
            }
            fullPath = null;
            return false;
        }

        public static readonly Dictionary<string, string> FilePathDic = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public static string ExtractFilePath(string command)
        {
            if(command.IsNullOrWhiteSpace()) return null;
            if(FilePathDic.ContainsKey(command)) return FilePathDic[command];
            else
            {
                string filePath = null;
                string partCmd = Environment.ExpandEnvironmentVariables(command).Replace(@"\\", @"\");
                if(partCmd.StartsWith(ShellExecuteCommand, StringComparison.OrdinalIgnoreCase))
                {
                    partCmd = partCmd.Substring(ShellExecuteCommand.Length);
                    string[] arr = partCmd.Split(new[] { "\",\"" }, StringSplitOptions.None);
                    if(arr.Length > 0)
                    {
                        string fileName = arr[0];
                        if(GetFullFilePath(fileName, out filePath))
                        {
                            FilePathDic.Add(command, filePath);
                            return filePath;
                        }
                        if(arr.Length > 1)
                        {
                            string arguments = arr[1];
                            filePath = ExtractFilePath(arguments);
                            if(filePath != null) return filePath;
                        }
                    }
                }

                string[] strs = Array.FindAll(partCmd.Split(IllegalChars), str
                    => IgnoreCommandParts.Any(part => !part.Equals(str.Trim()))).Reverse().ToArray();

                foreach(string str1 in strs)
                {
                    string str2 = str1;
                    int index = -1;
                    do
                    {
                        List<string> paths = new List<string>();
                        string path1 = str2.Substring(index + 1);
                        paths.Add(path1);
                        if(index > 0)
                        {
                            string path2 = str2.Substring(0, index);
                            paths.Add(path2);
                        }
                        int count = paths.Count;
                        for(int i = 0; i < count; i++)
                        {
                            foreach(char c in new[] { ',', '-' })
                            {
                                if(paths[i].Contains(c)) paths.AddRange(paths[i].Split(c));
                            }
                        }
                        foreach(string path in paths)
                        {
                            if(GetFullFilePath(path, out filePath))
                            {
                                FilePathDic.Add(command, filePath);
                                return filePath;
                            }
                        }
                        str2 = path1;
                        index = str2.IndexOf(' ');
                    }
                    while(index != -1);
                }
                FilePathDic.Add(command, null);
                return null;
            }
        }

        public static string RemoveIllegalChars(string fileName)
        {
            Array.ForEach(IllegalChars, c => fileName = fileName.Replace(c.ToString(), ""));
            return fileName.Replace("\\", "").Replace(":", "");
        }

        public static bool ObjectPathExist(string path, PathType type)
        {
            switch(type)
            {
                case PathType.File:
                    return File.Exists(path);
                case PathType.Directory:
                    return Directory.Exists(path);
                case PathType.Registry:
                    using(var key = RegistryEx.GetRegistryKey(path)) return key != null;
                default:
                    return false;
            }
        }

        public static string GetNewPathWithIndex(string oldPath, PathType type, int startIndex = -1)
        {
            string newPath;
            string dirPath = type == PathType.Registry ? RegistryEx.GetParentPath(oldPath) : Path.GetDirectoryName(oldPath);
            string name = type == PathType.Registry ? RegistryEx.GetKeyName(oldPath) : Path.GetFileNameWithoutExtension(oldPath);
            string extension = type == PathType.Registry ? "" : Path.GetExtension(oldPath);

            do
            {
                newPath = $@"{dirPath}\{name}";
                if(startIndex > -1) newPath += startIndex;
                newPath += extension;
                startIndex++;
            } while(ObjectPathExist(newPath, type));
            return newPath;
        }
    }
}
