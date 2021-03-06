﻿/***
 * Member: Soufyan Abdellati
 * Std Number 1: 0963595
 * Class: INF2A
 ***/


using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace SocketServer {
    public class ClientInfo {
        public string studentnr { get; set; }
        public string classname { get; set; }
        public int clientid { get; set; }
        public string teamname { get; set; }
        public string ip { get; set; }
        public string secret { get; set; }
        public string status { get; set; }
    }

    public class Message {
        public const string welcome = "WELCOME";
        public const string stopCommunication = "COMC-STOP";
        public const string statusEnd = "STAT-STOP";
        public const string secret = "SECRET";
    }

    public class SequentialServer {
        public Socket listener;
        public IPEndPoint localEndPoint;
        public IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
        public readonly int portNumber = 11111;

        public String results = "";
        public LinkedList<ClientInfo> clients = new LinkedList<ClientInfo>();

        private Boolean stopCond = false;
        private int processingTime = 1000;
        private int listeningQueueSize = 5;

        public void prepareServer() {
            byte[] bytes = new byte[1024];
            String data = null;
            int numByte = 0;
            string replyMsg = "";
            bool stop;

            try {
                Console.WriteLine("[Server] is ready to start ...");
                // Establish the local endpoint
                localEndPoint = new IPEndPoint(ipAddress, portNumber);
                listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                Console.Out.WriteLine("[Server] A socket is established ...");
                // associate a network address to the Server Socket. All clients must know this address
                listener.Bind(localEndPoint);
                // This is a non-blocking listen with max number of pending requests
                listener.Listen(listeningQueueSize);
                while (true) {
                    Console.WriteLine("[SERVER] Waiting connection ... ");
                    // Suspend while waiting for incoming connection 
                    Socket connection = listener.Accept();
                    this.sendReply(connection, Message.welcome);

                    stop = false;
                    while (!stop) {
                        numByte = connection.Receive(bytes);
                        data = Encoding.ASCII.GetString(bytes, 0, numByte);
                        replyMsg = processMessage(data);
                        if (replyMsg.Equals(Message.stopCommunication)) {
                            stop = true;
                            break;
                        }
                        else
                            this.sendReply(connection, replyMsg);
                    }

                }

            }
            catch (Exception e) {
                Console.Out.WriteLine(e.Message);
            }
        }
        public void handleClient(Socket con) {
        }
        public string processMessage(String msg) {
            Thread.Sleep(processingTime);
            Console.WriteLine("[Server] received from the client -> {0} ", msg);
            string replyMsg = "";

            try {
                switch (msg) {
                    case Message.stopCommunication:
                        replyMsg = Message.stopCommunication;
                        break;
                    default:
                        ClientInfo c = JsonSerializer.Deserialize<ClientInfo>(msg.ToString());
                        clients.AddLast(c);
                        if (c.clientid == -1) {
                            stopCond = true;
                            exportResults();
                        }
                        c.secret = c.studentnr + Message.secret;
                        c.status = Message.statusEnd;
                        replyMsg = JsonSerializer.Serialize<ClientInfo>(c);
                        break;
                }
            }
            catch (Exception e) {
                Console.Out.WriteLine("[Server] processMessage {0}", e.Message);
            }

            return replyMsg;
        }
        public void sendReply(Socket connection, string msg) {
            byte[] encodedMsg = Encoding.ASCII.GetBytes(msg);
            connection.Send(encodedMsg);
        }
        public void exportResults() {
            if (stopCond) {
                this.printClients();
            }
        }
        public void printClients() {
            string delimiter = " , ";
            Console.Out.WriteLine("[Server] This is the list of clients communicated");
            foreach (ClientInfo c in clients) {
                Console.WriteLine(c.classname + delimiter + c.studentnr + delimiter + c.clientid.ToString());
            }
            Console.Out.WriteLine("[Server] Number of handled clients: {0}", clients.Count);

            clients.Clear();
            stopCond = false;

        }
    }


    public class ConcurrentServer {
        public Socket listener;
        public IPEndPoint localEndPoint;
        public IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
        public readonly int portNumber = 11111;

        public String results = "";
        public List<ClientInfo> clients = new List<ClientInfo>();

        private Boolean stopCond = false;
        private int processingTime = 1000;
        private int listeningQueueSize = 5;

        List<Thread> client_threads = new List<Thread>();
        List<Socket> sockets = new List<Socket>();

        public ConcurrentServer() {

            // Establish the local endpoint
            localEndPoint = new IPEndPoint(ipAddress, portNumber);
            listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            // associate a network address to the Server Socket. All clients must know this address
            listener.Bind(localEndPoint);

            // This is a non-blocking listen with max number of pending requests
            listener.Listen(listeningQueueSize);

            Console.WriteLine("[SERVER] Waiting connection ... ");
            // Suspend while waiting for incoming connection 
            Socket s = listener.Accept();

            Thread t = new Thread(() => HandleClient(s));
            t.Start();

            sockets.Add(s);
            client_threads.Add(t);
        }

        public void HandleClient(Socket s) {
            byte[] bytes = new byte[1024];
            String data = null;
            int numByte = 0;
            string replyMsg = "";
            bool stop;


            while (true) {

                SendMsg(s, Message.welcome);

                stop = false;
                while (!stop) {
                    numByte = s.Receive(bytes);
                    data = Encoding.ASCII.GetString(bytes, 0, numByte);
                    replyMsg = processMessage(data);
                    if (replyMsg.Equals(Message.stopCommunication)) {
                        stop = true;
                        break;
                    }
                    else
                        this.SendMsg(s, replyMsg);
                }

            }

        }

        public string processMessage(string msg) {
            Thread.Sleep(processingTime);
            Console.WriteLine($"[Server] received from the client -> {msg}");
            string replyMsg = "";

            try {
                switch (msg) {
                    case Message.stopCommunication:
                        return msg;
                    default:
                        ClientInfo c = JsonSerializer.Deserialize<ClientInfo>(msg.ToString());
                        clients.Add(c);
                        if (c.clientid == -1) {
                            stopCond = true;
                            printClients();
                        }
                        c.secret = c.studentnr + Message.secret;
                        c.status = Message.statusEnd;
                        replyMsg = JsonSerializer.Serialize<ClientInfo>(c);
                        break;
                }
            }
            catch (Exception e) {
                Console.Out.WriteLine("[Server] processMessage {0}", e.Message);
            }

            return replyMsg;
        }

        public void printClients() {
            string delimiter = " , ";
            Console.Out.WriteLine("[Server] This is the list of clients communicated");
            foreach (ClientInfo c in clients) {
                Console.WriteLine(c.classname + delimiter + c.studentnr + delimiter + c.clientid.ToString());
            }
            Console.Out.WriteLine("[Server] Number of handled clients: {0}", clients.Count);

            clients.Clear();
            stopCond = false;

        }



        private void SendMsg(Socket s, string msg) {
            byte[] encodedMsg = Encoding.ASCII.GetBytes(msg);
            s.Send(encodedMsg);
        }
    }

    public class ServerSimulator {
        public static void sequentialRun() {
            Console.Out.WriteLine("[Server] A sample server, sequential version ...");
            SequentialServer server = new SequentialServer();
            server.prepareServer();
        }
        public static void concurrentRun() {
            new ConcurrentServer();
        }
    }
    public class Program {
        // Main Method 
        public static void Run() {
            Console.Clear();
            ServerSimulator.sequentialRun();
            // todo: uncomment this when the solution is ready.
            //ServerSimulator.concurrentRun();
        }
    }
}
