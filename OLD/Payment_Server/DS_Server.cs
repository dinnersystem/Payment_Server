using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;

namespace Payment_Server
{
    class DS_Server
    {
        TcpListener listener = new TcpListener(IPAddress.Parse("0.0.0.0") ,Int32.Parse(Properties.Resources.ds_port));
        public DS_Server() { listener.Start(); }
        public DS_Client Get_Client() { return new DS_Client (listener.AcceptTcpClient());  }
    }
}
