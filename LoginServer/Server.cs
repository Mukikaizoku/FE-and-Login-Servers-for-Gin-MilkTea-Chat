using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LoginServer
{
    //Server Controller Class
    class Server
    {
        private IPEndPoint ipEndPoint;
        private Socket listenSock;
        private Session backEndSession;
        //private FBSessionProcessor fbSessionProcessor;
        //private CFSessionProcessor cfSessionProcessor;
        //private Thread acceptingThread;
        
        private string backEndIp;
        private int backEndPort;
        private int listeningPort;
        private int maxClientNum;

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
            Session session = SessionManager.GetInstance().MakeNewSession(newClient, false);                        //Create a new session for the client
            Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
            Console.WriteLine("Client(" + session.sessionId + ", " + session.ip + ")" + " is Connected");         //Report to the console
            AsyncAcceptHandler();
        }

        /// <summary>
        /// The MainProcess method defines the continuously looped function of the server.
        /// </summary>
        private void MainProcess()
        {
            Thread.Sleep(30000);
            HealthCheckProcess();
        }

        private void HealthCheckProcess()
        {
            List<Session> timedoutSessions = SessionManager.GetInstance().GetTimedoutSessions();
            foreach (Session session in timedoutSessions)
            {
                session.ProcessTimeoutSession();
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
            Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
            Console.WriteLine("Removing Closed Sessions. . .");
            SessionManager.GetInstance().RemoveClosedSessions();
        }
    }
}
