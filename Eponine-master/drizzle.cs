using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using System.Threading;
//KA 2013年4月1日15:45:10
namespace drizzle
{
    public class drizzleTCP
    {
        /// <summary>
        /// 服务器
        /// </summary>
        public class Server
        {
            private IPAddress ipAddr;
            private TcpListener Listener;
            private Thread ListenThread;
            //private int Uid = 0;
            public List<Tcpreceiver> ListTcp; //= new List<TcpClient>();
            private bool isKeepListen = true;
            public delegate void ServerDelegateArrive(Server.Tcpreceiver tcp, string Receive);
            public event ServerDelegateArrive ServerEventArrive;
            public delegate void ServerDelegateArriveByte(Server.Tcpreceiver tcp, byte[] Data,int len);
            public event ServerDelegateArriveByte ServerEventArriveByte;
            private int connect(string host, int port)
            {
                try
                {
                    //IPHostEntry ipe = Dns.GetHostEntry(Dns.GetHostName());
                    //IPAddress ipa = ipe.AddressList[0];

                    ipAddr = IPAddress.Parse(host);
                    Listener = new TcpListener(ipAddr, port);
                    Listener.Start();
                    ListenThread = new Thread(keeplisten);
                    ListenThread.Start();
                    ListTcp = new List<Tcpreceiver>();
                    Console.WriteLine("Build established:" + port.ToString());
                    //Console.WriteLine(ipa.ToString());
                    return Listener.GetHashCode();
                }
                catch (Exception ee)
                {
                    Console.WriteLine("连接失败，请查看端口是否被占用");
                    Console.WriteLine(ee.Message.ToString());
                    return -1;
                }
            }
            public void DeleteTcpClient(Tcpreceiver recv)
            {
                this.ListTcp.Remove(recv);
            }
            public Server(string host, int port)
            {
                connect(host, port);
            }
            public Server(int port)
            {
                connect("0.0.0.0", port);
            }
            public int disconnect()
            {
                try
                {
                    foreach (Tcpreceiver tcp in ListTcp)
                    {
                        try
                        {
                            tcp.stop();
                        }
                        catch { }
                    }
                    ListenThread.Abort();
                    Listener.Stop();
                    return Listener.GetHashCode();
                }
                catch
                {
                    return -1;
                }
            }
            public void send(byte[] bytes)
            {
                foreach (Tcpreceiver tcp in ListTcp)
                {
                    try
                    {
                        tcp.send(bytes);
                    }
                    catch { }
                }
            }
            public void send(string str)
            {
                foreach (Tcpreceiver tcp in ListTcp)
                {
                    try
                    {
                        tcp.sendmessage(str);
                    }
                    catch { }
                }
            }
            /// <summary>
            /// 监听子程
            /// </summary>
            public void keeplisten()
            {
                TcpClient tcpClient;
                //开启死循环
                while (isKeepListen)
                {
                    tcpClient = Listener.AcceptTcpClient();
                    Tcpreceiver tcprece = new Tcpreceiver(this,tcpClient);
                    tcprece.TcpreceiverEventArrive += new Tcpreceiver.TcpreceiverDelegateArrive(tcprece_TcpreceiverEventArrive);
                    tcprece.TcpreceiverEventArriveByte += new Tcpreceiver.TcpreceiverDelegateArriveByte(tcprece_TcpreceiverEventArriveByte);
                    ListTcp.Add(tcprece);

                }
                foreach (Tcpreceiver tcp in ListTcp)
                {
                    tcp.stop();
                }
            }

            void tcprece_TcpreceiverEventArriveByte(Server.Tcpreceiver Uid, byte[] data,int len)
            {
                if(ServerEventArriveByte!=null)
                    ServerEventArriveByte(Uid, data,len);
                //throw new NotImplementedException();
            }

            private void tcprece_TcpreceiverEventArrive(Server.Tcpreceiver Uid, string Receive)
            {
                if (ServerEventArrive != null)
                    ServerEventArrive(Uid, Receive);
                //throw new NotImplementedException();
            }
            public class Tcpreceiver
            {
                public bool isOK = false;
                private TcpClient tcpClient;
                private NetworkStream ns = null;
                private bool isrunning = true;
                private int heartbreak = 0;
                private int preheartbreak = 0;
                private Server mserver;
                //private int Uid = -1;
                public string IP
                {
                    get
                    {
                        return ((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address.ToString();
                    }
                }
                public delegate void TcpreceiverDelegateArrive(Tcpreceiver Uid, string Receive);
                public delegate void TcpreceiverDelegateArriveByte(Tcpreceiver Uid, byte[] data,int len);
                public event TcpreceiverDelegateArrive TcpreceiverEventArrive;
                public event TcpreceiverDelegateArriveByte TcpreceiverEventArriveByte;
                private System.Threading.Timer timer;
                public Tcpreceiver(Server server,TcpClient tcp)
                {
                    mserver = server;
                    tcpClient = tcp;
                    //receive();
                    ThreadPool.QueueUserWorkItem(receive);
                    timer = new System.Threading.Timer(onHeartBreakCallback, null, 0, 1000);
                }
                private void onHeartBreakCallback(object obj)
                {
                    Eponine_master.SimpleHeartBreakGenerator simpleheartbreak = new Eponine_master.SimpleHeartBreakGenerator(null);
                    if (preheartbreak == 0 && heartbreak == 0)
                    {
                        send(simpleheartbreak.Generate(0, 0));
                        //sendmessage("heart");
                    }
                    else
                    {
                        if (preheartbreak < heartbreak)
                        {
                            preheartbreak = heartbreak;
                            send(simpleheartbreak.Generate(0, 0));
                        }
                        else
                        {
                            //Console.WriteLine("Connection Lost");
                            timer.Change(System.Threading.Timeout.Infinite, 0);
                            stop();
                        }
                    }
                    //timer.Change(0,1000); 
                }
                public void stop()
                {
                    isrunning = false;
                    tcpClient.Close();
                    mserver.DeleteTcpClient(this);
                    //Console.WriteLine("Drop Client Success");
                    //receicethread.Abort();
                }
                private void receive(object obj)
                {
                    string str;
                    while (isrunning)
                    {

                        try
                        {
                            byte[] data = new byte[1024];
                            ns = tcpClient.GetStream();
                            int stringLENGTH = ns.Read(data, 0, 1024);
                            if (stringLENGTH > 0)
                            {
                                heartbreak++;
                                ///////////////////////////////////////////////////////////
                                //do events
                                //////////////////////////////////////////////////////////
                                if (TcpreceiverEventArrive != null)
                                {
                                    str = Encoding.UTF8.GetString(data, 0, stringLENGTH);
                                    TcpreceiverEventArrive(this, str);
                                }
                                if (TcpreceiverEventArriveByte != null)
                                {
                                    TcpreceiverEventArriveByte(this, data,stringLENGTH);
                                }
                                GC.Collect();
                            }
                            Application.DoEvents();
                        }
                        catch (Exception ee)
                        {
                            
                            isrunning = false;
                            tcpClient.Close();
                        }
                    }
                }
                public void send(byte[] bytes)
                {
                    if (ns.CanWrite)
                    {
                        ns.Write(bytes, 0, bytes.Length);
                        ns.Flush();
                    }
                }
                public void sendmessage(string str)
                {
                    byte[] tmpbyte = Encoding.Default.GetBytes(str);
                    send(tmpbyte);
                    Console.WriteLine(((IPEndPoint)tcpClient.Client.RemoteEndPoint).Port.ToString() + ":" + str);
                }
            }
            private void console_write(string ConsoleString)
            {
                Console.WriteLine(ConsoleString);
            }

        }
        /// <summary>
        /// 客户端
        /// </summary>
        public class Client
        {
            TcpClient tcp = null;
            private bool isrunning = false;
            private Thread THrecevie;
            private NetworkStream ns;
            private string Clientip;
            private int Clientport;
            public delegate void ClientDelegateArrive(TcpClient tcp, string Receive);
            public delegate void ClientDelegateArriveByte(TcpClient tcp, byte[] Receive,int length);
            public event ClientDelegateArriveByte ClientEventArriveByte;
            public Client(string IP, int port)
            {
                Clientip = IP;
                Clientport = port;
                connect(IP, port);
            }
            public void connect(string IP, int port)
            {
                try
                {
                    tcp = new TcpClient();
                    //tcp.ReceiveTimeout = 1000;
                    tcp.Connect(IP, port);
                    ns = tcp.GetStream();
                    isrunning = true;
                    THrecevie = new Thread(receive);
                    THrecevie.Start();
                }
                catch(Exception ee)
                {
                    Thread.Sleep(1000);
                    tcp.Close();
                    //重连接
                    Console.WriteLine("reconnect");
                    //connect(Clientip, Clientport);
                }
            }
            public void send(string str)
            {
                byte[] tmpbyte = Encoding.UTF8.GetBytes(str);
                send(tmpbyte);
            }
            public void send(byte[] tmpbyte)
            {
                //byte[] tmpbyte = Encoding.UTF8.GetBytes(str);
                ns.Write(tmpbyte, 0, tmpbyte.Length);
                ns.Flush();
            }
            public void close()
            {
                isrunning = false;
                this.tcp.Close();
            }
            private void receive()
            {
                try
                {
                    string str = string.Empty;
                    byte[] data = new byte[4096];
                    while (isrunning)
                    {
                        int stringLENGTH = ns.Read(data, 0, 4096);
                        ns.Flush();
                        if (stringLENGTH > 0)
                        {
                            if (ClientEventArriveByte != null)
                            {
                                ClientEventArriveByte(tcp, data, stringLENGTH);
                            }
                        }
                        Thread.Sleep(1);

                    }
                }
                catch
                {
                    Thread.Sleep(1000);
                    tcp.Close();
                    //重连接
                    connect(Clientip, Clientport);
                    //tcp.Connect(Clientip, Clientport);
                    isrunning = false;
                }
            }
        }
    }
}
