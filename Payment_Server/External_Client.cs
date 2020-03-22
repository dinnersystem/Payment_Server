using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json;

namespace Payment_Server
{
    class External_Client
    {
        public JObject Config; public string ID;

        NetworkStream client;
        Hashtable Response = Hashtable.Synchronized(new Hashtable());
        Queue Request = Queue.Synchronized(new Queue());
        int work_id = 0; object lock_obj = new object();
        Action<string> dispose; bool should_dispose = false;
        JObject ping = new JObject();
        int ping_interval = Int32.Parse(Properties.Resources.ping_interval), work_interval = Int32.Parse(Properties.Resources.work_interval);

        public External_Client(TcpClient client ,Action<string> dispose)
        {
            client.NoDelay = true; this.client = client.GetStream(); 
            ping["operation"] = "ping"; this.dispose = dispose; 
            Run_Response();
            Task.Run(() =>
            {
                Task.WaitAll(new List<Task>()
                {
                    Task.Run(() => { for (; !should_dispose;Thread.Sleep(work_interval)) Run_Request(); }),
                    Task.Run(() => { for (; !should_dispose;Thread.Sleep(work_interval)) Run_Response(); }),
                    Task.Run(() => { for(; !should_dispose;Thread.Sleep(ping_interval)) Run(ping ,(string s) => { }); })
                }.ToArray());
                dispose(ID);
            });
        }

        void Run_Request()
        {
            try
            {
                if (Request.Count > 0)
                {
                    byte[] buffer = new byte[Int32.Parse(Properties.Resources.payload_len)];
                    byte[] temp = Encoding.UTF8.GetBytes(Request.Dequeue() as string);
                    for (int i = 0; i < temp.Length; i++) buffer[i] = temp[i];
                    client.Write(buffer, 0, buffer.Length);
                }
            }
            catch (Exception e) { should_dispose = true; }
        }

        void Run_Response()
        {
            try
            {
                byte[] temp = new byte[Int32.Parse(Properties.Resources.external_response_len)];
                client.Read(temp, 0, temp.Length);
                string receive = "";
                while(receive != "") Encoding.UTF8.GetString(temp).Replace("\0" ,"");
                JObject response = (JObject)JsonConvert.DeserializeObject(receive);
                JObject payload = (JObject)response["payload"];
                if (response["type"].ToObject<string>() == "config") { Config = payload; ID = Config["org_id"].ToObject<string>(); }
                else
                {
                    string id = response["work_id"].ToObject<string>();
                    if (!Response.ContainsKey(id)) throw new Exception("Received invalid work id '" + id + "' from external client.");
                    (Response[id] as Action<string>)(JsonConvert.SerializeObject(payload));
                    Response.Remove(id);
                }
            }
            catch (Exception e) { should_dispose = true; }
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