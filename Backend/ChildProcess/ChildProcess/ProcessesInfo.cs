using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ChildProcess
{
    internal class ProcessesInfo : ICommandHandler
    {
        public ProcessesInfo()
        {
        }

        public void HandleCommand(Communication stream)
        {
            Process[] localProcesses = Process.GetProcesses();
            List<ProcessInfo> ourProcesses = new List<ProcessInfo>();

            Parallel.ForEach(localProcesses, process =>
            {
                if (!process.ProcessName.StartsWith("Service") &&
                    !process.ProcessName.StartsWith("winlogon") &&
                    process.SessionId != 0)
                {
                    string processPath = GetProcessFilePath(process);
                    if (!string.IsNullOrEmpty(processPath))
                    {
                        string processOwner = GetProcessOwner(process);
                        TimeSpan processUptime = DateTime.Now - process.StartTime;
                        float cpuUsage = GetProcessCPUUsage(process);
                        long memoryUsage = process.PrivateMemorySize64;

                        if (!IsSystemStartedProcess(process.ProcessName, processOwner, processPath))
                        {
                            lock (ourProcesses)
                            {
                                ourProcesses.Add(new ProcessInfo
                                {
                                    Id = process.Id,
                                    Name = process.ProcessName,
                                    FilePath = processPath,
                                    Owner = processOwner,
                                    Uptime = processUptime.ToString(),
                                    CPUUsage = cpuUsage,
                                    MemoryUsage = memoryUsage
                                });
                            }
                        }
                    }
                }
            });
            var result = new
            {
                FeatureName = "ProcessesInfo",
                Processes = ourProcesses
            };

            string jsonData = JsonConvert.SerializeObject(result, Formatting.Indented);
            Console.WriteLine(jsonData);

            stream.Send(jsonData);
        }

        static string GetProcessOwner(Process process)
        {
            try
            {
                string query = $"SELECT * FROM Win32_Process WHERE ProcessId = {process.Id}";
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(query))
                {
                    ManagementObjectCollection processList = searcher.Get();
                    foreach (ManagementObject obj in processList)
                    {
                        string[] ownerInfo = new string[2];
                        obj.InvokeMethod("GetOwner", (object[])ownerInfo);
                        return ownerInfo[0];
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving process owner: {ex.Message}");
            }
            return "Unknown";
        }

        static string GetProcessFilePath(Process process)
        {
            try
            {
                return process.MainModule.FileName;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving process file path: {ex.Message}");
                return string.Empty;
            }
        }

        static float GetProcessCPUUsage(Process process)
        {
            return process.TotalProcessorTime.Ticks / (float)TimeSpan.TicksPerSecond;
        }

        static bool IsSystemStartedProcess(string processName, string processOwner, string processFilePath)
        {
            return processOwner.Equals("SYSTEM", StringComparison.OrdinalIgnoreCase) &&
                   processName.StartsWith("system", StringComparison.OrdinalIgnoreCase) &&
                   processFilePath.StartsWith(Environment.SystemDirectory, StringComparison.OrdinalIgnoreCase);
        }

        internal class ProcessInfo
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string FilePath { get; set; }
            public string Owner { get; set; }
            public string Uptime { get; set; }
            public float CPUUsage { get; set; }
            public long MemoryUsage { get; set; }
        }
    }
}
