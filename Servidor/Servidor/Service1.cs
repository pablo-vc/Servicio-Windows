using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Servidor
{
    public partial class ServicioServidor : ServiceBase
    {
        public ServicioServidor()
        {
            InitializeComponent();
        }
        protected override void OnStart(string[] args)
        {
            try
            {
                Thread hilo = new Thread(() =>
                {
                    try
                    {
                        (new TimeServer()).InitServer();
                    }
                    catch
                    {
                        EventLog.WriteEntry("Error arrancando servidor: ", EventLogEntryType.Error);
                        this.Stop();
                    }
                });
                hilo.IsBackground = true;
                hilo.Start();
            }
            catch
            {
                this.Stop();
            }
        }

        protected override void OnStop()
        {
            EventLog.WriteEntry("ServicioServidor", "Service stopped");
        }
    }
}
