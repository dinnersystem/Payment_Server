using System;
using Terminal.Gui;
using System.Collections;
using System.Threading.Tasks;
using System.Threading;

namespace Payment_Server
{
    class Program
    {
        static void Main(string[] args)
        {
            Core core = new Core();
            core.Run();

            Application.Init();
            var top = Application.Top;
            var win = new Window("Payment Server") { X = 1, Y = 1, Width = Dim.Fill(2), Height = Dim.Fill(1) };
            top.Add(win);
            var list = new ListView() { X = 3, Y = 4 , Width = 50, Height = Dim.Fill(1) };
            var err = new ListView() { X = 50, Y = 4, Width = 50, Height = Dim.Fill(1) };
            var clock = new ListView() { X = 25, Y = 1, Width = 50, Height = 1 };
            win.Add(list); win.Add(err); win.Add(clock);

            Task.Run(() =>
            {
                while(true)
                {
                    Thread.Sleep(1000);
                    ArrayList clients = new ArrayList();
                    lock (core.ext_client_set) foreach (var pair in core.ext_client_set) clients.Add("[ONLINE] External Client: " + pair.Key);
                    list.SetSource(clients); err.SetSource(Core.errors);
                    clock.SetSource(new ArrayList() { DateTime.Now.ToString("yyyy-MM-dd") + " " + DateTime.Now.ToString("HH:mm:ss") });
                    Application.Refresh();
                }
            });
            Application.Run();
        }
    }
}
