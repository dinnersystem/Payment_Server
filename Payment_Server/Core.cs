using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Threading.Tasks;


namespace Payment_Server
{
    class Core
    {
        public Hashtable ext_client_set = Hashtable.Synchronized(new Hashtable());
        DS_Server ds_server = new DS_Server();
        External_Server ext_server = new External_Server();
        public void Run()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    var client = ext_server.Get_Client((string id) => { ext_client_set.Remove(id); });
                    if (ext_client_set.ContainsKey(client.Item1)) ext_client_set.Remove(client.Item1);
                    ext_client_set.Add(client.Item1, client.Item2);
                }
            });

            Task.Run(() =>
            {
                while (true)
                {
                    DS_Client ds_client = ds_server.Get_Client();
                    string id = ds_client.Get_External_Id();
                    if (!ext_client_set.Contains(id)) throw new Exception("Unavailable external id on" + id);
                    External_Client ext_client = (External_Client)ext_client_set[id];
                    ext_client.Run(ds_client.Payload ,(string status) => { ds_client.Response(status); });
                }
            });
        }
    }
}
