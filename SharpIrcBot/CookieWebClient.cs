using System;
using System.Net;

namespace SharpIrcBot
{
    public class CookieWebClient : WebClient
    {
        public CookieContainer CookieJar { get; set; }
        public TimeSpan Timeout { get; set; }

        public CookieWebClient()
        {
            CookieJar = new CookieContainer();
            Timeout = TimeSpan.FromMilliseconds(100.0);
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

            req.Timeout = (Timeout == TimeSpan.Zero)
                ? System.Threading.Timeout.Infinite
                : (int)(Timeout.TotalMilliseconds);

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
