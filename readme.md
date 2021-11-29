# API GPM BROWSER
## Download driver and browser
[Download Browser](https://drive.google.com/drive/folders/1GTGsYsWPrDi0cAMXLo_esTgGZ-5jpc50?usp=sharing) and view [Video guide](https://youtu.be/l4Cj9hKma5Q)

## Sample
```bash
// Step 1: Init fake hardware info
Random _rand = new Random();
ProfileInfo profileInfo = new ProfileInfo();

profileInfo.Name = "Ronin-Facebook";

profileInfo.BrowseVersion = "93.0.4577.100";
profileInfo.UserAgent = $"Mozilla/5.0 ((Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{profileInfo.BrowseVersion} Safari/537.36";
profileInfo.Timezone = "Asia/Bangkok";

profileInfo.ScreenWidth = _rand.Next(720, 1080);
profileInfo.ScreenHeight = _rand.Next(720, 1080);
profileInfo.AvailScreenWidth = _rand.Next(720, 1080);
profileInfo.AvailScreenHeight = _rand.Next(720, 1080);
            
profileInfo.ProcessorCount = 12;

profileInfo.WebGLNoise = _rand.NextDouble();
profileInfo.WebGLRectNoise = _rand.NextDouble();
profileInfo.WebGLUniform2fNoise = _rand.NextDouble() * (0.001 - 0) + 0;
profileInfo.AudioNoise = _rand.NextDouble();
profileInfo.MaxVertexUniform = _rand.Next(3000, 4500);
profileInfo.MaxFragmentUniform = _rand.Next(900, 1500);

profileInfo.WebRTC.Mode = WebRTCMode.Disable;

// Step 2: Set key GPM
profileInfo.GPMKey = "Enter key here";

// Step 3: Init profile folder, path to chrome.exe and port remote chrome
string gpmBrowserPath = @"D:\Codes\chromium\src\out\Minimum\chrome.exe";
string profilePath = @"D:\Codes\chromium-test-file\profiles\test-ronin";
int portRemote = 9951;

// Step 4: Start browser and remote
ChromeDriver driver = GPMLogin.GPMStarter.StartProfile(gpmBrowserPath, profilePath, portRemote, profileInfo, "--enabled", hideConsole: false);

driver.Navigate().GoToUrl("http://codethuegiare.com/download");

var input = driver.FindElement(By.XPath("//input[@id='activeKey']"));

input.SendKeys("ABC");
driver.FindElement(By.XPath("//*[@id='btnGetLink']")).Click();
```