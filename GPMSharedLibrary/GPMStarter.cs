using GPMSharedLibrary.Helpers;
using GPMSharedLibrary.Models;
using Newtonsoft.Json;
using OpenQA.Selenium.Chrome;
using System;
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
        /// <param name="customFlag"></param>
        /// <param name="hideConsole"></param>
        /// <returns></returns>
        public static ChromeDriver StartProfile(string browserExePath, string profilePath, int remotePort, ProfileInfo profileInfo, string customFlag = null, string fileNameDriver= "gpmdriver.exe", int delayReturn=1000, bool hideConsole=true)
        {
            //kiểm tra đầu vào
            if (string.IsNullOrEmpty(profileInfo?.GPMKey))
                throw new Exception("profileInfo.GPMKey null or empty");

            //Bước 1: Tạo thư mục profile
            if (!Directory.Exists(profilePath))
                Directory.CreateDirectory(profilePath);

            //Bước 2: Tạo/ghi đè file GPM
            if (!Directory.Exists(Path.Combine(profilePath, "Default")))
                Directory.CreateDirectory(Path.Combine(profilePath, "Default"));
            WriteGPMFile(profilePath, profileInfo);
            if (!File.Exists(profilePath + "\\Default\\gpm"))
                throw new Exception($"{profilePath}\\Default\\gpm file not found.");

            //Bước 3: Copy thư mục extension
            CopyCookieExtension(profilePath);
            string cookieExtensionPath = Path.Combine(profilePath, "BrowserExtensions", "cookies-ext");

            //Bước 4: Khởi động Chrome
            string param = $"--user-data-dir=\"{profilePath}\" --lang={profileInfo.AcceptLanguage} --disable-encryption --user-agent=\"{profileInfo.UserAgent}\" --no-default-browser-check --uniform2f-noise={profileInfo.WebGLUniform2fNoise} --load-extension=\"{cookieExtensionPath}\" --max-vertex-uniform={profileInfo.MaxVertexUniform} --max-fragment-uniform={profileInfo.MaxFragmentUniform}";
            
            if (!string.IsNullOrEmpty(profileInfo.Proxy))
            {
                ProxyInfo proxyInfo = new ProxyInfo(profileInfo.Proxy);
                string proxyParam = $" --proxy-server={proxyInfo.Host}:{proxyInfo.Port}";
                if (proxyInfo.Type == "Socks 5") proxyParam = $" --proxy-server=socks5://{proxyInfo.Host}:{proxyInfo.Port}";
                else if (proxyInfo.Type == "Socks 4") proxyParam = $" --proxy-server=socks://{proxyInfo.Host}:{proxyInfo.Port}";

                param += $" {proxyParam}";
            }
            if (!string.IsNullOrEmpty(customFlag))
                param += " " + customFlag.Trim();

            param += $" --remote-debugging-port={remotePort}";
            Process.Start(browserExePath, param);

            Thread.Sleep(delayReturn);

            // Remote by driver
            ChromeDriverService service = ChromeDriverService.CreateDefaultService(AppDomain.CurrentDomain.BaseDirectory, fileNameDriver);
            service.HideCommandPromptWindow = hideConsole;
            ChromeOptions options = new ChromeOptions();
            options.DebuggerAddress = "127.0.0.1:" + remotePort;
            
            ChromeDriver driver = new ChromeDriver(service, options);

            return driver;
        }

        static void WriteGPMFile(string profilePath, ProfileInfo profileInfo)
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
                    writer.WritePropertyName("gpm");
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

                File.WriteAllText(Path.Combine(profilePath, "Default", "gpm"), sb.ToString());
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
