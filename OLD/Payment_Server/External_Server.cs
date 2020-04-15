using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;

namespace Payment_Server
{
    class External_Server
    {
        public Tuple<string, External_Client> Get_Client(string s)
        {
            External_Client client = new External_Client(listener.AcceptTcpClient(), dispose);
            return new Tuple<string, External_Client>(client.ID, client);
        }
    }
}
