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
        Queue Work = Queue.Synchronized(new Queue());
        Action<string> dispose; bool has_disposed = false;
        JObject ping = new JObject();
        int ping_interval = Int32.Parse(Properties.Resources.ping_interval) , work_interval = Int32.Parse(Properties.Resources.work_interval);

        public External_Client(TcpClient client, Action<string> dispose)
        {
            client.NoDelay = true; this.client = client.GetStream(); this.dispose = dispose; ping["operation"] = "ping";
            Run_Work(true);
            Task.Run(() => { for (; !has_disposed; Thread.Sleep(work_interval)) Run_Work(false); });
            Task.Run(() => { for (; !has_disposed; Thread.Sleep(ping_interval)) Run(ping, (string s) => { }); });
        }

        void Dispose() { if (!has_disposed) { dispose(ID); has_disposed = true; } }

        void Run_Work(bool config)
        {
            try
            {
                while (config || Work.Count > 0)
                {
                    Tuple<object ,object> item = new Tuple<object, object>(null ,null);
                    if (!config)
                    {
                        item = Work.Dequeue() as Tuple<object, object>;
                        byte[] buffer = new byte[Int32.Parse(Properties.Resources.payload_len)] ,temp = Encoding.UTF8.GetBytes(item.Item1 as string);
                        for (int i = 0; i < temp.Length; i++) buffer[i] = temp[i];
                        client.Write(buffer, 0, buffer.Length);
                    }
                    byte[] receive = new byte[Int32.Parse(Properties.Resources.external_response_len)];
                    client.Read(receive, 0, receive.Length);
                    JObject response = (JObject)JsonConvert.DeserializeObject(Encoding.UTF8.GetString(receive));
                    JObject payload = (JObject)response["payload"];
                    if (config) { Config = payload; ID = Config["org_id"].ToObject<string>(); config = false; }
                    else (item.Item2 as Action<string>)(JsonConvert.SerializeObject(payload));
                }
            }
            catch (Exception e)
            { 
                Console.WriteLine(e.Message + "\n" + e.StackTrace); 
                Dispose(); 
            }
        }

        public void Run(JObject payload, Action<string> callback) { Work.Enqueue(new Tuple<object, object>(JsonConvert.SerializeObject(payload), callback)); }
    }
}
