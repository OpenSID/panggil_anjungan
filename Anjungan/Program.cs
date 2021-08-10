using System;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;

namespace Anjungan
{
    class Program
    {
        private const String V = "configAnjungan";

        public static string url_opensid { get; private set; }
        public static string mac_address { get; private set; }
        public static string token { get; private set; }
        public static string url_anjungan { get; private set; }

        /// https://stackoverflow.com/questions/850650/reliable-method-to-get-machines-mac-address-in-c-sharp/11138208
        /// <summary>
        /// Get the MAC of the Netowrk Interface used to connect to the specified url.
        /// </summary>
        /// <param name="allowedURL">URL to connect to.</param>
        /// <param name="port">The port to use. Default is 80.</param>
        /// <returns></returns>
        private static PhysicalAddress GetCurrentMAC(string allowedURL, int port = 80)
        {
            //create tcp client
            var client = new TcpClient();

            //start connection
            client.Client.Connect(new IPEndPoint(Dns.GetHostAddresses(allowedURL)[0], port));

            //wait while connection is established
            while (!client.Connected)
            {
                Thread.Sleep(500);
            }

            //get the ip address from the connected endpoint
            var ipAddress = ((IPEndPoint)client.Client.LocalEndPoint).Address;

            //if the ip is ipv4 mapped to ipv6 then convert to ipv4
            if (ipAddress.IsIPv4MappedToIPv6)
                ipAddress = ipAddress.MapToIPv4();

            Debug.WriteLine(ipAddress);

            //disconnect the client and free the socket
            client.Client.Disconnect(false);

            //this will dispose the client and close the connection if needed
            client.Close();

            var allNetworkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

            //return early if no network interfaces found
            if (!(allNetworkInterfaces?.Length > 0))
                return null;

            foreach (var networkInterface in allNetworkInterfaces)
            {
                //get the unicast address of the network interface
                var unicastAddresses = networkInterface.GetIPProperties().UnicastAddresses;

                //skip if no unicast address found
                if (!(unicastAddresses?.Count > 0))
                    continue;

                //compare the unicast addresses to see 
                //if any match the ip address used to connect over the network
                for (var i = 0; i < unicastAddresses.Count; i++)
                {
                    var unicastAddress = unicastAddresses[i];

                    //this is unlikely but if it is null just skip
                    if (unicastAddress.Address == null)
                        continue;

                    var ipAddressToCompare = unicastAddress.Address;

                    Debug.WriteLine(ipAddressToCompare);

                    //if the ip is ipv4 mapped to ipv6 then convert to ipv4
                    if (ipAddressToCompare.IsIPv4MappedToIPv6)
                        ipAddressToCompare = ipAddressToCompare.MapToIPv4();

                    Debug.WriteLine(ipAddressToCompare);

                    //skip if the ip does not match
                    if (!ipAddressToCompare.Equals(ipAddress))
                        continue;

                    //return the mac address if the ip matches
                    return networkInterface.GetPhysicalAddress();
                }

            }

            //not found so return null
            return null;
        }

        //https://stackoverflow.com/questions/4580263/how-to-open-in-default-browser-in-c-sharp 
        private void OpenUrl(string url)
        {
            try
            {
                Process.Start(url);
            }
            catch
            {
                // hack because of this: https://github.com/dotnet/corefx/issues/10361
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    url = url.Replace("&", "^&");
                    Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                }
                else
                {
                    throw;
                }
            }
        }

        static void Main(string[] args)
        {
            //load
            Properties config = new Properties(V);
            //get value with default value
            var url_opensid = config.get("url_opensid", "https://berputar.opensid.or.id");
            var raw_mac_address = GetCurrentMAC("www.google.com").ToString().SplitInParts(2);
            var mac_address = String.Join(":", raw_mac_address);
            token = config.get("token_layanan", "isi-token-layanan-opendesa");
            url_anjungan = url_opensid + "/index.php/layanan-mandiri/masuk?mac_address=" + mac_address + "&token_layanan=" + token;
            Console.WriteLine(url_anjungan);
            var myProgram = new Program();
            myProgram.OpenUrl(url_anjungan);
            //set value
            config.set("url_opensid", url_opensid);
            config.set("mac_address", mac_address);
            config.set("token_layanan", token);
            //save
            config.Save();
        }

    }
}
