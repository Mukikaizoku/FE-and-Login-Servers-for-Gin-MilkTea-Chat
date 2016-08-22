using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using WebSocketSharp;
using WebSocketSharp.Server;
using ChatServer.Managers;

namespace ChatServer
{
    /// <summary>
    /// The Server class controls an instance of the server functions.
    /// </summary>
    class Server
    {
        //Connection fields
        private IPEndPoint ipEndPoint;
        private Socket listenSock;
        private Session backEndSession;
        private WebSocketServer webSocket;
        private Protocol protocol;
        private string backEndIp;
        private int backEndPort;
        private int listeningPort;
        private int maxClientNum;

        /// <summary>
        /// A constructor to initialize the server's network setup.
        /// </summary>
        /// <param name="listeningPort">The port the server will be listening on.</param>
        /// <param name="backEndIp">The IP for the back-end server.</param>
        /// <param name="backEndPort">The port for the back-end server.</param>
        /// <param name="maxClientNum">The maximum number of clients.</param>
        /// <param name="protocol">The server's protocol type.</param>
        public Server(int listeningPort, string backEndIp, int backEndPort, int maxClientNum, Protocol proto = Protocol.Tcp)
        {
            this.listeningPort = listeningPort;
            ipEndPoint = new IPEndPoint(IPAddress.Any, listeningPort);
            listenSock = null;
            backEndSession = null;
            this.maxClientNum = maxClientNum;
            SessionManager.GetInstance().Init(maxClientNum, listeningPort);
            protocol = proto;
            this.backEndIp = backEndIp;
            this.backEndPort = backEndPort;
        }

        /// <summary>
        /// The ShutDown method closes down the connections properly to release system resources with care.
        /// </summary>
        public void ShutDown()
        {
            if (listenSock != null)
            {
                listenSock.Shutdown(SocketShutdown.Both);
                listenSock.Close();
            }
            if (webSocket != null)
            {
                webSocket.Stop();
            }

            SessionManager.ShutDown();
            RoomManager.ShutDown();
            
            Console.WriteLine("Server has closed safely. Press enter to quit.");
            Console.ReadLine();
        }

        /// <summary>
        /// The ConnectToBackEnd method establishes the initial connection to the back-end server and creates a session class instance to handle the connection.
        /// </summary>
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
                catch (SocketException)
                {
                    Console.WriteLine("??Where is the back-end??");
                    Thread.Sleep(1000);
                    continue;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                    return;
                }

                Console.WriteLine("Making BackEndSession. . .");

                if ((backEndSession = SessionManager.GetInstance().MakeNewSession(backEndSock, true)) == null)
                {
                    SessionManager.GetInstance().Reset();
                    RoomManager.GetInstance().Reset();
                    backEndSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    continue;
                }
                SessionManager.InitServerProtocol(protocol);
                Console.WriteLine("\t>>Connected to BackEnd server");
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
                    listenSock.Listen(5);                                           //Start listening on the listening socket
                    Console.WriteLine("\t>>Start Listening\n");
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
            //Connect to the back-end server
            Console.WriteLine("Starting Milk Server. . .");
            ConnectToBackEnd();
            
            //In the case of web-server, initialize the web-socket
            if (protocol == Protocol.Web)
            {
                //Start WebSocketService
                Console.WriteLine("Initializing web socket. . .");
                string webIp = LoadIPFromFile();
                webSocket = new WebSocketServer(IPAddress.Parse(webIp), listeningPort);
                webSocket.AddWebSocketService<WebToProtocolTranslator>("/WebToProtocolTranslator");
                webSocket.Start();
                if (webSocket.IsListening)
                {
                    Console.WriteLine("WebSocket is listening on port {0} and providing the following services:", webSocket.Port);
                    foreach (var path in webSocket.WebSocketServices.Paths)
                    {
                        Console.WriteLine("\t " + path);
                    }
                }
                else
                {
                    Console.WriteLine("WebSocket is not active.");
                }
            } else
            {
                Console.WriteLine("Initializing TCP listen at port #" + listeningPort + ". . .");
                StartListen();
                //Async accept operation
                AsyncAcceptHandler();
            }
            while (true)
            {
                MainProcess();
            }
        }

        /***********Asynchronous Accepting Methods Start***********/

        /// <summary>
        /// The AsyncAccept Task initializes a task which accepts a client connection. Returns a Socket for the new connection.
        /// </summary>
        /// <param name="listenSocket">The Socket the server is listening on.</param>
        /// <returns>The task result which is a Socket setup for the newly accepted connection.</returns>
        public Task<Socket> AsyncAccept(Socket listenSocket)
        {
            Task<Socket> task = Task.Run(() => CatchAccept(listenSocket));
            return task;
        }

        /// <summary>
        /// The CatchAccept method safely makes a socket accept call.
        /// </summary>
        /// <param name="listenSocket"></param>
        /// <returns></returns>
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
            }
            catch (Exception e)
            {
                Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                Console.WriteLine("Exception throw during Async-Client_Accept.");
                Console.Write("\tType: " + e.GetType().ToString());
                Console.WriteLine("\tMessage: " + e.Message.ToString());
            }
            return null;
        }

        /// <summary>
        /// The AsyncAcceptHandler method awaits for a new accept connection and then creates a session for that connection.
        /// </summary>
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
            Console.WriteLine("Client (" + session.sessionId + ", " + session.ip + ")" + " is Connected!");       //Report to the console
            AsyncAcceptHandler();
        }

        /***********Asynchronous Accepting Methods End***********/

        /// <summary>
        /// The MainProcess method defines the continuously looped function of the server.
        /// </summary>
        private void MainProcess()
        {
            Thread.Sleep(100000);
            HealthCheckProcess();
        }

        /// <summary>
        /// The HealthCheckProcess method scans through all timed-out sessions and sends heartbeats to check their health.
        /// </summary>
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
            //Close any bad sessions
            Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
            Console.WriteLine(". . .Removing Closed Sessions. . .");
            SessionManager.GetInstance().RemoveClosedSessions();
        }

        /// <summary>
        /// Get the web-socket IP from file.
        /// </summary>
        /// <returns>Returns </returns>
        private string LoadIPFromFile ()
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "websetup.txt");               //Set the location of websetup.txt
            string[] ip = null;

            //Be sure to catch any file reading errors
            try
            {
                ip = File.ReadAllLines(path);                                                       //Read the websetup.txt
            }
            catch (FileNotFoundException e)
            {
                Console.WriteLine("\nFileNotFoundException during websetup.txt reading attempt.\n");
                Console.WriteLine(e.HResult + " : " + e.Message);
                Console.WriteLine("\nPlease ensure that setup.text is contained within the application's directory.");
            }
            catch (Exception e)
            {
                Console.WriteLine("\nException during websetup.txt reading attempt.\n");
                Console.WriteLine(e.HResult + " : " + e.Message);
                Console.WriteLine("\nPlease ensure that setup.text is contained within the application's directory.");
            }

            return ip[0];
        }
    }
}
