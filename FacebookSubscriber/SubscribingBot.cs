using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace FacebookSubscriber
{    
    public class SubscribingBot
    {
        private readonly string _login;
        private readonly string _password;

        private CookieContainer _cookieContainer;
        private string _token;
        private string _userId;

        public SubscribingBot(string login, string password)
        {
            _login = login;
            _password = password;
            Authorize();
        }

        public void Authorize()
        {
            CookieContainer tmpContainer = new CookieContainer();
            byte[] bytes = new UTF8Encoding().GetBytes($"&email={_login}&pass={_password}");
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://m.facebook.com/login.php");
            request.Method = "POST";
            request.KeepAlive = true;
            request.CookieContainer = tmpContainer;
            request.ContentType = "application/x-www-form-urlencoded";
            request.Referer = "https://m.facebook.com/";
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/66.0.3359.181 Safari/537.36";
            request.ContentLength = bytes.Length;
            request.GetRequestStream().Write(bytes, 0, bytes.Length);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();            
            tmpContainer.Add(response.Cookies);
            _cookieContainer = tmpContainer;

            var html = new StreamReader(response.GetResponseStream()).ReadToEnd();
            try
            {
                _token = ParseToken(html);
                _userId = ParseUserId(html);
            }
            catch
            {
                throw new Exception("Wrong login|password or your phone number isn't confirmed yet");
            }
            
        }

        public void Subscribe(string subjectId)
        {                   
            if (!Regex.IsMatch(subjectId, @"\d+"))
            {
                subjectId = GetRealId(subjectId);
            }
            var watch = System.Diagnostics.Stopwatch.StartNew();

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://m.facebook.com/a/subscriptions/add");
            string postData = $"subject_id={subjectId}&fb_dtsg={_token}&&__user={_userId}&_wap_notice_shown=";
            byte[] postDataBytes = new UTF8Encoding().GetBytes(postData);
            request.Method = "POST";
            request.KeepAlive = true;
            request.CookieContainer = _cookieContainer;
            request.ContentType = "application/x-www-form-urlencoded";
            request.Referer = "https://m.facebook.com/";
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/66.0.3359.181 Safari/537.36";

            request.ContentLength = postDataBytes.Length;
            request.GetRequestStream().Write(postDataBytes, 0, postDataBytes.Length);

            var response = (HttpWebResponse)request.GetResponse();            
            string html = new StreamReader(response.GetResponseStream()).ReadToEnd();
            
            watch.Stop();
            if (watch.ElapsedMilliseconds < 1000)
            {
                throw new Exception("Seems like we are blocked or page doesn't exist or maybe we are already following this group");
            };
        }

        private string ParseToken(string html)
        {
            var regex = new Regex(@"fb_dtsg.+value=""?([^""]+)");
            var preToken = regex.Matches(html)[0].Value.Split('"')[2];
            var token = preToken.Remove(preToken.Length - 1, 1);           

            return token;
        }

        private string ParseUserId(string html)
        {
            var regex = new Regex(@"USER_ID.:.+");
            var userId = regex.Matches(html)[0].Value.Split('"')[2];

            return userId;
        }
               
        private string GetRealId(string pseudoId)
        {
            var boundary = DateTime.Now.Ticks.ToString("x");
            var request = (HttpWebRequest)HttpWebRequest.Create("https://findmyfbid.com/?__amp_source_origin=https%3A%2F%2Ffindmyfbid.com");
            request.Method = WebRequestMethods.Http.Post;
            request.ContentType = $"multipart/form-data; boundary=----WebKitFormBoundary{boundary}";
            request.Host = "findmyfbid.com";
            var payload = $"------WebKitFormBoundary{boundary}\r\nContent-Disposition: form-data; name=\"url\"\r\n\r\n{pseudoId}\r\n------WebKitFormBoundary{boundary}--";
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/66.0.3359.181 Safari/537.36";

            using (var reqStream = request.GetRequestStream())
            {
                var reqWriter = new StreamWriter(reqStream);
                reqWriter.Write(payload);
                reqWriter.Flush();
            }

            var response = request.GetResponse();
            var html = new StreamReader(response.GetResponseStream()).ReadToEnd();
            string realId;
            try
            {
                realId = response.Headers["AMP-Redirect-To"].Split('/')[4];
            }
            catch(IndexOutOfRangeException ex)
            {
                throw new Exception("Page not found");
            }
            return realId;
        }
    }
}
