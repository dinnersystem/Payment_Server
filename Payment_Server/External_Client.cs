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
        int work_id = 0;
        object lock_obj = new object();

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
                byte[] buffer = new byte[Int32.Parse(Properties.Resources.payload_len)];
                byte[] temp = Encoding.UTF8.GetBytes(Request.Dequeue() as string);
                for (int i = 0; i < temp.Length; i++) buffer[i] = temp[i];
                client.Write(buffer, 0, buffer.Length);
            }
        }

        void Run_Response()
        {
            byte[] temp = new byte[Int32.Parse(Properties.Resources.external_response_len)];
            client.Read(temp, 0, temp.Length);
            JObject response = (JObject)JsonConvert.DeserializeObject(Encoding.ASCII.GetString(temp));
            JObject payload = (JObject)response["payload"];
            if (response["type"].ToObject<string>() == "config") Config = payload;
            else
            {
                string id = response["work_id"].ToObject<string>();
                if (!Response.ContainsKey(id)) throw new Exception("Received invalid work id '" + id + "' from external client.");
                (Response[id] as Action<string>)(JsonConvert.SerializeObject(payload));
                Response.Remove(id);
            }
        }

        public void Run(JObject payload, Action<string> callback)
        {
            lock (lock_obj)
            {
                payload["work_id"] = work_id.ToString();
                Request.Enqueue(JsonConvert.SerializeObject(payload));
                Response.Add(payload["work_id"].ToObject<string>(), callback);
                work_id += 1;
            }
        }
    }
}
