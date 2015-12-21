using System;
using System.Collections.Generic;
using System.Text;
using drizzle;
using System.Reflection;
using System.Threading;
namespace Eponine_master
{
    class Program
    {
        static void Main(string[] args)
        {
            drizzleTCP.Server server = new drizzleTCP.Server(34718);
            server.ServerEventArriveByte += server_ServerEventArriveByte;
            for (; ; )
            {
                Console.Write(">");
                string str = Console.ReadLine();
                var tmpstr = str.Split(' ');
                if (tmpstr.Length > 1)
                {
                    if (tmpstr[0].Trim().Equals("do"))
                    {
                        string ExeName = (string)tmpstr[1];
                        SimpleGenerator simplegenerator = new SimpleGenerator(ExeName + ".dll");
                        int index = 0;
                        int max = server.ListTcp.Count;
                        foreach (var recv in server.ListTcp)
                        {
                            recv.send(simplegenerator.Generate(index++, max));
                        }
                        for (; ; )
                        {
                            int iter = 0;
                            foreach (var recv in server.ListTcp)
                            {
                                if (!recv.isOK)
                                {
                                    iter = 1;
                                    break;
                                }
                                //recv.send(simplegenerator.Generate(index++, max));
                            }
                            if (iter == 0)
                            {
                                break;
                            }
                            Thread.Sleep(10);
                        }
                        Assembly ass = Assembly.LoadFile(Environment.CurrentDirectory + "/" + ExeName + ".dll");
                        Type tp = ass.GetType(ExeName + "." + ExeName);
                        Object obj = Activator.CreateInstance(tp);
                        MethodInfo meth = tp.GetMethod("ReduceLoop");  
                        int t = Convert.ToInt32(meth.Invoke(obj, new Object[] { 0, max }));
                    }
                }
            }
        }

        static void server_ServerEventArriveByte(drizzleTCP.Server.Tcpreceiver tcp, byte[] Data,int len)
        {
            Eponine_slave.SimpleParser sp = new Eponine_slave.SimpleParser(Data);
            if (sp.Parse())
            {
                if (sp.header == Eponine_slave.SimpleParser.ParserMeans.returnrequest)
                {
                    tcp.isOK = true;
                }
            }
            //throw new NotImplementedException();
        }

    }
}
