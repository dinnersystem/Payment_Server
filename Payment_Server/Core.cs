using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Globalization;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace Payment_Server
{
    class Core
    {
        public ConcurrentDictionary<string, External_Client> ext_client_set = new ConcurrentDictionary<string, External_Client>();
        public List<string> errors = new List<string>();

        DS_Server ds_server = new DS_Server();
        External_Server ext_server = new External_Server();
        public void Run()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    var client = ext_server.Get_Client((string id) => { ext_client_set.TryRemove(id, out External_Client ext_client); });
                    if (ext_client_set.ContainsKey(client.Item1)) ext_client_set.TryRemove(client.Item1, out External_Client ext_client);
                    ext_client_set.TryAdd(client.Item1, client.Item2);
                }
            });

            Task.Run(() =>
            {
                while (true)
                {
                    DS_Client ds_client = ds_server.Get_Client();
                    string id = ds_client.Get_External_Id();
                    if (!ext_client_set.ContainsKey(id))
                    {
                        errors.Add("[ERROR] Unavailable external id on " + id + ".");
                        errors.Add("[ERROR] Timestamp " + DateTime.Now.ToString("yyyy-MM-dd") + " " + DateTime.Now.ToString("HH:mm:ss") + ".");
                        errors.Add(JsonConvert.SerializeObject(ds_client.Payload).Substring(0 ,50));
                    }
                    else
                    {
                        External_Client ext_client = (External_Client)ext_client_set[id];
                        ext_client.Run(ds_client.Payload, (string status) => { ds_client.Response(status); });
                    }
                }
            });
        }
    }
}
