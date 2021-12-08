using GPMSharedLibrary.Helpers;
using GPMSharedLibrary.Models;
using Newtonsoft.Json;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace GPMLogin
{
    public class GPMStarter
    {
        /// <summary>
        /// Create ChromeDriver to remote browser
        /// </summary>
        /// <param name="browserExePath"></param>
        /// <param name="profilePath"></param>
        /// <param name="remotePort"></param>
        /// <param name="profileInfo"></param>
        /// <param name="fileNameDriver"></param>
        /// <param name="delayReturn">Delay before return (depend on hardware fast)</param>
        /// <param name="customFlags"></param>
        /// <param name="hideConsole"></param>
        /// <returns></returns>
        public static ChromeDriver StartProfile(string browserExePath, ProfileInfo profileInfo, string fileNameDriver= "gpmdriver.exe", int delayReturn=1000, bool hideConsole=true, string startUrl=null)
        {
            // Check params
            if (string.IsNullOrEmpty(profileInfo?.GPMKey))
                throw new Exception("profileInfo.GPMKey null or empty");
            if(string.IsNullOrEmpty(profileInfo?.ProfilePath))
                throw new Exception("Must be set profile path");

            //Step 1: Create folder profile
            if (!Directory.Exists(profileInfo.ProfilePath))
                Directory.CreateDirectory(profileInfo.ProfilePath);

            //Step 2: Override file gpm
            if (!Directory.Exists(Path.Combine(profileInfo.ProfilePath, "Default")))
                Directory.CreateDirectory(Path.Combine(profileInfo.ProfilePath, "Default"));
            WriteGPMFile(profileInfo.ProfilePath, profileInfo, profileInfo.ConfigName);

            if (!File.Exists(profileInfo.ProfilePath + $"\\Default\\{profileInfo.ConfigName}"))
                throw new Exception($"{profileInfo.ProfilePath}\\Default\\{profileInfo.ConfigName} file not found.");

            //Step 3: set params to gpm browser
            List<string> parmas = new List<string>();
            parmas.Add($"--user-data-dir=\"{profileInfo.ProfilePath}\"");
            parmas.Add($"--disable-encryption");
            parmas.Add($"--user-agent=\"{profileInfo.UserAgent}\"");
            parmas.Add($"--no-default-browser-check");
            parmas.Add($"--uniform2f-noise={profileInfo.WebGLUniform2fNoise}");
            parmas.Add($"--max-vertex-uniform={profileInfo.MaxVertexUniform}");
            parmas.Add($"--max-fragment-uniform={profileInfo.MaxFragmentUniform}");

            if (!string.IsNullOrEmpty(profileInfo.AcceptLanguage))
                parmas.Add($"--lang={profileInfo.AcceptLanguage}");
            if (!string.IsNullOrEmpty(profileInfo.ConfigName))
                parmas.Add($"--config-name={profileInfo.ConfigName}");
            if (!string.IsNullOrEmpty(profileInfo.WebGLVendor))
                parmas.Add($"--webgl-vendor=\"{profileInfo.WebGLVendor}\"");
            if (!string.IsNullOrEmpty(profileInfo.WebGLRender))
                parmas.Add($"--webgl-renderer=\"{profileInfo.WebGLRender}\"");

            if (profileInfo.Extensions != null && profileInfo.Extensions.Count > 0)
                parmas.Add($"--load-extension=\"{string.Join(",", profileInfo.Extensions)}\"");

            if (!string.IsNullOrEmpty(profileInfo.Proxy))
            {
                ProxyInfo proxyInfo = new ProxyInfo(profileInfo.Proxy);
                string proxyParam = $"--proxy-server={proxyInfo.Host}:{proxyInfo.Port}";
                if (proxyInfo.Type == "Socks 5") proxyParam = $"--proxy-server=socks5://{proxyInfo.Host}:{proxyInfo.Port}";
                else if (proxyInfo.Type == "Socks 4") proxyParam = $"--proxy-server=socks://{proxyInfo.Host}:{proxyInfo.Port}";

                parmas.Add($"{proxyParam}");
            }

            if (profileInfo.CustomFlags != null && profileInfo.CustomFlags.Count > 0)
                parmas.AddRange(profileInfo.CustomFlags);

            parmas.Add($"--remote-debugging-port={profileInfo.RemotePort}");
            if (!string.IsNullOrEmpty(startUrl))
                parmas.Add(startUrl);

            string param = string.Join(" ", parmas);

            Process.Start(browserExePath, param);

            Thread.Sleep(delayReturn);

            // Remote by driver
            ChromeDriverService service = ChromeDriverService.CreateDefaultService(AppDomain.CurrentDomain.BaseDirectory, fileNameDriver);
            service.HideCommandPromptWindow = hideConsole;
            ChromeOptions options = new ChromeOptions();
            options.DebuggerAddress = "127.0.0.1:" + profileInfo.RemotePort;
            
            ChromeDriver driver = new ChromeDriver(service, options);

            return driver;
        }

        static void WriteGPMFile(string profilePath, ProfileInfo profileInfo, string configName)
        {
            try
            {
                ProxyInfo proxyInfo = new ProxyInfo(profileInfo.Proxy);

                // Write json
                StringBuilder sb = new StringBuilder();
                StringWriter sw = new StringWriter(sb);

                using (JsonWriter writer = new JsonTextWriter(sw))
                {
                    writer.WriteStartObject();
                    writer.WritePropertyName(configName);
                    writer.WriteStartObject();

                    // Name
                    writer.WritePropertyName("name");
                    writer.WriteValue(profileInfo.Name ?? "GPMLogin");

                    // User agent
                    writer.WritePropertyName("userAgent");
                    writer.WriteValue(profileInfo.UserAgent ?? "");

                    // Timezone

                    writer.WritePropertyName("timezone");
                    writer.WriteValue(profileInfo.Timezone ?? "");

                    // Font
                    writer.WritePropertyName("fonts");
                    writer.WriteStartArray();
                    /*
                    foreach (string font in profileInfo.FontList)
                    {
                        writer.WriteValue(font);
                    }
                    */
                    writer.WriteEndArray();

                    writer.WritePropertyName("fonts_exclude");
                    writer.WriteStartArray();

                    writer.WriteEndArray();

                    // Screen
                    writer.WritePropertyName("screen");
                    writer.WriteStartObject();
                    writer.WritePropertyName("height");
                    writer.WriteValue(profileInfo.ScreenHeight);
                    writer.WritePropertyName("width");
                    writer.WriteValue(profileInfo.ScreenWidth);
                    writer.WritePropertyName("availHeight");
                    writer.WriteValue(profileInfo.AvailScreenHeight);
                    writer.WritePropertyName("availWidth");
                    writer.WriteValue(profileInfo.AvailScreenWidth);
                    writer.WriteEndObject();

                    // Navigator
                    writer.WritePropertyName("navigator");
                    writer.WriteStartObject();
                    writer.WritePropertyName("processorCount");
                    writer.WriteValue(profileInfo.ProcessorCount);
                    writer.WriteEndObject();

                    // Audio
                    writer.WritePropertyName("audio");
                    writer.WriteStartObject();
                    writer.WritePropertyName("noise");
                    writer.WriteValue(profileInfo.AudioNoise);
                    writer.WriteEndObject();

                    // Webgl
                    writer.WritePropertyName("webgl");
                    writer.WriteStartObject();
                    writer.WritePropertyName("canvasNoise");
                    writer.WriteValue(profileInfo.WebGLNoise);
                    writer.WritePropertyName("clientRectNoise");
                    writer.WriteValue(profileInfo.WebGLRectNoise);
                    writer.WritePropertyName("uniform2fNoise");
                    writer.WriteValue(profileInfo.WebGLUniform2fNoise);
                    writer.WriteEndObject();

                    // License
                    writer.WritePropertyName("license");
                    writer.WriteStartObject();
                    writer.WritePropertyName("key");
                    writer.WriteValue(profileInfo.GPMKey ?? "");
                    writer.WritePropertyName("machineId");
                    writer.WriteValue("");
                    writer.WritePropertyName("thirdparty_key");
                    writer.WriteValue(profileInfo.ThirdPartyKey ?? "");
                    writer.WriteEndObject();

                    // Proxy
                    ProxyInfo proxy = new ProxyInfo(profileInfo.Proxy);

                    writer.WritePropertyName("proxyAuth");
                    writer.WriteStartObject();
                    writer.WritePropertyName("autoAuth");
                    writer.WriteValue(string.IsNullOrEmpty(proxy.UserName) ? false : true);
                    writer.WritePropertyName("username");
                    writer.WriteValue(proxy.UserName ?? "");
                    writer.WritePropertyName("password");
                    writer.WriteValue(proxy.Password ?? "");
                    writer.WriteEndObject();

                    // Brand
                    writer.WritePropertyName("brand");
                    writer.WriteStartObject();
                    writer.WritePropertyName("version");
                    writer.WriteValue(profileInfo.BrowseVersion ?? "");
                    //writer.WriteValue("96.0.4606.81");
                    writer.WriteEndObject();

                    // WebRTC
                    writer.WritePropertyName("webRTC");
                    writer.WriteStartObject();
                    writer.WritePropertyName("mode");
                    writer.WriteValue((profileInfo.WebRTC?.Mode ?? WebRTCMode.Disable).ToString().ToLower());
                    writer.WritePropertyName("publicIP");
                    writer.WriteValue(profileInfo.WebRTC?.PublicIP?.ToString()?.ToLower() ?? "127.0.0.1");
                    writer.WriteEndObject();

                    // Advance
                    writer.WritePropertyName("advance");
                    writer.WriteStartObject();
                    writer.WritePropertyName("maxVertexUniform");
                    writer.WriteValue(profileInfo.MaxVertexUniform);
                    writer.WritePropertyName("maxFragmentUniform");
                    writer.WriteValue(profileInfo.MaxFragmentUniform);
                    writer.WriteEndObject();

                    writer.WriteEndObject();
                }

                File.WriteAllText(Path.Combine(profilePath, "Default", configName), sb.ToString());
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Copy Cookie Extension
        /// </summary>
        /// <param name="dbProfile"></param>
        /// <param name="profilePath"></param>
        static void CopyCookieExtension(string profilePath)
        {
            try
            {
                string extensionPath = Path.Combine(profilePath, "BrowserExtensions", "cookies-ext");
                if (!Directory.Exists(extensionPath))
                    Directory.CreateDirectory(extensionPath);
                DeepCopy.DirectoryCopy(new DirectoryInfo("BrowserExtensions\\cookies-ext"), new DirectoryInfo(extensionPath));

            }
            catch { }
        }
    }
}
