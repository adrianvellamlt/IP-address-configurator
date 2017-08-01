using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Newtonsoft.Json;
using static System.String;
using System.Runtime.InteropServices;

namespace IPAddressConfigurator
{
    class Program
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_HIDE = 0;
        
        private static IPAddress LocalIPAddress()
        {
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                return null;
            }

            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());

            return host
                .AddressList
                .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
        }

        private static dynamic SerializeJson()
        {
            using (StreamReader r = new StreamReader(Directory.GetCurrentDirectory() + "\\settings.json"))
            {
                return JsonConvert.DeserializeObject<dynamic>(r.ReadToEnd());
            }
        }

        static void Main(string[] args)
        {
            #region Initialisation
            dynamic json = SerializeJson();
            string ip = json.StaticIP.Value;
            var subnet = Join(".", ip.Split('.').Take(3));
            var handle = GetConsoleWindow();
            ShowWindow(handle, SW_HIDE);
            #endregion

            var isDhcpEnabled = Process.Start(new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments =
                    "interface ip set address \"" + json.ConnectionName.Value + "\" dhcp",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden
            })?.StandardOutput.ReadToEnd().Contains(json.ReturnMessage.Value) ?? false;
            
            if (isDhcpEnabled)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments =
                        "interface ip set address name=\"" + json.ConnectionName.Value + "\" static "
                        + json.StaticIP.Value + " " + json.Subnet.Value + " " + subnet + ".254",
                    Verb = "runas",
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                });
            }
            else
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments =
                        "interface ip set address \"" + json.ConnectionName.Value + "\" dhcp",
                    Verb = "runas",
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                });
            }
        }
    }
}
