using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Web;

namespace GetSciEi
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            const string searchWord =
                "Time effect of pile-soil-geogrid-cushion interaction of rigid pile composite foundations under high-speed railway embankments";
            //发送请求拿到searchid
            var cookie = GetCookies();
            var searchId = EiGetSearchId(GetSearchUrl(searchWord), cookie);
            //组建post的提交数据
            var formdata = GetEiCiteCountFormData();
            GetEiCiteCount(searchId, cookie, formdata);

            Console.Read();
        }

        /// <summary>
        /// 发送请求得到cookies
        /// </summary>
        /// <returns>返回cookies字符串</returns>
        public static string GetCookies()
        {
            CookieContainer cookieContainer = new CookieContainer();
            const string url = "https://www.engineeringvillage.com/search/quick.url?usageZone=evlogo&usageOrigin=header";
            HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            myHttpWebRequest.Timeout = 20 * 1000; //连接超时
            myHttpWebRequest.Accept = "*/*";
            myHttpWebRequest.UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/71.0.3578.98 Safari/537.36";
            myHttpWebRequest.CookieContainer = new CookieContainer(); //暂存到新实例
            myHttpWebRequest.GetResponse().Close();
            string cookiesstr = myHttpWebRequest.CookieContainer.GetCookieHeader(myHttpWebRequest.RequestUri); //把cookies转换成字符串
            return cookiesstr;
        }

        /// <summary>
        /// 输入检索的网址，返回的是searchID字符串，以及将json写入了文件
        /// </summary>
        /// <param name="searchUrl">请求网址</param>
        public static string EiGetSearchId(string searchUrl, string cookie)
        {
            HttpWebRequest request = WebRequest.CreateHttp(searchUrl);
            request.Method = "GET";
            request.Accept = "application/json, text/javascript, */*; q=0.01";
            request.Headers.Add("Accept-Encoding", "gzip, deflate, br");
            request.Headers.Add("Accept-Language", "zh-CN,zh;q=0.9");
            request.KeepAlive = true;
            request.ContentType = "application/json";
            request.Headers.Add("X-Requested-With", "XMLHttpRequest");
            request.Headers.Add("X-NewRelic-ID", "VQQAUldRCRAFUFFQBwgCUQ==");
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/71.0.3578.98 Safari/537.36";
            request.Referer = "https://www.engineeringvillage.com/search/expert.url?usageZone=evlogo&usageOrigin=header";

            //string cookie;
            //using (FileStream cookieStream = new FileStream(@"C:\Users\我是佟丽娅\Desktop\cookies.txt", FileMode.Open))
            //{
            //    using (StreamReader cookieReader = new StreamReader(cookieStream))
            //    {
            //        cookie = cookieReader.ReadToEnd();
            //    }
            //}
            request.Headers.Add("Cookie", cookie);
            HttpWebResponse searchIdResponse = (HttpWebResponse)request.GetResponse();

            string searchId = "";
            using (Stream seachIdStream = new GZipStream(searchIdResponse.GetResponseStream() ?? throw new InvalidOperationException(), CompressionMode.Decompress))
            {
                //控制台输出
                using (StreamReader sr = new StreamReader(seachIdStream ?? throw new InvalidOperationException()))
                {
                    searchId = sr.ReadToEnd();
                }
            }
            System.Text.UTF8Encoding utf8 = new System.Text.UTF8Encoding(false);
            File.WriteAllText(@".\searchId.txt", searchId.Replace(" ", ""), utf8);
            try
            {
                JObject ja = (JObject)JsonConvert.DeserializeObject(searchId.ToString());
                Console.WriteLine($"检索到{ja["searchMetaData"]["resultscount"]}条记录");
                Console.WriteLine($"searchID为{ja["searchMetaData"]["searchId"]}");
                return ja["searchMetaData"]["searchId"].ToString();
            }
            catch (Exception e)//cookies过期的情况下
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("更换cookies");//cookies过期情况下推出程序
                Console.ReadKey();
                Environment.Exit(0);
                return null;
            }
        }

        /// <summary>
        /// 返回post的提交数据
        /// </summary>
        public static string GetEiCiteCountFormData()
        {
            JObject basicJObject = (JObject)JsonConvert.DeserializeObject(File.ReadAllText(@".\searchId.txt"));
            JObject formData = new JObject
            {
                 {"issn", basicJObject["results"][0]["citedby"]["issn"].ToString()},
                {"isbn", basicJObject["results"][0]["citedby"]["isbn"].ToString()},
                {"isbn13", basicJObject["results"][0]["citedby"]["isbn13"].ToString()},
                {"doi", basicJObject["results"][0]["citedby"]["doi"].ToString()},
                {"pii", basicJObject["results"][0]["citedby"]["pii"].ToString()},
                {"vol", basicJObject["results"][0]["citedby"]["firstvolume"].ToString()},
                {"issue", basicJObject["results"][0]["citedby"]["firstpage"].ToString()},
                {"page", basicJObject["results"][0]["citedby"]["firstvolume"].ToString()},
                {"an", basicJObject["results"][0]["citedby"]["an"].ToString()},
                {"security", basicJObject["results"][0]["citedby"]["md5"].ToString()},
                {"sid", basicJObject["searchMetaData"]["oAdobeAnalytics"]["visitor"]["sisId"].ToString()}
            };
            Console.WriteLine($"[{formData.ToString()}]");
            //byte[] bytes = Encoding.UTF8.GetBytes();
            Console.WriteLine();
            return "citedby=" + HttpUtility.UrlEncode($"[{formData.ToString()}]");
        }

        /// <summary>
        /// 发送引用请求
        /// </summary>
        /// <param name="searchId">文章的searchId</param>
        /// <param name="cookie">cookies</param>
        /// <param name="formdata">提交的数据</param>
        public static void GetEiCiteCount(string searchId, string cookie, string formdata)
        {
            //引用的请求地址
            const string url = " https://www.engineeringvillage.com/toolsinscopus/citedbycount.url";
            HttpWebRequest request = WebRequest.CreateHttp(url);
            // ReSharper disable once AssignNullToNotNullAttribute
            request.Proxy = null;

            request.Method = "POST";

            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/71.0.3578.98 Safari/537.36";
            request.Accept = "application/json, text/javascript, */*; q=0.01";
            request.Headers.Add("Accept-Encoding", "gzip, deflate, br");
            request.Headers.Add("Accept-Language", "zh-CN,zh;q=0.9");
            request.KeepAlive = true;
            request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
            //注意cookie是必须的，跳转时不会发生变化
            request.Headers.Add("Cookie", cookie);
            request.Headers.Add("X-Requested-With", "XMLHttpRequest");
            request.Headers.Add("X-NewRelic-ID", "VQQAUldRCRAFUFFQBwgCUQ==");
            request.Referer = $"https://www.engineeringvillage.com/search/expert.url?SEARCHID={searchId}&COUNT=1&usageOrigin=header&usageZone=evlogo";
            byte[] temp = Encoding.UTF8.GetBytes(formdata);
            request.ContentLength = formdata.Length;
            using (Stream requeststStream = request.GetRequestStream())
            {
                requeststStream.Write(temp, 0, formdata.Length);
            }
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream myResponseStream = new GZipStream(response.GetResponseStream() ?? throw new InvalidOperationException(), CompressionMode.Decompress);
            StreamReader myStreamReader = new StreamReader(myResponseStream ?? throw new InvalidOperationException(), Encoding.Default);
            string retString = myStreamReader.ReadToEnd();
            myStreamReader.Close();
            myResponseStream.Close();
            Console.WriteLine(retString);
        }

        /// <summary>
        /// 根据文章标题，返回提交的网址
        /// </summary>
        /// <param name="searchWord">文章标题</param>
        /// <returns></returns>
        public static string GetSearchUrl(string searchWord)
        {
            string basicUrl =
                $"https://www.engineeringvillage.com/search/submit.url?usageOrigin=searchform&usageZone=expertsearch&editSearch=&isFullJsonResult=true&angularReq=true&CID=searchSubmit&searchtype=Expert&origin=searchform&category=expertsearch&searchWord1={searchWord.Replace(" ", "%20")}&database=1&yearselect=yearrange&startYear=2018&endYear=2018&updatesNo=1&sort=relevance";

            return basicUrl;
        }
    }
}