using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Payment_Server
{
    class External_Client
    {
        public JObject Config;

        NetworkStream client;
        Hashtable Response = Hashtable.Synchronized(new Hashtable());
        Queue Request = Queue.Synchronized(new Queue());
        public External_Client(TcpClient client)
        {
            this.client = client.GetStream();
            Run_Response();
            Task.Run(() => { while (true) Run_Request(); });
            Task.Run(() => { while (true) Run_Response(); });
        }

        void Run_Request()
        {
            if (Request.Count > 0)
            {
                string s = Request.Dequeue() as string;
                byte[] temp = new byte[Int32.Parse(Properties.Resources.payload_len)];
                for (int i = 0; i < s.Length; i++) temp[i] = (byte)s[i];
                client.Write(temp, 0, temp.Length);
            }
        }

        void Run_Response()
        {
            byte[] temp = new byte[Int32.Parse(Properties.Resources.external_response_len)];
            client.Read(temp, 0, temp.Length);
            JObject response = (JObject)JsonConvert.DeserializeObject(Encoding.ASCII.GetString(temp));
            JObject payload = response["payload"].ToObject<JObject>();
            if (response["type"].ToObject<string>() == "config") Config = payload;
            else
            {
                string id = payload["org_id"].ToObject<string>();
                if (!Response.ContainsKey(id)) throw new Exception("Received invalid id '" + id + "' from external client.");
                (Response[id] as Action<string>)(payload["msg"].ToObject<string>());
                Response.Remove(id);
            }
        }

        public void Run(JObject payload, Action<string> callback)
        {
            Request.Enqueue(JsonConvert.SerializeObject(payload));
            Response.Add(payload["org_id"].ToObject<string>(), callback);
        }
    }
}
