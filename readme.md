# API GPM BROWSER
## Download driver and browser
Use:
<br /> - [Download Browser and Driver](https://drive.google.com/drive/folders/1GTGsYsWPrDi0cAMXLo_esTgGZ-5jpc50?usp=sharing)
<br /> - [Video guide remote single thread](https://youtu.be/l4Cj9hKma5Q)
<br /> - [Video guide remote multi thread](https://youtu.be/9_3eyWuAXz0)
<br /> - [Video guide export cookie](https://youtu.be/7zZjsfuZ7tQ)
<br />Contacts: [ngochoaitn@gmail.com](mailto:ngochoaitn@gmail.com) or [Facebook](https://facebook.com/ngochoaitn)

## Sample
```bash
// Step 1: Init fake hardware info
Random _rand = new Random();
ProfileInfo profileInfo = new ProfileInfo();

profileInfo.ProfilePath = @"D:\Codes\chromium-test-file\profiles\test-profile";
profileInfo.RemotePort = 9001;

profileInfo.Name = "Test profile";
profileInfo.ConfigName = "test";

profileInfo.BrowseVersion = "96.0.4664.45";
profileInfo.UserAgent = $"Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{profileInfo.BrowseVersion} Safari/537.36";
profileInfo.Timezone = "Asia/Bangkok";

profileInfo.ScreenWidth = _rand.Next(720, 1080);
profileInfo.ScreenHeight = _rand.Next(720, 1080);
profileInfo.AvailScreenWidth = _rand.Next(720, 1080);
profileInfo.AvailScreenHeight = _rand.Next(720, 1080);

profileInfo.ProcessorCount = 12;

profileInfo.WebGLNoise = _rand.NextDouble();
profileInfo.WebGLRectNoise = _rand.NextDouble();
profileInfo.WebGLVendor = "Custom vendor";
profileInfo.WebGLRender = "Custom render";
profileInfo.WebGLUniform2fNoise = _rand.NextDouble() * (0.001 - 0) + 0;
profileInfo.AudioNoise = _rand.NextDouble();
profileInfo.MaxVertexUniform = _rand.Next(3000, 4500);
profileInfo.MaxFragmentUniform = _rand.Next(900, 1500);

profileInfo.CustomFlags.Add("--enabled");

profileInfo.WebRTC.Mode = WebRTCMode.Real;
// profileInfo.Proxy = "socks5://1.2.3.4:567"; //"103.155.217.247:32865"; // 

// Step 2: Set key GPM
profileInfo.GPMKey = "Enter code here";

// Step 3: Init profile folder, path to chrome.exe and port remote chrome
string gpmBrowserPath = @"D:\Codes\chromium\src\out\Release\chrome.exe"; //https://drive.google.com/drive/folders/1GTGsYsWPrDi0cAMXLo_esTgGZ-5jpc50?usp=sharing

/****************Use export cookie plugin (cookie will send to SimpleServer.cs). Guide: https://youtu.be/7zZjsfuZ7tQ ***********************/
//GPMSimpleHttpServer simpleHttpServer = new GPMSimpleHttpServer(6699);
//simpleHttpServer.StartAsync();

//profileInfo.Id = "fixed id";
//string extensionPath = profileInfo.CopyCookieExtension();
//profileInfo.Extensions.Add(extensionPath);
//var onlineStatus = ProfileConection.GetProfileStatus(profileInfo.Id);
/**/

// Step 4: Start browser and remote
ChromeDriver driver = GPMLogin.GPMStarter.StartProfile(gpmBrowserPath,  profileInfo,
	// custom config
	hideConsole: false,
	startUrl: "http://codethuegiare.com/" // Prevent open 2 tab at first startup
	);

driver.Navigate().GoToUrl("http://codethuegiare.com/download");

var input = driver.FindElement(By.XPath("//input[@id='activeKey']"));

input.SendKeys("ABC");
driver.FindElement(By.XPath("//*[@id='btnGetLink']")).Click();

Thread.Sleep(2000);
//driver.Close();
//driver.Quit();
Console.ReadLine();// Keep simple server live
```