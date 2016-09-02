using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FirstCore
{
    public class Program
    {
        public static void Main(string[] args)
        {
            ListenLoop("fe80::c0ba:5a5a:55eb:5e99", 18888, "127.0.0.1", 18889);
        }
        static void ListenLoop(string IPListen, int PortListen, string ForwardIP, int ForwardPort)
        {
            EndPoint Listen = new IPEndPoint(IPAddress.Parse(IPListen), PortListen);
            Socket listenSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            listenSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            listenSocket.Bind(Listen);
            listenSocket.Listen(10);

            Console.Out.WriteLine("listening on {0}", Listen);

            while (true)
            {
                Socket v6socket = listenSocket.Accept();
                Console.Out.WriteLine("incoming connection from {0}", v6socket.RemoteEndPoint);
                Task pipe = PipeConnections(v6socket, ForwardIP, ForwardPort);
            }
        }
        private static async Task PipeConnections(Socket sourceSocket, string forwardIP, int forwardPort)
        {
            using(sourceSocket)
            using (Socket targetSocket = new Socket(SocketType.Stream, ProtocolType.Tcp))
            {
                //targetSocket.Bind(v4Receiver);
                EndPoint v4Receiver = new IPEndPoint(IPAddress.Parse(forwardIP), forwardPort);
                await targetSocket.ConnectAsync(v4Receiver);

                int received;
                ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[4096]);
                while ((received = await sourceSocket.ReceiveAsync(buffer, SocketFlags.None)) != 0)
                {
                    Console.WriteLine("received {0} bytes", received);
                    int sent = await targetSocket.SendAsync(buffer, SocketFlags.None);
                }
                Console.WriteLine("END pipe from {0}<-->{1}", sourceSocket.RemoteEndPoint, targetSocket.LocalEndPoint);
            }
        }
    }
}
