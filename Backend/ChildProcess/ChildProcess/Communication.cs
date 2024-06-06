using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace ChildProcess
{
    public class Communication
    {
        private Socket _clientSocket;
        public const int SERVER_PORT = 51663;
        public const string SERVER_IP = "127.0.0.1";

        public Communication(string managerName)
        {
            _clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _clientSocket.Connect(SERVER_IP, SERVER_PORT);
            //Console.WriteLine("Connected to server.");
            //string username = "qasw";
            var jsonData = new
            {
                MangerUsername = "asd",
                AgentName = Dns.GetHostName(),
            };

            string jsonString = JsonConvert.SerializeObject(jsonData, Formatting.Indented);
            //Console.WriteLine(jsonString);
            _clientSocket.Send(Encoding.UTF8.GetBytes(jsonString));
            //byte[] data = new byte[16];
            //_clientSocket.Receive(data);
            //Console.WriteLine(Encoding.UTF8.GetString(data, 0, 16));
        }

        public void Send(string message)
        {
            if (_clientSocket.Connected)
            {
                try
                {
                    byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                    byte[] messageLengthBytes = BitConverter.GetBytes(messageBytes.Length);

                    if (BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(messageLengthBytes);
                    }

                    _clientSocket.Send(messageLengthBytes);
                    _clientSocket.Send(messageBytes);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error sending message: {e.Message}");
                }
            }
            else
            {
                throw new Exception("Socket is not connected.");
            }
        }

        public string Receive()
        {
            if (!_clientSocket.Connected )
            {
                throw new Exception("Socket is not connected.");
            }
            try
            {
                byte[] lengthBytes = new byte[4];
                _clientSocket.Receive(lengthBytes);
                
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(lengthBytes);
                }

                int messageLength = BitConverter.ToInt32(lengthBytes, 0);
                if(messageLength == 0)
                {
                    return null;
                }
                byte[] messageBytes = new byte[messageLength];
                int bytesRead = _clientSocket.Receive(messageBytes);

                return Encoding.UTF8.GetString(messageBytes, 0, bytesRead);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error receiving message: {e.Message}");
                return null;
            }
        }

        public void Close()
        {
            if (_clientSocket != null && _clientSocket.Connected)
            {
                _clientSocket.Shutdown(SocketShutdown.Both);
                _clientSocket.Close();
            }
        }
    }
}
