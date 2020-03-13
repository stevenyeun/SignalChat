using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ChatServerCS
{
    class MyWebClient:WebClient
    {
        int m_nTimeOut = 0;

        public MyWebClient(int timeOut)
        {
            m_nTimeOut = timeOut * 1000;
        }

        protected override WebRequest GetWebRequest(Uri address)

        {
            WebRequest request = base.GetWebRequest(address);

            request.Timeout = m_nTimeOut;

            return request;
        }

    }
}
