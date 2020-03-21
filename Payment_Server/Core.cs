using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Globalization;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.IO;

namespace Payment_Server
{
    class Core
    {
        public ConcurrentDictionary<string, External_Client> ext_client_set = new ConcurrentDictionary<string, External_Client>();
        public List<string> errors = new List<string>();

        DS_Server ds_server = new DS_Server();
        External_Server ext_server = new External_Server();
        StreamWriter logger = new StreamWriter("payment_log.txt", true);

        public Core() { logger.AutoFlush = true; }

        void Show_Error(string abstract_msg, string msg = null)
        {
            errors.Add("[ERROR] " + abstract_msg);
            errors.Add("[ERROR] Timestamp " + DateTime.Now.ToString("yyyy-MM-dd") + " " + DateTime.Now.ToString("HH:mm:ss") + ".");
            if(msg != null) errors.Add(msg);
        }
        public void Run()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        var client = ext_server.Get_Client((string id) => { ext_client_set.TryRemove(id, out External_Client ext_client); });
                        if (ext_client_set.ContainsKey(client.Item1)) ext_client_set.TryRemove(client.Item1, out External_Client ext_client);
                        ext_client_set.TryAdd(client.Item1, client.Item2);
                    }
                    catch (Exception e) { Show_Error(e.Message); }
                }
            });

            Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        DS_Client ds_client = ds_server.Get_Client();
                        string id = ds_client.Get_External_Id();
                        logger.WriteLine("From DS," + DateTime.Now.ToString("yyyy-MM-dd") + " " + DateTime.Now.ToString("HH:mm:ss") + "," + JsonConvert.SerializeObject(ds_client.Payload));

                        if (!ext_client_set.ContainsKey(id)) Show_Error("Unavailable external id on " + id + ".", JsonConvert.SerializeObject(ds_client.Payload));
                        else
                        {
                            External_Client ext_client = (External_Client)ext_client_set[id];
                            ext_client.Run(ds_client.Payload, (string status) =>
                            {
                                logger.WriteLine("From EXT," + DateTime.Now.ToString("yyyy-MM-dd") + " " + DateTime.Now.ToString("HH:mm:ss") + "," + status);
                                ds_client.Response(status);
                            });
                        }
                    }
                    catch (Exception e) { Show_Error(e.Message); }
                }
            });
        }
    }
}
