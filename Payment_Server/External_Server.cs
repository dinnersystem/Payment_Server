using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;

namespace Payment_Server
{
    class External_Server
    {
        TcpListener listener = new TcpListener(IPAddress.Any ,Int32.Parse(Properties.Resources.external_port));

        public External_Server() { listener.Start(); }
        public Tuple<string, External_Client> Get_Client()
        {
            External_Client client = new External_Client(listener.AcceptTcpClient());
            return new Tuple<string, External_Client>(client.Config["org_id"].ToObject<string>(), client);
        }
    }
}
