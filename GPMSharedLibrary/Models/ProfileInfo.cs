using GPMSharedLibrary.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Text;

namespace GPMSharedLibrary.Models
{
    public class ProfileInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        /// <summary>
        /// Custom file Config name and property name in file config. Default is "gpm"
        /// </summary>
        public string ConfigName { get; set; } = "gpm";

        public string ProfilePath { get; set; }

        public ushort RemotePort { get; set; }

        public string GPMKey { get; set; }

        public string ThirdPartyKey { get; set; }

        // Browser info
        public string UserAgent { get; set; }
        //public string WinVersion { get; set; }
        //public string WinPlatform { get; set; }
        //public string BrowseName { get; set; }
        /// <summary>
        /// Suggest: 93.xx.xxxx.xx, 94.x.xxxx.xxx, 95.xxx.xxxx.xxx
        /// </summary>
        public string BrowseVersion { get; set; }

        public string AcceptLanguage { get; set; }
        public string Proxy { get; set; }
        public string Timezone { get; set; }
        public List<string> ExcludeFontList { get; set; }

        // Screen
        /// <summary>
        /// Not set: -1
        /// </summary>
        public int ScreenWidth { get; set; } = -1;
        /// <summary>
        /// Not set: -1
        /// </summary>
        public int ScreenHeight { get; set; } = -1;
        /// <summary>
        /// Not set: -1
        /// </summary>
        public int AvailScreenWidth { get; set; } = -1;
        /// <summary>
        /// Not set: -1
        /// </summary>
        public int AvailScreenHeight { get; set; } = -1;

        // Hardware

        /// <summary>
        /// Not set: -1
        /// </summary>
        public int ProcessorCount { get; set; } = -1;

        /// <summary>
        /// Not set: -1
        /// Range: 0.0001 -> 0.9990
        /// </summary>
        public double WebGLNoise { get; set; } = -1;
        /// <summary>
        /// Not set: -1
        /// Range: 0.0001 -> 0.0999
        /// </summary>
        public double WebGLRectNoise { get; set; } = -1;

        public string WebGLVendor { get; set; }
        public string WebGLRender { get; set; }
        /// <summary>
        /// Not set: -1
        /// Range: 0.001 -> 0.199 (total 4 number. Eg error 0.0001)
        /// </summary>
        public double WebGLUniform2fNoise { get; set; }
        /// <summary>
        /// Not set: -1
        /// Range: 0.0001 -> 0.9999
        /// </summary>
        public double AudioNoise { get; set; } = -1;
        /// <summary>
        /// Range: 3000-4500
        /// </summary>
        public int MaxVertexUniform { get; set; }
        /// <summary>
        /// Range: 900->1500
        /// </summary>
        public int MaxFragmentUniform {get; set; }

        // Other setting
        public List<string> CustomFlags { get; set; }
        public List<string> Extensions { get; set; }

        public WebRTC WebRTC { get; set; } = new WebRTC();

        public ProfileInfo()
        {
            Random _rand = new Random();

            // Default data
            this.ExcludeFontList = new List<string>();
            this.AcceptLanguage = "en-US";
            this.Timezone = "Asia/Bangkok";
            this.CustomFlags = new List<string>();
            this.Extensions = new List<string>();

            this.RemotePort = (ushort)_rand.Next(6000, 9000);
        }

        public void ParseFromGPMFile(string file)
        {
            string json = File.ReadAllText(file);
            dynamic jsonObj = JsonConvert.DeserializeObject<dynamic>(json);

            this.Name = Convert.ToString(jsonObj.gpm.name);
            this.UserAgent = Convert.ToString(jsonObj.gpm.userAgent);
            this.Timezone = Convert.ToString(jsonObj.gpm.timezone);
            this.ExcludeFontList = JsonConvert.DeserializeObject<List<string>>(Convert.ToString(jsonObj.gpm.fonts));
            this.ScreenHeight = Convert.ToInt32(jsonObj.gpm.screen.height);
            this.ScreenWidth = Convert.ToInt32(jsonObj.gpm.screen.width);
            this.AvailScreenHeight = Convert.ToInt32(jsonObj.gpm.screen.availHeight);
            this.AvailScreenWidth = Convert.ToInt32(jsonObj.gpm.screen.availWidth);
            this.ProcessorCount = Convert.ToInt32(jsonObj.gpm.navigator.processorCount);

            this.AudioNoise = Convert.ToDouble(jsonObj.gpm.audio.noise);
            this.WebGLNoise = Convert.ToDouble(jsonObj.gpm.webgl.canvasNoise);
            this.WebGLRectNoise = Convert.ToDouble(jsonObj.gpm.webgl.clientRectNoise);
            this.WebGLUniform2fNoise = Convert.ToDouble(jsonObj.gpm.webgl.uniform2fNoise);

            this.BrowseVersion = Convert.ToString(jsonObj.gpm.brand.version);

            this.MaxFragmentUniform = Convert.ToInt32(jsonObj.gpm.advance.maxFragmentUniform);
            this.MaxVertexUniform = Convert.ToInt32(jsonObj.gpm.advance.maxVertexUniform);
        }

        public string CopyCookieExtension()
        {
            try
            {
                string dirPath = Path.Combine(this.ProfilePath, "Default", "GPMBrowserExtenions", "cookies-ext");
                if (!Directory.Exists(dirPath))
                    Directory.CreateDirectory(dirPath);
                DeepCopy.DirectoryCopy(new DirectoryInfo("BrowserExtensions\\cookies-ext"), new DirectoryInfo(dirPath));

                this.ConfigGpmCommandToCookieExtension("");

                return dirPath;
            }
            catch 
            {
                throw;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="profileInfo"></param>
        /// <param name="command"></param>
        private void ConfigGpmCommandToCookieExtension(string command)
        {
            try
            {
                if (string.IsNullOrEmpty(this.Id))
                    throw new Exception("profile id is null or emplty");
                string cookieExtensionPath = Path.Combine(this.ProfilePath, "Default", "GPMBrowserExtenions", "cookies-ext");
                if (!Directory.Exists(cookieExtensionPath))
                    throw new Exception("Folder cookie-extension not exits");
                string gpmCommandFile = Path.Combine(cookieExtensionPath, "gpm_cmd.json");
                string pathFileCookie = Path.Combine(cookieExtensionPath, "gpm_restore_cookie.json");
                dynamic gpmCommand = new ExpandoObject();
                gpmCommand.gpm_profile_id = this.Id;
                gpmCommand.command = command;
                gpmCommand.url_server = $"http://127.0.0.1:{GPMSimpleHttpServer.PORT}";
                gpmCommand.file_cookie_save = pathFileCookie;
                File.WriteAllText(gpmCommandFile, JsonConvert.SerializeObject(gpmCommand));
            }
            catch { }
        }
    }

    public enum WebRTCMode
    {
        Disable,
        Fake,
        Real
    }

    public class WebRTC
    {
        public WebRTCMode Mode { get; set; } = WebRTCMode.Real;
        public string PublicIP { get; set; } = "";
    }
}
