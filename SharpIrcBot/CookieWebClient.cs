using System;
using System.Net;

namespace SharpIrcBot
{
    public class CookieWebClient : WebClient
    {
        public CookieContainer CookieJar;
        public int Timeout;

        public CookieWebClient()
        {
            CookieJar = new CookieContainer();
            Timeout = 100;
        }

        public void ClearCookieJar()
        {
            CookieJar = new CookieContainer();
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            var req = base.GetWebRequest(address);
            if (req == null)
            {
                return null;
            }

            req.Timeout = Timeout;
            var httpReq = req as HttpWebRequest;
            if (httpReq != null)
            {
                httpReq.CookieContainer = CookieJar;
                httpReq.KeepAlive = false;
            }
            return req;
        }
    }
}
