using Microsoft.VisualBasic;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using static System.Net.Mime.MediaTypeNames;

namespace ChildProcess
{
    internal class Program
    {
        private static Communication _serverSocket = null;
        public static int Main(string[] args)
        {
            _serverSocket = new Communication("");
            try
            {

                while (true)
                {
                    try
                    {
                        string recvCmd = _serverSocket.Receive();

                        if (recvCmd != null)
                        {

                            ICommandHandler tmp = CreateCommandHandler(recvCmd);
                            if (tmp != null)
                            {
                                tmp.HandleCommand(_serverSocket);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("COMMS ERROR: " + e.ToString());
                        var jsonData = new
                        {
                            FeatureName = "ERROR: " + e.ToString()
                        };

                        string jsonString = JsonConvert.SerializeObject(jsonData, Formatting.Indented);
                        Console.WriteLine(jsonString);
                        _serverSocket.Send(jsonString);

                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: " + ex.ToString());
                var jsonData = new
                {
                    FeatureName = "ERROR: " + ex.ToString()
                };

                string jsonString = JsonConvert.SerializeObject(jsonData, Formatting.Indented);
                Console.WriteLine(jsonString);
                _serverSocket.Send(jsonString);
            }

            return 0;
        }
        private static ICommandHandler CreateCommandHandler(string command)
        {
            try
            {
                if (command.StartsWith("FileExplorer"))
                {
                    string tmp = command.Substring(13);
                    return new FileExplorer(tmp);

                }
                if (command.StartsWith("Terminal"))
                {
                    string tmp = command.Substring(9);
                    return new Terminal(tmp);

                }
                switch (command)
                {
                    case "ProcessesInfo":
                        return new ProcessesInfo();
                    case "ScreenCapture":
                        return new ScreenCapture();
                    default:
                        return null;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("CmdHandler ERROR: " + e.ToString());
                return null;
            }
        }

    }
}