using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace Payment_Server
{
    class DS_Client
    {
        NetworkStream client;
        public readonly JObject Payload;

        public DS_Client(TcpClient client)
        {
            this.client = client.GetStream();
            StringBuilder payload = new StringBuilder();
            do
            {
                byte[] temp = new byte[Int32.Parse(Properties.Resources.payload_len)];
                this.client.Read(temp, 0, temp.Length);
                payload.Append(Encoding.ASCII.GetString(temp));
            } while (this.client.DataAvailable);
            this.Payload = (JObject)JsonConvert.DeserializeObject(payload.ToString());
        }

        public void Response(string s) { client.Write(new ReadOnlySpan<byte>(Encoding.ASCII.GetBytes(s))); client.Close(); }

        public string Get_External_Id() { return Payload["org_id"].ToObject<string>(); }
    }
}
