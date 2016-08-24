using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LoginServer
{
    struct HeartBeatInfo
    {
        public int clientID;
        public int healthCheckCount;
    }

    //Server Controller Class
    class Server
    {
        private IPEndPoint ipEndPoint;
        private Socket listenSock;
        private Session backEndSession;
        private string backEndIp;
        private int backEndPort;
        private int listeningPort;
        private int maxClientNum;

        Queue<HeartBeatInfo> heartBeatSentQueue = new Queue<HeartBeatInfo>();

        //Constructor that sets up and initializes the server controller
        public Server(int listeningPort, string backEndIp, int backEndPort, int maxClientNum)
        {
            this.listeningPort = listeningPort;
            ipEndPoint = new IPEndPoint(IPAddress.Any, listeningPort);
            listenSock = null;
            backEndSession = null;
            this.maxClientNum = maxClientNum;
            SessionManager.GetInstance().Init(maxClientNum, listeningPort);

            this.backEndIp = backEndIp;
            this.backEndPort = backEndPort;
        }

        public void ShutDown()
        {

            listenSock.Shutdown(SocketShutdown.Both);
            listenSock.Close();

            SessionManager.ShutDown();
            RoomManager.ShutDown();
            
            Console.WriteLine("Server has closed safely.");
        }

        public void ConnectToBackEnd()
        {
            Console.WriteLine("Connecting To BackEnd Server...");
            
            Socket backEndSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            while (true)
            {
                try
                {
                    IPHostEntry ipHost = Dns.GetHostEntry(backEndIp);
                    IPAddress ipAddr = ipHost.AddressList[0];
                    backEndSock.Connect(new IPEndPoint(ipAddr, backEndPort));
                }
                catch (ArgumentNullException)
                {
                    Console.WriteLine("IP or port number is null");
                    return;
                }
                catch (SocketException)
                {
                    Console.WriteLine("Where is he??");
                    Thread.Sleep(1000);
                    continue;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                    return;
                }

                Console.WriteLine("Making BackEndSession");

                if ((backEndSession = SessionManager.GetInstance().MakeNewSession(backEndSock, true)) == null)
                {
                    SessionManager.GetInstance().Reset();
                    RoomManager.GetInstance().Reset();
                    backEndSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    continue;
                }
                Console.WriteLine("Connected to BackEnd server");
                return;
            }
        }

        /// <summary>
        /// The StartListen method initializes our listening socket.
        /// </summary>
        public void StartListen()
        {
            listenSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                listenSock.Bind(ipEndPoint);                                        //Bind the listening socket

                try
                {
                    listenSock.Listen(5);                                //Start listening on the listening socket
                    Console.WriteLine("Start Listening");
                }
                catch (SocketException e)
                {
                    Console.WriteLine("Exception throw during listenSock.Listen.");
                    Console.WriteLine("\tType: " + e.GetType().ToString());
                    Console.WriteLine("\tMessage: " + e.Message.ToString());
                    Console.WriteLine();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception throw during listenSock.Listen.");
                    Console.WriteLine("\tType: " + e.GetType().ToString());
                    Console.WriteLine("\tMessage: " + e.Message.ToString());
                    Console.WriteLine();
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("Exception throw during listenSock.Bind.");
                Console.WriteLine("\tType: " + e.GetType().ToString());
                Console.WriteLine("\tMessage: " + e.Message.ToString());
                Console.WriteLine();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception throw during listenSock.Bind.");
                Console.WriteLine("\tType: " + e.GetType().ToString());
                Console.WriteLine("\tMessage: " + e.Message.ToString());
                Console.WriteLine();
            }
        }

        /// <summary>
        /// The Start method initializes the Server's functionality. 
        /// </summary>
        public void Start()
        {
            Console.WriteLine("Starting Milk Server to listen at port #" + listeningPort + ". . .");
            ConnectToBackEnd();
            
            StartListen();

            //Async accept operation
            AsyncAcceptHandler();

            while (true)
            {
                MainProcess();
            }
        }

        public Task<Socket> AsyncAccept(Socket listenSocket)
        {
            Task<Socket> task = Task.Run(() => CatchAccept(listenSocket));
            return task;
        }

        private Socket CatchAccept(Socket listenSocket)
        {
            try
            {
                return listenSocket.Accept();
            }
            catch (SocketException e)
            {
                Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                Console.WriteLine("Exception throw during Async-Client_Accept.");
                Console.Write("\tType: " + e.GetType().ToString());
                Console.WriteLine("\tMessage: " + e.Message.ToString());
                Console.WriteLine();
            }
            catch (Exception e)
            {
                Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                Console.WriteLine("Exception throw during Async-Client_Accept.");
                Console.Write("\tType: " + e.GetType().ToString());
                Console.WriteLine("\tMessage: " + e.Message.ToString());
                Console.WriteLine();
            }
            return null;
        }

        async private void AsyncAcceptHandler ()
        {
            Socket newClient = null;
            try
            {
                newClient = await AsyncAccept(listenSock);
            }
            catch (SocketException e)
            {
                Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                Console.WriteLine("Exception throw during Async-Client_Accept.");
                Console.Write("\tType: " + e.GetType().ToString());
                Console.WriteLine("\tMessage: " + e.Message.ToString());
            }
            catch (Exception e)
            {
                Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                Console.WriteLine("Exception throw during Async-Client_Accept.");
                Console.Write("\tType: " + e.GetType().ToString());
                Console.WriteLine("\tMessage: " + e.Message.ToString());
            }
            Session session = SessionManager.GetInstance().MakeNewSession(newClient, false);                                //Create a new session for the client
            Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
            Console.WriteLine("[CONNECT] Client " + session.sessionId + " (" + session.ip + ")" + " is Connected");         //Report to the console
            AsyncAcceptHandler();
        }

        /// <summary>
        /// The MainProcess method defines the continuously looped function of the server.
        /// </summary>
        private void MainProcess()
        {
            Thread.Sleep(70000);
            HealthCheckProcess();
        }

        private void HealthCheckProcess()
        {
            HeartBeatInfo heartBeatInfo;
            List<Session> timedoutSessions = SessionManager.GetInstance().GetTimedoutSessions();
            Console.WriteLine("...................................................Sending Heartbeats...................................................");
            foreach (Session session in timedoutSessions)
            {
                if (session.ProcessTimeoutSession(out heartBeatInfo))
                {
                    heartBeatSentQueue.Enqueue(heartBeatInfo);
                }
                if (session.socket == backEndSession?.socket)
                {
                    if (!session.isConnected)
                    {
                        Console.WriteLine("Backend Server is down");
                        SessionManager.GetInstance().Reset();
                        RoomManager.GetInstance().Reset();
                        ConnectToBackEnd();
                    }
                }
            }
            if (heartBeatSentQueue.Count > 0)
            {
                Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
            }
            int x = 0;
            foreach (HeartBeatInfo heart in heartBeatSentQueue)
            {
                Console.Write("[Client " + heart.clientID + ":" + heart.healthCheckCount + "] ");
                x++;
                if (x % 8 == 0)
                {
                    Console.WriteLine();
                }
            }
            Console.WriteLine();
            heartBeatSentQueue.Clear();
            SessionManager.GetInstance().RemoveClosedSessions();
        }
    }
}
