using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace GPMSharedLibrary.Helpers
{
    public enum Opcode
    {
        Fragment = 0,
        Text = 1,
        Binary = 2,
        CloseConnection = 8,
        Ping = 9,
        Pong = 10
    }

    /// <summary>
    /// Trạng thái xuất cookie, lấy bằng cách ProfileConection.GetStatusExportCookie(profileId)
    /// </summary>
    public enum ExportCookieStatus
    {
        New = 1,
        HaveNotCreated = 2,
        ExportingCookie = 3,
        ExportCookieDone = 4
    }

    /// <summary>
    /// Trạng thái của 1 profile, lấy dữ liệu bằng cách ProfileConection.GetProfileStatus(profileId)
    /// </summary>
    public enum ProfileStatus
    {
        Online = 1,
        Offline = 2
    }

    /// <summary>
    /// Thông tin kết nối giữa tool vào extension
    /// </summary>
    public class ProfileConection
    {
        double _timeout = 1.5d; // => 1s extension phải ping qua 1 lần
        public string ProfileId { get; set; }
        public DateTime? LastPing { get; set; } = null;
        public ProfileStatus CurrentStatus
        {
            get
            {
                if (!LastPing.HasValue)
                    return ProfileStatus.Offline;
                else
                {
                    if ((DateTime.Now - LastPing.Value).TotalSeconds <= _timeout)
                        return ProfileStatus.Online;
                    else
                        return ProfileStatus.Offline;
                }
            }
        }
        public ProfileConection(string profileId, DateTime? lastPing)
        {
            ProfileId = profileId;
            LastPing = lastPing;
        }

        static ConcurrentDictionary<string, ExportCookieStatus> _exportCookieStatus = new ConcurrentDictionary<string, ExportCookieStatus>();
        public static void SetExportStatus(string profileId, ExportCookieStatus status)
        {
            if (!_exportCookieStatus.ContainsKey(profileId))
                _exportCookieStatus.TryAdd(profileId, status);
            else
                _exportCookieStatus[profileId] = status;
        }
        public static ExportCookieStatus GetStatusExportCookie(string profileId)
        {
            if (!_exportCookieStatus.ContainsKey(profileId))
                return ExportCookieStatus.HaveNotCreated;
            else
                return _exportCookieStatus[profileId];
        }

        static ConcurrentDictionary<string, ProfileConection> _profileStatus = new ConcurrentDictionary<string, ProfileConection>();

        public static void UpdateLastTimePingProfile(string profileId)
        {
            if (_profileStatus.ContainsKey(profileId))
                _profileStatus[profileId] = new ProfileConection(profileId, DateTime.Now);
            else
                _profileStatus.TryAdd(profileId, new ProfileConection(profileId, DateTime.Now));
        }
        public static ProfileStatus GetProfileStatus(string profileId)
        {
            if (_profileStatus.ContainsKey(profileId))
                return _profileStatus[profileId].CurrentStatus;
            else
                return ProfileStatus.Offline;
        }
    }

    /// <summary>
    /// Khởi chạy 1 http server và nhập dữ liệu từ chrome extension
    /// https://xuanthulab.net/networking-su-dung-lop-httplistener-trong-c-de-tao-may-chu-web-http-don-gian.html
    /// </summary>
    public class GPMSimpleHttpServer
    {
        private HttpListener listener;
        public static int PORT = 9996;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="prefixes">http://localhost:12345</param>
        public GPMSimpleHttpServer(int port)
        {
            PORT = port;
            if (!HttpListener.IsSupported)
                throw new Exception("Not support HttpListener.");
            var prefixes = new List<string>{ $"http://127.0.0.1:{PORT}/"};
            if (prefixes == null || prefixes.Count == 0)
                throw new ArgumentException("prefixes");

            // Khởi tạo HttpListener
            listener = new HttpListener();
            foreach (string prefix in prefixes)
                listener.Prefixes.Add(prefix);

        }
        public async void StartAsync()
        {
            Debug.WriteLine($"Start GPM Simple server port={PORT}");
            // Bắt đầu lắng nghe kết nối HTTP
            listener.Start();
            do
            {
                try
                {
                    // Một client kết nối đến
                    HttpListenerContext context = await listener.GetContextAsync();
                    try
                    {
                        await ProcessRequest(context);
                    }
                    catch(Exception ex)
                    {
                        Debug.WriteLine($"MyHttpServer throw: {ex.Message}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                // Console.WriteLine("...");

            }
            while (listener.IsListening);
        }

        // Xử lý trả về nội dung tùy thuộc vào URL truy cập
        //      /               hiện thị dòng Hello World
        //      /stop           dừng máy chủ
        //      /json           trả về một nội dung json
        //      /requestinfo    thông tin truy vấn
        async Task ProcessRequest(HttpListenerContext context)
        {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;
            Console.WriteLine($"{request.HttpMethod} {request.RawUrl} {request.Url.AbsolutePath}");
            string jsonBody = "";
            // Lấy stream / gửi dữ liệu về cho client
            var outputstream = response.OutputStream;
            switch (request.Url.AbsolutePath)
            {
                case "/requestinfo":
                    {
                        // Gửi thông tin về cho Client
                        context.Response.Headers.Add("content-type", "text/html");
                        context.Response.StatusCode = (int)HttpStatusCode.OK;

                        string responseString = this.GenerateHTML(request);
                        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                        response.ContentLength64 = buffer.Length;
                        await outputstream.WriteAsync(buffer, 0, buffer.Length);
                    }
                    break;
                case "/":
                    {
                        byte[] buffer = System.Text.Encoding.UTF8.GetBytes("Hello world!");
                        response.ContentLength64 = buffer.Length;
                        await outputstream.WriteAsync(buffer, 0, buffer.Length);
                    }
                    break;

                //case "/stop":
                //    {
                //        listener.Stop();
                //        Console.WriteLine("stop http");
                //    }
                //    break;

                case "/json":
                    {
                        response.Headers.Add("Content-Type", "application/json");
                        var product = new
                        {
                            Name = "Macbook Pro",
                            Price = 2000,
                            Manufacturer = "Apple"
                        };
                        string jsonstring = JsonConvert.SerializeObject(product);
                        byte[] buffer = Encoding.UTF8.GetBytes(jsonstring);
                        response.ContentLength64 = buffer.Length;
                        await outputstream.WriteAsync(buffer, 0, buffer.Length);

                    }
                    break;
                case "/cookies":
                case "/cookies/":
                    {
                        jsonBody = ReadStringBodyInRequest(request);
                        dynamic messageFromExtension = TryConvertToObjecct(jsonBody);//danger
                        if (messageFromExtension != null)
                        {
                            byte[] buffer = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new { message = $"Thank! I have recieved your cookies" }));
                            await outputstream.WriteAsync(buffer, 0, buffer.Length);

                            dynamic gpm_restore_cookie = new ExpandoObject();
                            gpm_restore_cookie.id = Guid.NewGuid().ToString();// Id duy nhất để không khôi phục nhiều lần
                            gpm_restore_cookie.cookies = messageFromExtension.data;
                            string jsonWriteToFile = JsonConvert.SerializeObject(gpm_restore_cookie);

                            // Ghi JSON ra file gpm_restore_cookie.json lưu vào thư mục extension
                            string gpmProfileId = Convert.ToString(messageFromExtension.gpm_profile_id);
                            string fileCookiePath = Convert.ToString(messageFromExtension?.file_cookie_save ?? "");
                            //new DataContext().Profiles.FirstOrDefault(p => p.Id == gpmProfileId)?.ExportGpmCommandToCookieExtension("");// Hủy lệnh
                            if (!string.IsNullOrEmpty(fileCookiePath))
                                File.WriteAllText(fileCookiePath, jsonWriteToFile); // Ví dụ một đường dẫn
                            Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} Exported cookie => {fileCookiePath}");
                            ProfileConection.SetExportStatus(gpmProfileId, ExportCookieStatus.ExportCookieDone);
                        }
                        else
                        {
                            byte[] buffer = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new { message = $"Can not parse cookie" }));
                            await outputstream.WriteAsync(buffer, 0, buffer.Length);
                            Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} can not parse data");
                        }
                    }
                    break;
                case "/hello":
                case "/hello/":
                    {
                        jsonBody = ReadStringBodyInRequest(request);
                        dynamic temp = TryConvertToObjecct(jsonBody);
                        if (temp != null)
                        {
                            byte[] buffer = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new { message = $"Hi {temp?.gpm_profile_id}" }));
                            await outputstream.WriteAsync(buffer, 0, buffer.Length);
                            Console.WriteLine($"Client hello: {temp?.gpm_profile_id}");
                            ProfileConection.UpdateLastTimePingProfile(Convert.ToString(temp?.gpm_profile_id));
                        }
                        else
                        {
                            byte[] buffer = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new { message = $"Hi unknow" }));
                            await outputstream.WriteAsync(buffer, 0, buffer.Length);
                            Console.WriteLine($"Unknow client message");
                        }
                    }
                    break;
                case "/ping":
                case "/ping/":
                    {
                        jsonBody = ReadStringBodyInRequest(request);
                        dynamic temp = TryConvertToObjecct(jsonBody);
                        if (temp != null)
                        {
                            byte[] buffer = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new { message = $"Hi {temp?.gpm_profile_id}" }));
                            await outputstream.WriteAsync(buffer, 0, buffer.Length);
                            Console.WriteLine($"Client ping: {temp?.gpm_profile_id}");
                            ProfileConection.UpdateLastTimePingProfile(Convert.ToString(temp?.gpm_profile_id));
                        }
                        else
                        {
                            byte[] buffer = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new { message = $"Hi unknow" }));
                            await outputstream.WriteAsync(buffer, 0, buffer.Length);
                            Console.WriteLine($"Unknow client message");
                        }
                    }
                    break;
                default:
                    {
                        response.StatusCode = (int)HttpStatusCode.NotFound;
                        byte[] buffer = System.Text.Encoding.UTF8.GetBytes("NOT FOUND!");
                        response.ContentLength64 = buffer.Length;
                        await outputstream.WriteAsync(buffer, 0, buffer.Length);
                    }
                    break;
            }

            // switch (request.Url.AbsolutePath)


            // Đóng stream để hoàn thành gửi về client
            outputstream.Close();
        }

        dynamic TryConvertToObjecct(string json)
        {
            try
            {
                return JsonConvert.DeserializeObject<dynamic>(json);
            }
            catch
            {
                return null;
            }
        }

        // Tạo nội dung HTML trả về cho Client (HTML chứa thông tin về Request)
        public string GenerateHTML(HttpListenerRequest request)
        {
            string format = @"<!DOCTYPE html>
                            <html lang=""en""> 
                                <head>
                                    <meta charset=""UTF-8"">
                                    {0}
                                 </head> 
                                <body>
                                    {1}
                                </body> 
                            </html>";
            string head = "<title>Test WebServer</title>";
            var body = new StringBuilder();
            body.Append("<h1>Request Info</h1>");
            body.Append("<h2>Request Header:</h2>");

            // Header infomation
            var headers = from key in request.Headers.AllKeys
                          select $"<div>{key} : {string.Join(",", request.Headers.GetValues(key))}</div>";
            body.Append(string.Join("", headers));

            //Extract request properties
            body.Append("<h2>Request properties:</h2>");
            var properties = request.GetType().GetProperties();
            foreach (var property in properties)
            {
                var name_pro = property.Name;
                string value_pro;
                try
                {
                    value_pro = property.GetValue(request)?.ToString();
                }
                catch (Exception e)
                {
                    value_pro = e.Message;
                }
                body.Append($"<div>{name_pro} : {value_pro}</div>");

            };
            string html = string.Format(format, head, body.ToString());
            return html;
        }

        string ReadStringBodyInRequest(HttpListenerRequest request)
        {
            try
            {
                using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                {
                    return reader.ReadToEnd();
                }
            }
            catch
            {
                return "";
            }
        }
    }
}
