using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Servidor
{
    internal class TimeServer
    {
        private Socket s;
        int puerto;
        int defaultPort = 31416;
        bool ServerRunning = true;
        public static readonly object lockPuerto = new object();
        public static readonly object lockLog = new object();
        private string rutaPuerto = Environment.GetEnvironmentVariable("PROGRAMDATA") + "\\puerto.txt";
        private string rutaLog = Environment.GetEnvironmentVariable("PROGRAMDATA") + "\\log.txt";
        public void InitServer()
        {
            puerto = LeerPuerto(rutaPuerto);
            IPEndPoint ie = new IPEndPoint(IPAddress.Any, puerto);
            using (s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                try
                {
                    s.Bind(ie);
                    s.Listen(10);
                    EscribirEventLog($"Servidor conectado en: {ie.Address}:{ie.Port}");
                    while (ServerRunning)
                    {
                        Socket client = s.Accept();
                        Thread hilo = new Thread(() => ClientDispatcher(client));
                        hilo.IsBackground = true;
                        hilo.Start();
                    }
                }
                catch (SocketException e) when (e.ErrorCode == (int)SocketError.AddressAlreadyInUse)
                {
                    EscribirEventLog("No hay puertos disponibles");
                    throw;
                }
                catch (SocketException e)
                {
                    EscribirEventLog("Fin del servidor");
                }
            }
        }

        public void ClientDispatcher(Socket sClient)
        {
            using (sClient)
            {
                IPEndPoint ieClient = (IPEndPoint)sClient.RemoteEndPoint;
                EscribirEventLog($"Cliente conectado: {ieClient.Address}:{ieClient.Port}");
                Encoding codificacion = Console.OutputEncoding;
                using (NetworkStream ns = new NetworkStream(sClient))
                using (StreamReader sr = new StreamReader(ns, codificacion))
                using (StreamWriter sw = new StreamWriter(ns, codificacion))
                {
                    sw.AutoFlush = true;
                    string msg = "";
                    sw.WriteLine("Bienvenido al servidor");
                    try
                    {
                        msg = sr.ReadLine();
                        if (msg != null)
                        {
                            string[] partido = msg.Trim().Split();
                            string[] fecha = DateTime.Now.ToString().Split();
                            switch (partido[0])
                            {
                                case "help":
                                    sw.WriteLine("time - Devuelve hora, minutos y segundos");
                                    sw.WriteLine("date - Devuelve día, mes y año");
                                    sw.WriteLine("all - Devuelve tanto la fecha como la hora");
                                    break;
                                case "time":
                                    sw.WriteLine(fecha[1]);
                                    break;
                                case "date":
                                    sw.WriteLine(fecha[0]);
                                    break;
                                case "all":
                                    sw.WriteLine($"{DateTime.Now}");
                                    break;
                                case "close":
                                    sw.WriteLine("Nice try");
                                    break;
                                default:
                                    sw.WriteLine("Comando no valido");
                                    sw.WriteLine("\"help\" para conocer los comandos disponibles");
                                    EscribirEventLog($"Comando no valido: {msg}");
                                    break;
                            }
                            GuardarMensaje(rutaLog, msg, ieClient, false);
                        }
                    }
                    catch (IOException)
                    {
                        msg = null;
                        GuardarMensaje(rutaLog, "Error cliente", ieClient, true);
                    }
                }
            }
        }


        private void StopServer()
        {
            Console.WriteLine("Cerrando servidor");
            ServerRunning = false;
            s.Close();
        }
        private int LeerPuerto(String ruta)
        {
            lock (lockPuerto)
            {
                try
                {
                    using (StreamReader sr = new StreamReader(ruta))
                    {
                        string line = sr.ReadLine();
                        if (line != null && int.TryParse(line.Trim(), out int puerto) && puerto < IPEndPoint.MaxPort && puerto > IPEndPoint.MinPort)
                        {
                            if (puerto < IPEndPoint.MaxPort && puerto > IPEndPoint.MinPort)
                            {
                                return puerto;
                            }
                            return puerto < IPEndPoint.MaxPort && puerto > IPEndPoint.MinPort ? puerto : defaultPort;
                        }
                    }
                }
                catch (Exception e) when (e is IOException || e is FileNotFoundException || e is UnauthorizedAccessException)
                {
                    EscribirEventLog("Error leyendo el archivo de puerto");
                }
                return defaultPort;
            }
        }

        private void GuardarMensaje(string ruta, string msg, IPEndPoint ie, bool error)
        {
            lock (lockLog)
            {
                try
                {
                    using (StreamWriter sw = new StreamWriter(ruta, true))
                    {
                        string inicio = error ? "[ERROR] " : "";
                        sw.WriteLine($"{inicio}[{DateTime.Now}-@{ie.Address}:{ie.Port}] {msg}");
                    }
                }
                catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
                {
                    //Caso especial, no se utiliza la función "EscribirEventLog" por posible bucle infinito en caso de fallar ambas funciones,
                    //se podría poner un try  catch vacio para que ante un fallo no pasase nada
                    EventLog.WriteEntry("ServicioServidor", "Error accediendo el archivo de logs");
                }
            }
        }

        private void EscribirEventLog(string msg)
        {
            try
            {
                EventLog.WriteEntry("ServicioServidor", msg);
            }
            catch (Exception)
            {
                GuardarMensaje(rutaLog, "Error guardando en el visor de eventos", (IPEndPoint)s.RemoteEndPoint, true);
            }

        }

    }
}
