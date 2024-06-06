using Newtonsoft.Json;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace ChildProcess
{
    internal class FileExplorer : ICommandHandler
    {
        static string pathToView1;
        public FileExplorer(string pathToView)
        {
            pathToView1 = pathToView;
        }
        public void HandleCommand(Communication stream)
        {
            string directoryPath = pathToView1;

            try
            {
                string result = GenerateDirectoryStructure(directoryPath);
                Console.WriteLine(result);
                
                stream.Send(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }
        }
        static string GenerateDirectoryStructure(string rootDir)
        {
            Structure root = new Structure();
            root.Path = rootDir;

            try
            {
                if (File.Exists(rootDir))
                {
                    // If rootDir is a file, return its content
                    return JsonConvert.SerializeObject(new FileStructure(rootDir, File.ReadAllText(rootDir)), Formatting.Indented);
                }
                else
                {
                    DirectoryInfo directoryInfo = new DirectoryInfo(rootDir);
                    DirectoryStructure directoryStructure = new DirectoryStructure();
                    directoryStructure.Path = rootDir;
                    AddDirectoriesAndFiles(directoryStructure, directoryInfo.GetDirectories(), directoryInfo.GetFiles());
                    return JsonConvert.SerializeObject(directoryStructure, Formatting.Indented);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }

            // Return an empty string if an error occurs
            return string.Empty;
        }

        static void AddDirectoriesAndFiles(DirectoryStructure parent, DirectoryInfo[] subdirectories, FileInfo[] files)
        {
            foreach (DirectoryInfo directoryInfo in subdirectories)
            {
                parent.Directories.Add(directoryInfo.Name);
            }
            foreach (FileInfo fileInfo in files)
            {
                parent.Files.Add(fileInfo.Name);
            }
        }
    }
    class Structure
    {
        public string Path { get; set; }
        public string FeatureName { get; set; } = "FileExplorer";
    }

    class DirectoryStructure : Structure
    {
        public List<string> Directories { get; set; } = new List<string>();
        public List<string> Files { get; set; } = new List<string>();
    }

    class FileStructure : Structure
    {
        public string FileContent { get; set; }

        public FileStructure(string path, string content)
        {
            Path = path;
            FileContent = content;
        }
    }

}