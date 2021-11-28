using Newtonsoft.Json;
using System;

namespace GPMSharedLibrary.Models
{
    internal class ProxyInfo
    {
        /// <summary>
        /// Proxy, Socks 4, Socks 5  -- Do not change, it is fixed!
        /// </summary>
        public string Type { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }

        /// <summary>
        /// Raw string can be: IP:Port or IP:Port:User:Pass
        /// </summary>
        /// <param name="proxyRawString"></param>
        /// <returns></returns>
        public ProxyInfo(string proxyString)
        {
            // Default (some times, app can be dis because Host is null)
            this.Host = "No";
            this.Type = "Proxy";
            string proxyRawString = proxyString;

            if (string.IsNullOrEmpty(proxyRawString))
                return;

            string prefix = "";
            if (proxyRawString.IndexOf("socks5://") == 0)
            {
                prefix = "socks5://";
                this.Type = "Socks 5";
                proxyRawString = proxyRawString.Replace(prefix, "");
            }
            if (proxyRawString.IndexOf("socks://") == 0)
            {
                prefix = "socks://";
                this.Type = "Socks 4";
                proxyRawString = proxyRawString.Replace(prefix, "");
            }

            string[] spliter = proxyRawString.Split(':');
            if (spliter.Length == 2)
            {
                this.Host = spliter[0];
                int port = 80;
                int.TryParse(spliter[1], out port);
                this.Port = port;
            }
            else if (spliter.Length == 4)
            {
                this.Host = spliter[0];
                int port = 80;
                int.TryParse(spliter[1], out port);
                this.Port = port;
                this.UserName = spliter[2];
                this.Password = spliter[3];
            }
        }
    }
}
