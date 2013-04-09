using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Web;

namespace eRepublikSitter
{
    public class WebRequestManager
    {
        public static string SendGenericWebRequest(string url, CookieContainer cookieContainer)
        {
            HttpWebRequest request = CreateRequest(url, cookieContainer);
            return SendCustomWebRequest(request);
        }

        public static string SendCustomWebRequest(HttpWebRequest request)
        {
            HttpWebResponse response = GetResponse(request);

            Stream responseStream = responseStream = response.GetResponseStream();
            if (response.ContentEncoding.ToLower().Contains("gzip"))
                responseStream = new GZipStream(responseStream, CompressionMode.Decompress);
            else if (response.ContentEncoding.ToLower().Contains("deflate"))
                responseStream = new DeflateStream(responseStream, CompressionMode.Decompress);

            StreamReader reader = new StreamReader(responseStream, Encoding.Default);

            return reader.ReadToEnd();

        }

        public static HttpWebRequest CreateCustomPostRequest(string url, string postData, CookieContainer cookieContainer)
        {
            HttpWebRequest request = CreateRequest(url, cookieContainer);
            request.Method = "POST";

            // Convert POST data into a byte array.
            byte[] byteArray = Encoding.UTF8.GetBytes(postData);

            // This is required or it looks like a CSRF attack to symfony
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = byteArray.Length;

            using (Stream dataStream = request.GetRequestStream())
            {
                dataStream.Write(byteArray, 0, byteArray.Length);
            }

            return request;
        }

        public static HttpWebRequest CreateRequest(string url, CookieContainer cookieContainer)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.ProtocolVersion = HttpVersion.Version10;
            request.CookieContainer = cookieContainer;
            request.UserAgent = FIREFOX3_USER_AGENT_STRING;
            request.AllowAutoRedirect = true;
            request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            request.Headers.Add(HttpRequestHeader.AcceptLanguage, "en-us,en;q=0.5");
            request.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip,deflate");
            request.Headers.Add(HttpRequestHeader.AcceptCharset, "ISO-8859-1,utf-8;q=0.7,*;q=0.7");
            request.Headers.Add(HttpRequestHeader.KeepAlive, "300");
            
            return request;
        }

        public static HttpWebResponse GetResponse(HttpWebRequest request)
        {
            return (HttpWebResponse)request.GetResponse();
        }

        private const string FIREFOX3_USER_AGENT_STRING = "Mozilla/5.0 (Windows; U; Windows NT 6.1; en-US; rv:1.9.0.10) Gecko/2009042316 Firefox/3.0.10 (.NET CLR 3.5.30729)";
    }
}
