using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using ChatServer.Managers;

namespace ChatServer
{
    /// <summary>
    /// The Session class holds the information for one connection/client (including the connection with the back-end server). Its methods serve to receive and send messages.
    /// </summary>
    class Session
    {
        //Properties
        public WebToProtocolTranslator webSocket
        {
            get;
            private set;
        }
        public static CookieJarManager cookieJarMaster
        {
            get;
            private set;
        }
        public Socket socket
        {
            get;
            private set;
        }
        public IPAddress ip
        {
            get;
            private set;
        }

        public char[] id
        {
            get;
            private set;
        }
        public DateTime lastStartTime
        {
            get;
            private set;
        }
        public static Protocol serverType
        {
            get;
            private set;
        }
        //Session information
        public int sessionId;
        public int roomNo;
        public bool isConnected;
        public bool isHealthCheckSent;
        public bool isWebSession;
        public bool isBackEndSession;
        public int healthCheckCount;

        //Variables needed for Async receive methods
        private byte[] headerByte = new byte[Marshal.SizeOf(typeof(CFHeader))];
        private byte[] body;
        private CFHeader header;
        private FBHeader headerBE;
        private int headerSize;
        private int bodyLength;
        private int undefinedMessageCounter;
        private static Session backEndSession;

        /// <summary>
        /// The ResetStartTime method sets the time of the last action to the current time for the purpose of detecting inactivity.
        /// </summary>
        public void ResetStartTime()
        {
            lastStartTime = DateTime.Now;
        }

        /// <summary>
        /// The standard parameterless constructor initializes variables.
        /// </summary>
        public Session()
        {
            lastStartTime = default(DateTime);
            socket = null;
            ip = null;
            isConnected = false;
            sessionId = -1;
            roomNo = -1;
            isHealthCheckSent = false;
            healthCheckCount = 0;
        }

        /// <summary>
        /// The Init method initializes the session connection with a client.
        /// </summary>
        /// <param name="socket">The socket for the newly accepted connection.</param>
        /// <param name="backEnd">The appropriate back-end session reference.</param>
        public void Init(Socket socket, Session backEnd)
        {
            isConnected = false;
            sessionId = -1;
            roomNo = -1;
            this.socket = socket;
            id = null;
            lastStartTime = DateTime.Now;
            isHealthCheckSent = false;
            healthCheckCount = 0;
            ip = IPAddress.Parse(((IPEndPoint)socket.RemoteEndPoint).Address.ToString());
            backEndSession = backEnd;
            isBackEndSession = false;
            undefinedMessageCounter = 0;
        }
        /// <summary>
        /// The Init method initializes the session connection with a client.
        /// </summary>
        /// <param name="webToProtocolTranslator">The web socket service for the newly accepted connection.</param>
        /// <param name="backEnd">The appropriate back-end session reference.</param>
        public void Init(WebToProtocolTranslator webToProtocolTranslator, Session backEnd)
        {
            isConnected = false;
            sessionId = -1;
            roomNo = -1;
            webSocket = webToProtocolTranslator;
            id = null;
            lastStartTime = DateTime.Now;
            isHealthCheckSent = false;
            healthCheckCount = 0;
            //ip = IPAddress.Parse(((IPEndPoint)socket.RemoteEndPoint).Address.ToString());
            backEndSession = backEnd;
            isBackEndSession = false;
            isWebSession = true;
            undefinedMessageCounter = 0;
        }

        public static void InitSessionStatic(Protocol protocol)
        {
            serverType = protocol;
            cookieJarMaster = new CookieJarManager();
        }

        /// <summary>
        /// The InitBE method initializes the session connection with the back-end server.
        /// </summary>
        /// <param name="socket">The socket for the connection with the back-end server.</param>
        public void InitBE(Socket socket)
        {
            isConnected = false;
            sessionId = -1;
            roomNo = -1;
            this.socket = socket;
            id = null;
            lastStartTime = DateTime.Now;
            isHealthCheckSent = false;
            healthCheckCount = 0;
            ip = IPAddress.Parse(((IPEndPoint)socket.RemoteEndPoint).Address.ToString());
            backEndSession = this;
            isBackEndSession = true;
            undefinedMessageCounter = 0;
        }

        public void LogIn(char[] id)
        {
            this.id = id;
        }

        public bool IsLogedIn()
        {
            return (id == null) ? false : true;
        }

        public void LogOut()
        {
            id = null;
        }

        public bool IsInRoom()
        {
            return (roomNo == -1) ? false : true;
        }

        /*****Asynchronous Message Receive Pattern START*****/
        
        public Task<int> AsyncReceive(byte[] buffer, int size)
        {
            Task<int> task = Task.Run(() => CatchReceive(buffer, size));
            return task;
        }

        public int CatchReceive (byte[] buffer, int size)
        {
            try
            {
                return socket.Receive(buffer, size, 0);
            }
            catch (SocketException e)
            {
                Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                Console.WriteLine("SocketException throw during AsyncReceive Task.");
                Console.Write("\tType: " + e.GetType().ToString());
                Console.WriteLine("\tMessage: " + e.Message.ToString());
            }
            catch (Exception e)
            {
                Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                Console.WriteLine("Exception throw during AsyncReceive Task.");
                Console.Write("\tType: " + e.GetType().ToString());
                Console.WriteLine("\tMessage: " + e.Message.ToString());
            }
            return -1;
        }

        /// <summary>
        /// The StartAsyncHeaderReceive message initiates an asynchronous message receive on a particular session's socket.
        /// </summary>
        async public void StartAsyncHeaderReceive()
        {
            int bytesReceived = 0;
            //Distinguish between back-end messages and client messages
            if (isBackEndSession)
            {
                headerSize = Marshal.SizeOf(typeof(FBHeader));
                headerByte = new byte[Marshal.SizeOf(typeof(FBHeader))];
            } else
            {
                headerSize = Marshal.SizeOf(typeof(CFHeader));
                headerByte = new byte[Marshal.SizeOf(typeof(CFHeader))];
            }
            try
            {
                //Initiate an asynchronous receive call expecting a header
                bytesReceived = await AsyncReceive(headerByte, headerSize);
                //socket.BeginReceive(headerByte, 0, headerSize, 0, new AsyncCallback(OnHeaderReceive), null);
            }
            catch (SocketException e)
            {
                Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                Console.WriteLine("SocketException throw during socket.BeginReceive.");
                Console.Write("\tType: " + e.GetType().ToString());
                Console.WriteLine("\tMessage: " + e.Message.ToString());
            }
            catch (Exception e)
            {
                Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                Console.WriteLine("Exception throw during socket.BeginReceive.");
                Console.Write("\tType: " + e.GetType().ToString());
                Console.WriteLine("\tMessage: " + e.Message.ToString());
            }

            if (bytesReceived == -1) //Fin received
            {
                if (!socket.Connected)
                {
                    if (isBackEndSession)   //Need to reestablish connection with back-end as soon as possible
                    {
                        Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                        Console.WriteLine("*****Connection with back-end server Suddenly Abrupted******");
                        Console.WriteLine("*********************************Initializing back-end reconnect*********************************");
                        IPEndPoint backEndPort = socket.RemoteEndPoint as IPEndPoint;
                        try
                        {
                            socket.Close();
                        }
                        catch (Exception e)
                        {

                        }
                        socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                        bool isBackEndConnected = false;

                        while (isBackEndConnected == false)
                        {
                            try
                            {
                                socket.Connect(backEndPort);
                            }
                            catch (SocketException)
                            {
                                Console.WriteLine("??Where is the back-end??");
                                Thread.Sleep(1000);
                                continue;
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("Exception throw during socket.Connect.");
                                Console.WriteLine("\tType: " + e.GetType().ToString());
                                Console.WriteLine("\tMessage: " + e.Message.ToString());
                                Thread.Sleep(1000);
                            }

                            if (socket.Connected)
                            {
                                Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                                Console.WriteLine(">>Successfully Reconnected to BackEnd server! ! !");
                                Console.WriteLine(">>Reinitializing server functions. . .");
                                isBackEndConnected = true;
                            }
                        }
                        StartAsyncHeaderReceive();
                        return;
                    }
                    else
                    {
                        Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                        Console.WriteLine("*****Connection from " + sessionId + " Suddenly Abrupted******");
                        ConnectionCloseLogout();
                        socket.Close();
                        return;
                    }
                }
            }


            if (isBackEndSession)
            {
                headerBE = (FBHeader)Serializer.ByteToStructure(headerByte, typeof(FBHeader));
                FormatMessage(headerBE, false);
                //Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                //Console.Write("[RECEIVE] ");
                //Console.Write("[" + headerBE.type + "] ");
                //Console.Write("[" + bytesReceived + " bytes] ");
                //Console.Write("[STATE." + headerBE.state + "] ");
                //Console.Write("[LENGTH." + headerBE.length + "] ");
                //Console.WriteLine("[FROM: Gin Back-End]");
                bodyLength = headerBE.length;
            }
            else
            {
                header = (CFHeader)Serializer.ByteToStructure(headerByte, typeof(CFHeader));
                FormatMessage(header, false);
                //Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                //Console.Write("[RECEIVE] ");
                //Console.Write("[" + header.type + "] ");
                //Console.Write("[" + bytesReceived + " bytes] ");
                //Console.Write("[STATE." + header.state + "] ");
                //Console.Write("[LENGTH." + header.length + "] ");
                //Console.WriteLine("[FROM: " + sessionId + "]");
                bodyLength = header.length;

                if (header.type == CFMessageType.ConnectionPass && header.state == CFMessageState.SUCCESS)
                {
                    Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                    Console.WriteLine("Connection Passing succeeded by Client " + sessionId + "!");
                    return;
                }
            }

            if (undefinedMessageCounter > 3)
            {
                Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                Console.WriteLine("*****Undefined Message Spamming from Client " + sessionId + " ******");
                Console.WriteLine(" *****Messages received from Client " + sessionId + " Halted ******");
                undefinedMessageCounter = 0;
                //Need to handle a problematic connection here
                try
                {
                    socket.Close();
                    Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                    Console.WriteLine("*****Connection with " + sessionId + " Closed ******");
                }
                catch (Exception e)
                {
                    Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                    Console.WriteLine("*****Connection with  " + sessionId + " already Closed ******");
                }
                return;
            }
            //If the bodyLength is greater than 0, this means a body message is about to arrive and we need to async receive it
            if (bodyLength > 0)
            {
                StartAsyncBodyReceive();
            }
            else
            {
                //Otherwise, set body to null and process the header-only message
                body = null;
                if (isBackEndSession)
                {
                    ProcessMessage(headerBE, body);
                }
                else
                {
                    ProcessMessage(header, body);
                }
                StartAsyncHeaderReceive();
            }
        }

        /// <summary>
        /// The StartAsyncBodyReceive method initiates an asynchronous wait for a body message.
        /// </summary>
        async public void StartAsyncBodyReceive()
        {
            body = new byte[bodyLength];                                                                //Set the expected body size
            try
            {                                                                                               //new AsyncCallback(OnBodyReceive)
                //headerCallback.Method = OnBodyReceive;
                await AsyncReceive(body, bodyLength);
                //socket.BeginReceive(body, 0, bodyLength, 0, new AsyncCallback(OnBodyReceive), null);    //Initialize an asynchronous receive to collect the expected body message
            }
            catch (SocketException e)
            {
                Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                Console.WriteLine("SocketException throw during socket.BeginReceive.");
                Console.Write("\tType: " + e.GetType().ToString());
                Console.WriteLine("\tMessage: " + e.Message.ToString());
            }
            catch (Exception e)
            {
                Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                Console.WriteLine("Exception throw during socket.BeginReceive.");
                Console.Write("\tType: " + e.GetType().ToString());
                Console.WriteLine("\tMessage: " + e.Message.ToString());
            }

            //Process the message with the appropriate header type and then restart the asynchronous receive cycle
            if (isBackEndSession)
            {
                ProcessMessage(headerBE, body);
            }
            else
            {
                ProcessMessage(header, body);
            }
            StartAsyncHeaderReceive();
        }

        /*****Asynchronous Message Receive Pattern END*****/


        /// <summary>
        /// The ProcessMessage method processes a message.
        /// </summary>
        /// <param name="header">A client-front end header struct.</param>
        /// <param name="body">The body message.</param>
        private void ProcessMessage(CFHeader header, byte[] body)
        {
            CFMessageType type = header.type;
            int bodyLength = header.length;

            //Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
            //Console.WriteLine(">>Received CF " + header.type + " from " + sessionId);

            switch (type)
            {
                case CFMessageType.Signup:
                case CFMessageType.Id_Dup:
                case CFMessageType.LogIn:
                    undefinedMessageCounter = 0;
                    InvalidMessage(header, body);
                    break;

                case CFMessageType.LogOut:
                    undefinedMessageCounter = 0;
                    LoginMessage(header, body);
                    break;

                case CFMessageType.Room_Create:
                case CFMessageType.Room_Join:
                case CFMessageType.Room_Leave:
                case CFMessageType.Room_List:
                    undefinedMessageCounter = 0;
                    RoomMessage(header, body);
                    break;

                case CFMessageType.ConnectionPass:
                    undefinedMessageCounter = 0;
                    ConnectionPassingMessage(header, body);
                    break;

                case CFMessageType.Chat_MSG_From_Client:
                case CFMessageType.Chat_MSG_Broadcast:
                    undefinedMessageCounter = 0;
                    ChatMessage(header, body);
                    break;

                case CFMessageType.Health_Check:
                    undefinedMessageCounter = 0;
                    ResetStartTime();
                    healthCheckCount = 0;
                    isHealthCheckSent = false;
                    break;

                case CFMessageType.Agent_Quit:
                    if (ip == IPAddress.Parse("127.0.0.1"))
                    {
                        Environment.Exit(0);
                    }
                    break;

                default:
                    undefinedMessageCounter++;
                    Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                    Console.WriteLine(">>Undefined Message Type from " + sessionId);
                    break;
            }
        }
        /// <summary>
        /// The ProcessMessage method processes a message.
        /// </summary>
        /// <param name="header">A front-end-back-end header struct.</param>
        /// <param name="body">The body message.</param>
        private void ProcessMessage(FBHeader header, byte[] body)
        {
            FBMessageType type = header.type;
            int bodyLength = header.length;

            //Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
            //Console.WriteLine("**Received FB " + header.type + " from Gin back-end");

            switch (type)
            {
                case FBMessageType.Signup:
                case FBMessageType.Id_Dup:
                case FBMessageType.Login:
                    undefinedMessageCounter = 0;
                    InvalidMessage(header, body);
                    break;
                case FBMessageType.Logout:
                    undefinedMessageCounter = 0;
                    LoginMessage(header, body);
                    break;
                case FBMessageType.Room_Create:
                case FBMessageType.Room_Join:
                case FBMessageType.Room_Leave:
                case FBMessageType.Room_List:
                case FBMessageType.Room_Delete:
                    undefinedMessageCounter = 0;
                    RoomMessage(header, body);
                    break;

                case FBMessageType.Health_Check:
                    undefinedMessageCounter = 0;
                    backEndSession.ResetStartTime();
                    backEndSession.isHealthCheckSent = false;
                    backEndSession.healthCheckCount = 0;
                    break;

                case FBMessageType.Cookie_Run:
                    undefinedMessageCounter = 0;
                    CookieRunMessage(header, body);
                    break;

                case FBMessageType.Connection_Info:
                    undefinedMessageCounter = 0;
                    ConnectionInfo();
                    break;


                case FBMessageType.Kill_Request:
                    Environment.Exit(0);
                    break;

                default:
                    undefinedMessageCounter++;
                    Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                    Console.WriteLine("**Undefined Message Type from Gin back-end");
                    break;
            }
        }


        private void CookieRunMessage (FBHeader header, byte[] body)
        {
            Session clientSession = SessionManager.GetInstance().GetSession(header.sessionId);
            FBCookieRunBody cookieRunBody = (FBCookieRunBody)Serializer.ByteToStructure(body, typeof(FBCookieRunBody));
            //Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
            //Console.Write("!!ID value: " + new string(cookieRunBody.id) + "");
            //Console.WriteLine("\t Cookie: " + cookieRunBody.cookie.ToString() + "");

            int result = cookieJarMaster.AddCookie(new string (cookieRunBody.id), cookieRunBody.cookie);

            if (result >= 0)
            {
                //Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                //Console.WriteLine("!!Login-Cookie for " + new string(cookieRunBody.id) + " successfully added to the cookie jar.");
            } else
            {
                //Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                //Console.WriteLine("!!Login-Cookie for " + new string(cookieRunBody.id) + " was unable to be added to the cookie jar :( . . .");
            }
            CookieJarManager.PrintCookieList();
        }

        /// <summary>
        /// The SignUpMessage method processes a sign-up message.
        /// </summary>
        /// <param name="header"></param>
        /// <param name="body"></param>
        private void SignUpMessage(CFHeader header, byte[] body)
        {
            FBHeader requestHeader = new FBHeader();

            requestHeader.type = FBMessageType.Signup;
            requestHeader.length = Marshal.SizeOf<FBSignupRequestBody>();
            requestHeader.state = FBMessageState.Request;
            requestHeader.sessionId = sessionId;

            byte[] headerByte = Serializer.StructureToByte(requestHeader);
            FormatMessage(requestHeader, true);
            //Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
            //Console.WriteLine(">>Forwarding " + requestHeader.type.ToString() + " TO Gin Back-End Server. . .");
            SendData(backEndSession, headerByte);

            CFSignupRequestBody requestFromClient = (CFSignupRequestBody)Serializer.ByteToStructure(body, typeof(CFSignupRequestBody));

            FBSignupRequestBody requestBody = new FBSignupRequestBody();
            requestBody.id = requestFromClient.id;
            requestBody.password = requestFromClient.password;
            requestBody.IsDummy = requestFromClient.isDummy;

            byte[] bodyByte = Serializer.StructureToByte(requestBody);
            
            SendData(backEndSession, bodyByte);
        }
        private void SignupMessage(FBHeader header, byte[] body)
        {

            Session clientSession = SessionManager.GetInstance().GetSession(header.sessionId);
            CFHeader requestHeader = new CFHeader();

            if (header.state == FBMessageState.Success)
            {
                //Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                //Console.WriteLine("!!Client " + sessionId + " signed up successfully!");
                requestHeader.state = CFMessageState.SUCCESS;
            }
            else if (header.state == FBMessageState.Fail)
            {
                requestHeader.state = CFMessageState.FAIL;
            }
            else
            {
                requestHeader.state = CFMessageState.REQUEST;
            }

            requestHeader.type = CFMessageType.Signup;

            requestHeader.length = 0;

            byte[] headerByte = Serializer.StructureToByte(requestHeader);
            FormatMessage(requestHeader, true);
            //Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
            //Console.WriteLine(">>Forwarding " + requestHeader.type + " to Client " + clientSession.sessionId + ". . .");

            if (isWebSession)
            {
                SendDataThroughWebSocket(clientSession, headerByte, null);
            } else
            {
                SendData(clientSession, headerByte);
            }
        }



        /// <summary>
        /// The ConnectionPassingMessage method receives a connection passing message from a client and verifies the authentication.
        /// </summary>
        /// <param name="header">The received header data.</param>
        /// <param name="body">The received body data.</param>
        private void ConnectionPassingMessage(CFHeader header, byte[] body)
        {
            ConnectionPassRequestBody connectionPassBody = (ConnectionPassRequestBody)Serializer.ByteToStructure(body, typeof(ConnectionPassRequestBody));
            CFHeader responseHeader = new CFHeader();
            responseHeader.type = CFMessageType.ConnectionPass;
            responseHeader.length = 0;

            //Set-up message body to send back to back-end containing the ID for the Client who's connection passing was successful or failed
            FBHeader forwardHeader = new FBHeader();
            FBCookieRunResponseBody cookieRunResponseBody = new FBCookieRunResponseBody();
            cookieRunResponseBody.id = connectionPassBody.id;
            forwardHeader.type = FBMessageType.Cookie_Run;
            forwardHeader.length = Marshal.SizeOf<FBCookieRunResponseBody>();

            //Check validity of the cookie
            if (cookieJarMaster.ValidateCookie(new string (connectionPassBody.id), connectionPassBody.cookie))
            {
                responseHeader.state = CFMessageState.SUCCESS;
                forwardHeader.state = FBMessageState.Success;
                this.LogIn(connectionPassBody.id);
                //Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                //Console.Write(">>Informing " + responseHeader.type + " success to Client " + this.sessionId);
                //Console.WriteLine(" AND " + forwardHeader.type + " success to Gin back-end. . .");
            } else
            {
                responseHeader.state = CFMessageState.FAIL;
                forwardHeader.state = FBMessageState.Fail;
                //Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                //Console.Write(">>Informing " + responseHeader.type + " fail to Client " + this.sessionId);
                //Console.WriteLine(" AND " + forwardHeader.type + " fail to Gin back-end. . .");
            }
            FormatMessage(responseHeader, true);
            FormatMessage(forwardHeader, true);

            byte[] headerByte = Serializer.StructureToByte(responseHeader);

            if (isWebSession)
            {
                SendDataThroughWebSocket(this, headerByte, null);
            }
            else
            {
                SendData(this, headerByte);
            }
            
            //Inform BE that the connection passing request succeeded or failed
            byte[] forwardByte = Serializer.StructureToByte(forwardHeader);
            SendData(backEndSession, forwardByte);
            
            forwardByte = Serializer.StructureToByte(cookieRunResponseBody);
            SendData(backEndSession, forwardByte);

        }
        //private void ConnectionPassingMessage(FBHeader header, byte[] body)
        //{
        //    ConnectionPassRequestBody connectionPassBody = (ConnectionPassRequestBody)Serializer.ByteToStructure(body, typeof(ConnectionPassRequestBody));
        //    CFHeader responseHeader = new CFHeader();
        //    Session clientSession = SessionManager.GetInstance().GetSession(header.sessionId);
        //    //Set the response message state
        //    if (header.state == FBMessageState.Success)
        //    {
        //        responseHeader.state = CFMessageState.SUCCESS;
        //        FBLoginResponseBody responseFromBackEnd = (FBLoginResponseBody)Serializer.ByteToStructure(body, typeof(FBLoginResponseBody));
        //        clientSession.LogIn(responseFromBackEnd.id);
        //        Console.WriteLine(new string(responseFromBackEnd.id) + " is logged in");
        //    }
        //    else
        //    {
        //        responseHeader.state = CFMessageState.FAIL;
        //    }



        //    if (header.state == FBMessageState.Success)
        //    {

        //    }
        //    else if (header.state == FBMessageState.Fail)
        //    {
        //        Console.WriteLine("!!Client " + sessionId + "'s Login Fail");
        //    }
        //    responseHeader.type = CFMessageType.LogIn;
        //    //Take action on id and cookie

        //    responseHeader.length = 0;

        //    byte[] headerByte = Serializer.StructureToByte(responseHeader);
        //    Console.WriteLine(">>Forwarding " + responseHeader.type + " to Client " + clientSession.sessionId + ". . .");

        //    if (clientSession.isWebSession)
        //    {
        //        SendDataThroughWebSocket(clientSession, headerByte, null);
        //    }
        //    else
        //    {
        //        SendData(clientSession, headerByte);
        //    }

        //}

        private void InvalidMessage(CFHeader header, byte[] body)
        {
            CFHeader responseHeader = new CFHeader();
            responseHeader.type = header.type;
            responseHeader.state = CFMessageState.FAIL;

            responseHeader.length = Marshal.SizeOf<CFRoomResponseBody>();

            byte[] failHeaderByte = Serializer.StructureToByte(responseHeader);

            CFRoomResponseBody failResponseBody = new CFRoomResponseBody();

            byte[] failBodyByte = Serializer.StructureToByte(failResponseBody);

            FormatMessage(responseHeader, true);
            //Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
            //Console.WriteLine("**Responding " + responseHeader.type.ToString() + " fail to Client " + sessionId + ". . .");
            failHeaderByte = CombineMessagesForClient(failHeaderByte, failBodyByte, null);
            SendData(this, failHeaderByte);
            return;
        }
        private void InvalidMessage(FBHeader header, byte[] body)
        {
            FBHeader responseHeader = new FBHeader();
            responseHeader.type = header.type;
            responseHeader.state = FBMessageState.Fail;

            responseHeader.length = Marshal.SizeOf<CFRoomResponseBody>();

            byte[] failHeaderByte = Serializer.StructureToByte(responseHeader);

            CFRoomResponseBody failResponseBody = new CFRoomResponseBody();

            byte[] failBodyByte = Serializer.StructureToByte(failResponseBody);

            FormatMessage(responseHeader, true);
            //Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
            //Console.WriteLine("**Responding " + responseHeader.type.ToString() + " fail to Client " + sessionId + ". . .");
            failHeaderByte = CombineMessagesForClient(failHeaderByte, failBodyByte, null);
            SendData(this, failHeaderByte);
            return;
        }



        private void LoginMessage(CFHeader header, byte[] body)
        {
            FBHeader requestHeader = new FBHeader();
            switch (header.type)
            {
                case CFMessageType.Id_Dup:
                    {
                        requestHeader.type = FBMessageType.Id_Dup;
                        break;
                    }
                case CFMessageType.LogIn:
                    {
                        requestHeader.type = FBMessageType.Login;
                        break;
                    }
                case CFMessageType.LogOut:
                    {
                        requestHeader.type = FBMessageType.Logout;
                        break;
                    }
                default:
                    Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                    Console.WriteLine("**Undefined Login Message Type from " + socket.RemoteEndPoint);
                    return;
            }

            requestHeader.length = body.Length;
            requestHeader.state = FBMessageState.Request;
            requestHeader.sessionId = sessionId;

            byte[] headerByte = Serializer.StructureToByte(requestHeader);
            FormatMessage(requestHeader, true);
            //Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
            //Console.WriteLine(">>Forwarding " + requestHeader.type.ToString() + " to Gin Back-End Server. . .");
            SendData(backEndSession, headerByte);

            CFLoginRequestBody requestFromClient = (CFLoginRequestBody)Serializer.ByteToStructure(body, typeof(CFLoginRequestBody));

            FBLoginRequestBody requestBody = new FBLoginRequestBody();
            requestBody.id = requestFromClient.id;
            requestBody.password = requestFromClient.password;

            byte[] bodyByte = Serializer.StructureToByte(requestBody);
            
            SendData(backEndSession, bodyByte);
        }
        private void LoginMessage(FBHeader header, byte[] body)
        {
            Session clientSession = SessionManager.GetInstance().GetSession(header.sessionId);

            CFHeader responseHeader = new CFHeader();

            if (header.state == FBMessageState.Success)
            {
                responseHeader.state = CFMessageState.SUCCESS;
            }
            else if (header.state == FBMessageState.Fail)
            {
                responseHeader.state = CFMessageState.FAIL;
            }
            else
            {
                responseHeader.state = CFMessageState.REQUEST;
            }

            switch (header.type)
            {
                case FBMessageType.Id_Dup:
                    {
                        if (header.state == FBMessageState.Success)
                        {
                            //Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                            //Console.WriteLine("!!Id " + sessionId + " not duplicated");
                        }
                        responseHeader.type = CFMessageType.Id_Dup;
                        break;
                    }
                case FBMessageType.Login:
                    {
                        if (header.state == FBMessageState.Success)
                        {
                            FBLoginResponseBody responseFromBackEnd = (FBLoginResponseBody)Serializer.ByteToStructure(body, typeof(FBLoginResponseBody));
                            clientSession.LogIn(responseFromBackEnd.id);
                            Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                            Console.WriteLine(new string(responseFromBackEnd.id) + " is logged in.");
                        }
                        else if (header.state == FBMessageState.Fail)
                        {
                            Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                            Console.WriteLine("!!Client " + sessionId + "'s Login Fail.");
                        }
                        responseHeader.type = CFMessageType.LogIn;
                        break;
                    }
                case FBMessageType.Logout:
                    {
                        if (clientSession == null)
                        {
                            if (header.type == FBMessageType.Logout)
                                return;

                            //Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                            //Console.WriteLine("!!Client " + sessionId + " is already Logged Out");
                            return;
                        }
                        if (header.state == FBMessageState.Success)
                        {
                            if (clientSession.IsInRoom())
                            {
                                RoomManager.GetInstance().RemoveUserInRoom(clientSession);
                            }
                        }
                        responseHeader.type = CFMessageType.LogOut;
                        break;
                    }
                default:
                    Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                    Console.WriteLine("**Undefined Login Message Type from Client " + sessionId);
                    return;

            }

            responseHeader.length = 0;

            byte[] headerByte = Serializer.StructureToByte(responseHeader);
            FormatMessage(responseHeader, true);
            //Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
            //Console.WriteLine(">>Forwarding " + responseHeader.type + " to Client " + clientSession.sessionId + ". . .");

            if (clientSession.isWebSession)
            {
                SendDataThroughWebSocket(clientSession, headerByte, null);
            }
            else
            {
                SendData(clientSession, headerByte);
            }

            //LogOut USer after sending log out confirmation
            if (header.type == FBMessageType.Logout && header.state == FBMessageState.Success)
            {
                Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                Console.WriteLine("!!Client " + new string(clientSession.id) + " is logged out");
                clientSession.LogOut();

            }
        }

        private void RoomMessage (CFHeader header, byte[] body)
        {
            if (!IsLogedIn())
            {
                CFHeader responseHeader = new CFHeader();
                responseHeader.type = header.type;
                responseHeader.state = CFMessageState.FAIL;

                responseHeader.length = Marshal.SizeOf<CFRoomResponseBody>();

                byte[] failHeaderByte = Serializer.StructureToByte(responseHeader);

                CFRoomResponseBody failResponseBody = new CFRoomResponseBody();

                byte[] failBodyByte = Serializer.StructureToByte(failResponseBody);


                FormatMessage(responseHeader, true);
                //Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                //Console.WriteLine("**Responding " + responseHeader.type.ToString() + " fail to Client " + sessionId + ". . .");
                if (isWebSession)
                {
                    SendDataThroughWebSocket(this, failHeaderByte, failBodyByte);
                } else
                {
                    failHeaderByte = CombineMessagesForClient(failHeaderByte, failBodyByte, null);
                    SendData(this, failHeaderByte);
                }
                return;
            }

            if (header.type == CFMessageType.Room_List)
            {
                body = null;
            }

            CFRoomRequestBody requestFromClient = new CFRoomRequestBody();
            if (body != null && header.type == CFMessageType.Room_Join)
            {
                requestFromClient = (CFRoomRequestBody)Serializer.ByteToStructure(body, typeof(CFRoomRequestBody));
            }

            FBHeader requestHeader = new FBHeader();
            switch (header.type)
            {
                case CFMessageType.Room_Create:
                    {
                        requestHeader.type = FBMessageType.Room_Create;
                        break;
                    }
                case CFMessageType.Room_Join:
                    {
                        //Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                        //Console.WriteLine("!!Client " + sessionId + " requests to join Room " + requestFromClient.roomNo);
                        requestHeader.type = FBMessageType.Room_Join;
                        break;
                    }
                case CFMessageType.Room_Leave:
                    {
                        FormatBroadCastMessage(header, sessionId);
                        SendBroadCast(CFMessageType.Room_Leave, id, roomNo, null);
                        requestHeader.type = FBMessageType.Room_Leave;
                        break;
                    }
                case CFMessageType.Room_List:
                    {
                        requestHeader.type = FBMessageType.Room_List;
                        break;
                    }
                default:
                    Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                    Console.WriteLine("**Undefined Room Message Type from Client " + sessionId);
                    return;
            }
            requestHeader.state = FBMessageState.Request;
            requestHeader.sessionId = sessionId;
            requestHeader.length = Marshal.SizeOf(typeof(FBRoomRequestBody));
            byte[] headerByte = Serializer.StructureToByte(requestHeader);
            FormatMessage(requestHeader, true);
            //Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
            //Console.WriteLine(">>Forwarding " + requestHeader.type.ToString() + " to Gin Back-End Server. . .");
            SendData(backEndSession, headerByte);
            
            if (body == null && header.type != CFMessageType.Room_Create)
                return;
            
            FBRoomRequestBody requestBody = new FBRoomRequestBody();
            requestBody.id = id;
            requestBody.roomNo = requestFromClient.roomNo;

            byte[] bodyByte = Serializer.StructureToByte(requestBody);
            
            SendData(backEndSession, bodyByte);
        }
        private void RoomMessage(FBHeader header, byte[] body)
        {
            Session clientSession = SessionManager.GetInstance().GetSession(header.sessionId);

            if (header.type != FBMessageType.Room_Delete)
            {
                if (clientSession == null)
                    return;
            }
            CFHeader requestHeader = new CFHeader();
            CFLoginResponseBody responseBody = new CFLoginResponseBody();

            if (header.state == FBMessageState.Success)
            {
                requestHeader.state = CFMessageState.SUCCESS;
            }
            else if (header.state == FBMessageState.Fail)
            {
                requestHeader.state = CFMessageState.FAIL;
            }
            else
            {
                requestHeader.state = CFMessageState.REQUEST;
            }

            switch (header.type)
            {
                case FBMessageType.Room_Create:
                    {
                        if (header.state == FBMessageState.Success)
                        {
                            //Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                            //Console.WriteLine("!!Room Create Success");
                            int roomNo = BitConverter.ToInt32(body, 0);
                            RoomManager.GetInstance().MakeNewRoom(roomNo);
                        }
                        requestHeader.type = CFMessageType.Room_Create;
                        break;
                    }
                case FBMessageType.Room_Delete:
                    {
                        if (header.state == FBMessageState.Success)
                        {
                            //Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                            //Console.WriteLine("!!Room Delete Success");
                            int roomNo = BitConverter.ToInt32(body, 0);
                            RoomManager.GetInstance().RemoveRoom(roomNo);

                            return; // no user to send response.
                        }
                        //requestHeader.type = CFMessageType.Room_Delete;
                        break;
                    }
                case FBMessageType.Room_Join:
                    {
                        if (header.state == FBMessageState.Success)
                        {
                            //Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                            //Console.WriteLine("!!Room Join Success");
                            int roomNo = BitConverter.ToInt32(body, 0);
                            CFRoomRequestBody rb = (CFRoomRequestBody)Serializer.ByteToStructure(body, typeof(CFRoomRequestBody));
                            //Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                            //Console.WriteLine(">>Broadcasting " + clientSession.sessionId + " joining room " + rb.roomNo + ". . .");
                            FormatBroadCastMessage(header, clientSession.sessionId);
                            SendBroadCast(CFMessageType.Room_Join, clientSession.id, rb.roomNo, null);
                            RoomManager.GetInstance().AddUserInRoom(clientSession, roomNo);
                        }
                        else if (header.state == FBMessageState.Fail)
                        {
                            //Determine if the room join failure is due to lack of existence or if it's in a different server
                            requestHeader.type = CFMessageType.Room_Join;
                            if (header.length == 0)
                            {
                                Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                                Console.WriteLine("!!Room Join Fail");
                                body = null;
                            } else
                            {
                                //Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                                //Console.WriteLine("!!Room is in different server. . . sending connection passing information. . .");
                                CFLoginResponseBody responseFromBackEnd = (CFLoginResponseBody)Serializer.ByteToStructure(body, typeof(CFLoginResponseBody));
                                //clientSession.LogIn(responseFromBackEnd.i);
                                //Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                                //Console.WriteLine(">>" + new string (responseFromBackEnd.id) + " login-in credentials authorized.");

                                //Transfer the back-end's response data to a body that the client can read
                                requestHeader.length = Marshal.SizeOf<CFLoginResponseBody>();
                                responseBody.ip = responseFromBackEnd.ip;
                                responseBody.port = responseFromBackEnd.port;
                                responseBody.protocolType = responseFromBackEnd.protocolType;
                                responseBody.cookie = responseFromBackEnd.cookie;


                                FormatMessage(requestHeader, true);
                                //Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                                //Console.WriteLine(">>Forwarding " + responseHeader.type.ToString() + " to Client " + clientSession.sessionId + ". . .");
                                Console.Write("\t\t>>IP: " + new string(responseBody.ip));
                                Console.Write("\t>>Port: " + responseBody.port.ToString());
                                Console.Write("\t>>Proto: \t" + responseBody.protocolType.ToString());
                                Console.WriteLine("\t>>Cookie: " + responseBody.cookie.ToString());
                            }
                        }

                        break;
                    }
                case FBMessageType.Room_Leave:
                    {
                        if (header.state == FBMessageState.Success)
                        {
                            //Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                            //Console.WriteLine("!!Room Leave Success");
                            RoomManager.GetInstance().RemoveUserInRoom(clientSession);
                        }
                        requestHeader.type = CFMessageType.Room_Leave;
                        break;
                    }
                case FBMessageType.Room_List:
                    {
                        if (body == null)
                        {
                            //Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                            //Console.WriteLine("!!Room List is null");
                        }
                        //Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                        //Console.WriteLine("!!Room List Success");
                        requestHeader.type = CFMessageType.Room_List;
                        break;
                    }
                default:
                    Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                    Console.WriteLine("**Undefined Room Message Type from Back-End");
                    return;
            }
            if (body == null)
            {
                requestHeader.length = 0;
            }
            else if (header.type != FBMessageType.Room_Join)
            {
                requestHeader.length = body.Length;
            }

            byte[] headerByte = Serializer.StructureToByte(requestHeader);
            FormatMessage(requestHeader, true);

            byte[] bodyByte;

            //We only need to send a body if the case is Login Success reporting back to the client
            if (header.type == FBMessageType.Room_Join && header.state == FBMessageState.Fail && body != null)
            {
                bodyByte = Serializer.StructureToByte(responseBody);
                headerByte = CombineMessagesForClient(headerByte, bodyByte, null);
                Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                Console.WriteLine("**Definitely loaded the bodybyte");
            } else
            {
                bodyByte = body;
            }

            //if (body == null)
            //    return;


            //Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
            //Console.WriteLine(">>Forwarding " + requestHeader.type.ToString() + " back to Client " + header.sessionId + ". . .");

            if (clientSession.isWebSession)
            {
                SendDataThroughWebSocket(clientSession, headerByte, bodyByte);
            }
            else
            {
                headerByte = CombineMessagesForClient(headerByte, bodyByte, null);
                SendData(clientSession, headerByte);
            }
        }

        private void ChatMessage(CFHeader header, byte[] body)
        {
            if (!IsInRoom())
            {
                CFHeader responseHeader = new CFHeader();
                responseHeader.type = header.type;
                responseHeader.state = CFMessageState.FAIL;
                responseHeader.length = 0;

                byte[] failHeaderByte = Serializer.StructureToByte(responseHeader);
                FormatMessage(responseHeader, true);
                //Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                //Console.WriteLine("**Responding " + responseHeader.type.ToString() + " fail back to Client " + sessionId + ". . .");
                SendData(this, failHeaderByte);
                return;
            }

            FBHeader requestHeader = new FBHeader();
            switch (header.type)
            {
                case CFMessageType.Chat_MSG_From_Client:
                    {
                        if (body != null)
                        {
                            //Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                            //Console.WriteLine(">>Broadcasting Message from Client " + sessionId);
                            FormatBroadCastMessage(header, sessionId);
                            SendBroadCast(CFMessageType.Chat_MSG_Broadcast, id, roomNo, body);
                        }
                        requestHeader.type = FBMessageType.Chat_Count;
                        break;
                    }
                default:
                    Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                    Console.WriteLine("**Undefined Chat Message Type from Client " + sessionId);
                    return;
            }

            requestHeader.state = FBMessageState.Request;
            requestHeader.sessionId = sessionId;

            FBChatRequestBody requestBody = new FBChatRequestBody();

            requestBody.id = id;

            byte[] bodyByte = Serializer.StructureToByte(requestBody);
            requestHeader.length = bodyByte.Length;

            byte[] headerByte = Serializer.StructureToByte(requestHeader);
            FormatMessage(requestHeader, true);
            //Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
            //Console.WriteLine(">>Forwarding " + requestHeader.type.ToString() + " to Gin Back-End Server. . .");
            SendData(backEndSession, headerByte);
            SendData(backEndSession, bodyByte);
        }
        

        private void HealthCheck(FBHeader header)
        {

            if (header.state == FBMessageState.Request)
            {
                FBHeader requestHeader = new FBHeader();
                requestHeader.state = FBMessageState.Success;
                requestHeader.sessionId = header.sessionId;
                requestHeader.type = FBMessageType.Health_Check;
                requestHeader.length = 0;


                byte[] headerByte = Serializer.StructureToByte(requestHeader);
                FormatMessage(requestHeader, true);
                //Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                //Console.WriteLine("Sending " + requestHeader.type.ToString() + " to Gin Back-End Server. . .");
                SendData(backEndSession, headerByte);
            }
            else
            {
                backEndSession.ResetStartTime();
                return;
            }
        }

        private void ConnectionInfo()
        {
            char[] ip = new char[15];

            foreach (IPAddress address in Dns.GetHostAddresses(Dns.GetHostName()))
            {
                string s = address.ToString();
                if (address.AddressFamily == AddressFamily.InterNetwork)
                {
                    Array.Copy(address.ToString().ToCharArray(), ip, address.ToString().Length);
                    break;
                }
            }

            FBHeader header = new FBHeader();
            header.type = FBMessageType.Connection_Info;
            header.state = FBMessageState.Success;

            FBConnectionInfoBody info = new FBConnectionInfoBody();
            info.ip = ip;
            info.port = SessionManager.GetInstance().GetServicePort();
            info.protocol = serverType;

            byte[] body = Serializer.StructureToByte(info);
            header.length = body.Length;
            header.sessionId = -1;

            byte[] headerByte = Serializer.StructureToByte(header);
            backEndSession.socket.Send(headerByte);

            backEndSession.socket.Send(body);
        }


        private bool SendData(Session session, byte[] buf)
        {
            if (session == null)
            {
                Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                Console.WriteLine("Session is null");
            }
            try
            {
                session.socket.Send(buf);
            }
            catch (SocketException)
            {
                session.LogOut();
                return false;
            }
            catch (ObjectDisposedException)
            {
                session.LogOut();
                return false;
            }
            catch (Exception e)
            {
                Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                Console.WriteLine(e.ToString());
                session.LogOut();
                return false;
            }
            return true;
        }
        
        private bool SendDataThroughWebSocket (Session client, byte[] headerBuff, byte[] bodyBuff)
        {
            if (client.webSocket == null || client.webSocket.State == WebSocketSharp.WebSocketState.Closed || client.webSocket.State == WebSocketSharp.WebSocketState.Closing)
            {
                Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                Console.WriteLine(">>Client " + client.sessionId + " WebSocket is dead or closed<<");
                return false;
            }
            //Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
            //Console.WriteLine(">>Sending message to Client " + client.sessionId + " by WebSocket<<");
            byte[] data = null;
            if (bodyBuff != null)
            {
                data = new byte[headerBuff.Length + bodyBuff.Length];
                Array.Copy(headerBuff, 0, data, 0, headerBuff.Length);
                Array.Copy(bodyBuff, 0, data, headerBuff.Length, bodyBuff.Length);
            } else
            {
                data = headerBuff;
            }
            try
            {
                client.webSocket.SendData(data);
            }
            catch (Exception e)
            {
                Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                Console.WriteLine("Exception throw during webSocket.SendData.");
                Console.Write("\tType: " + e.GetType().ToString());
                Console.WriteLine("\tMessage: " + e.Message.ToString());
                return false;
            }
            return true;
        }

        private void SendBroadCast(CFMessageType type, char[] id, int roomNo, byte[] message)
        {
            List<Session> users = RoomManager.GetInstance().GetUsersInRoom(roomNo);
            CFHeader header = new CFHeader();
            string msg;
            switch (type)
            {
                case CFMessageType.Room_Join:
                    msg = new string(id) + "has joined room " + roomNo;
                    message = Encoding.UTF8.GetBytes(msg);
                    break;
                case CFMessageType.Room_Leave:
                    msg = new string(id) + "has left room " + roomNo;
                    message = Encoding.UTF8.GetBytes(msg);
                    break;
            }
            header.type = CFMessageType.Chat_MSG_Broadcast;
            header.state = CFMessageState.REQUEST;

            CFChatResponseBody body = new CFChatResponseBody();
            body.date = DateTime.Now;
            body.id = id;

            if (message != null)
            {
                Console.WriteLine("\t\t       [LENGTH]: " + (message.Length + Marshal.SizeOf(typeof(CFHeader)) + Marshal.SizeOf(typeof(CFChatResponseBody))));
                body.msgLen = message.Length;
            } else
            {
                Console.WriteLine("\t\t       [LENGTH]: " + (Marshal.SizeOf(typeof(CFHeader)) + Marshal.SizeOf(typeof(CFChatResponseBody))));
                body.msgLen = 0;
            }

            byte[] bodyByte = Serializer.StructureToByte(body);

            header.length = bodyByte.Length;

            byte[] headerByte = Serializer.StructureToByte(header);
            
            foreach (var user in users)
            {
                Console.WriteLine("\t\t       [ SEND ]: Client " + user.sessionId);
                if (user.isWebSession)
                {
                    if (body.msgLen > 0)
                    {
                        byte[] combinedMessage = new byte[bodyByte.Length + message.Length];
                        Array.Copy(bodyByte, 0, combinedMessage, 0, bodyByte.Length);
                        Array.Copy(message, 0, combinedMessage, bodyByte.Length, message.Length);
                        user.SendDataThroughWebSocket(user, headerByte, combinedMessage);
                    } else
                    {
                        user.SendDataThroughWebSocket(user, headerByte, bodyByte);
                    }
                } else
                {
                    //Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                    //Console.WriteLine("\t::Total Length: " + headerByte.Length);
                    if (body.msgLen > 0)
                    {
                        headerByte = CombineMessagesForClient(headerByte, bodyByte, message);
                        user.SendData(user, headerByte);
                    } else
                    {
                        headerByte = CombineMessagesForClient(headerByte, bodyByte, null);
                        user.SendData(user, headerByte);
                    }
                }
            }
        }

        public bool ProcessTimeoutSession(out HeartBeatInfo heartBeatInfo)                                         //Health_check healthcheck heartbeat heart_beat
        {
            heartBeatInfo = new HeartBeatInfo();
            if (socket == backEndSession.socket)
            {
                if (!backEndSession.isHealthCheckSent)
                {
                    heartBeatInfo.clientID = sessionId;
                    heartBeatInfo.healthCheckCount = healthCheckCount;
                    FBHeader header = new FBHeader();

                    header.type = FBMessageType.Health_Check;
                    header.state = FBMessageState.Request;
                    header.length = 0;
                    header.sessionId = backEndSession.sessionId;

                    byte[] headerByte = Serializer.StructureToByte(header);
                    //FormatMessage(header, true);
                    //Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                    //Console.WriteLine(">>Sending " + header.type.ToString() + " to Gin Back-End Server. . .");
                    SendData(backEndSession, headerByte);
                    backEndSession.healthCheckCount = 0;
                    backEndSession.ResetStartTime();
                    return true;
                }
                else
                {
                    backEndSession.isConnected = false;
                }
            } else
            {
                if (!isHealthCheckSent)
                {
                    heartBeatInfo.clientID = sessionId;
                    heartBeatInfo.healthCheckCount = healthCheckCount;
                    CFHeader header = new CFHeader();

                    header.type = CFMessageType.Health_Check;
                    header.state = CFMessageState.REQUEST;
                    header.length = 0;

                    byte[] headerByte = Serializer.StructureToByte(header);
                    //FormatMessage(header, true);
                    //Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                    //Console.WriteLine(">>Sending " + header.type.ToString() + " to Client " + sessionId + ". . .");
                    if (isWebSession)
                    {
                        this.SendDataThroughWebSocket(this, headerByte, null);
                    } else
                    {
                        SendData(this, headerByte);
                    }
                    ResetStartTime();
                    isHealthCheckSent = true;
                    return true;
                }
                else
                {
                    ConnectionCloseLogout();
                    isConnected = false;
                }
            }
            return false;
        }

        public void ConnectionCloseLogout()
        {
            CFHeader fakeHeader = new CFHeader();
            fakeHeader.type = CFMessageType.LogOut;
            fakeHeader.state = CFMessageState.REQUEST;
            fakeHeader.length = Marshal.SizeOf(typeof(CFLoginRequestBody));

            CFLoginRequestBody fakeBody = new CFLoginRequestBody();
            fakeBody.id = id;

            byte[] fakeBodyByte = Serializer.StructureToByte(fakeBody);

            if (!IsLogedIn())   //Don't resend a logout if already logged out
            {
                return;
            }
            LoginMessage(fakeHeader, fakeBodyByte);
        }

        private byte[] CombineMessagesForClient (byte[] one, byte[] two, byte[] three)
        {
            byte[] combinedByte = null;
            if (one == null)
            {
                return null;
            } else if (two == null)
            {
                return one;
            } else if (three == null)
            {
                combinedByte = new byte[one.Length + two.Length];
                Array.Copy(one, 0, combinedByte, 0, one.Length);
                Array.Copy(two, 0, combinedByte, one.Length, two.Length);
            } else
            {
                combinedByte = new byte[one.Length + two.Length + three.Length];
                Array.Copy(one, 0, combinedByte, 0, one.Length);
                Array.Copy(two, 0, combinedByte, one.Length, two.Length);
                Array.Copy(three, 0, combinedByte, one.Length + two.Length, three.Length);
            }
            return combinedByte;
        }


        /******Web Socket Functions Start******/

        public void ProcessWebSocketMessage (byte[] data)
        {
            headerSize = Marshal.SizeOf(typeof(CFHeader));
            headerByte = new byte[Marshal.SizeOf(typeof(CFHeader))];
            if (data.Length < headerSize)
            {
                Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                Console.WriteLine("Unusually short WebSocket from Client " + sessionId);
                return;
            }
            
            Array.Copy(data, headerByte, headerSize);
            header = (CFHeader)Serializer.ByteToStructure(headerByte, typeof(CFHeader));
            FormatMessage(header, false);
            //Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
            //Console.WriteLine("Web Socket Message Received from Client " + sessionId);
            //Console.Write("\t\t Type: " + header.type);
            //Console.Write("\t\t State: " + header.state);
            //Console.WriteLine("\t\t Length: " + header.length);
            bodyLength = header.length;

            if (data.Length < headerSize + bodyLength)
            {
                Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                Console.WriteLine("Body data from WebSocket incomplete from Client " + sessionId);
                return;
            }

            body = new byte[bodyLength];
            Array.Copy(data, headerSize, body, 0, bodyLength);

            ProcessMessage(header, body);
        }

        /******Web Socket Functions End******/


        private void FormatMessage(CFHeader header, bool isSend)
        {
            int spacerLength;
            Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
            if (isSend)
            {
                Console.Write("[  SEND ] ");
            }
            else
            {
                Console.Write("[RECEIVE] ");
            }
            spacerLength = header.type.ToString().Length;
            spacerLength = 20 - spacerLength;
            if (spacerLength < 0)
            {
                spacerLength = 0;
            }
            Console.Write("[");
            Console.Write(header.type);
            for (int i = 0; i < spacerLength; i++)
            {
                Console.Write(" ");
            }
            Console.Write("] ");
            if (header.state == CFMessageState.FAIL)
            {
                Console.Write("[STATE:  " + header.state + " ] ");
            }
            else
            {
                Console.Write("[STATE:" + header.state + "] ");
            }
            spacerLength = header.length.ToString().Length;
            spacerLength = 5 - spacerLength;
            if (spacerLength < 0)
            {
                spacerLength = 0;
            }
            Console.Write("[BODY:");
            for (int i = 0; i < spacerLength; i++)
            {
                Console.Write(" ");
            }
            Console.Write(header.length + "] ");
            if (isSend)
            {
                Console.Write("[  TO: Client " + sessionId + "] ");
            }
            else
            {
                Console.Write("[FROM: Client " + sessionId + "] ");
            }
            //if (header.type == CFMessageType.Health_Check)
            //{
            //    Console.Write("[COUNT: " + healthCheckCount + "] ");
            //}
            Console.WriteLine();
        }
        private void FormatMessage(FBHeader header, bool isSend)
        {
            int spacerLength;
            Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
            if (isSend)
            {
                Console.Write("[  SEND ] ");
            }
            else
            {
                Console.Write("[RECEIVE] ");
            }
            spacerLength = header.type.ToString().Length;
            spacerLength = 20 - spacerLength;
            if (spacerLength < 0)
            {
                spacerLength = 0;
            }
            Console.Write("[");
            Console.Write(header.type);
            for (int i = 0; i < spacerLength; i++)
            {
                Console.Write(" ");
            }
            Console.Write("] ");
            if (header.state == FBMessageState.Fail)
            {
                Console.Write("[STATE:  " + header.state + " ] ");
            }
            else
            {
                Console.Write("[STATE:" + header.state + "] ");
            }
            spacerLength = header.length.ToString().Length;
            spacerLength = 5 - spacerLength;
            if (spacerLength < 0)
            {
                spacerLength = 0;
            }
            Console.Write("[BODY:");
            for (int i = 0; i < spacerLength; i++)
            {
                Console.Write(" ");
            }
            Console.Write(header.length + "] ");
            if (isSend)
            {
                Console.Write("[  TO: Gin Back-End] ");
            }
            else
            {
                Console.Write("[FROM: Gin Back-End] ");
            }
            Console.Write("[SERVING: Client " + header.sessionId + "] ");
            //if (header.type == FBMessageType.Health_Check)
            //{
            //    Console.Write("[COUNT: " + healthCheckCount + "] ");
            //}
            Console.WriteLine();
        }
        private void FormatBroadCastMessage(CFHeader header, int sessionID)
        {
            int spacerLength;
            Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
            Console.Write("[BRDCAST] ");
            spacerLength = header.type.ToString().Length;
            spacerLength = 20 - spacerLength;
            if (spacerLength < 0)
            {
                spacerLength = 0;
            }
            Console.Write("[");
            Console.Write(header.type);
            for (int i = 0; i < spacerLength; i++)
            {
                Console.Write(" ");
            }
            Console.Write("] ");
            if (header.state == CFMessageState.FAIL)
            {
                Console.Write("[STATE:  " + header.state + " ] ");
            }
            else
            {
                Console.Write("[STATE:" + header.state + "] ");
            }
            spacerLength = header.length.ToString().Length;
            spacerLength = 5 - spacerLength;
            if (spacerLength < 0)
            {
                spacerLength = 0;
            }
            Console.Write("[BODY:");
            for (int i = 0; i < spacerLength; i++)
            {
                Console.Write(" ");
            }
            Console.Write(header.length + "] ");
            Console.Write("[FROM: Client " + sessionId + "] ");
            //if (header.type == CFMessageType.Health_Check)
            //{
            //    Console.Write("[COUNT: " + healthCheckCount + "] ");
            //}
            Console.WriteLine();
        }
        private void FormatBroadCastMessage(FBHeader header, int sessionID)
        {
            int spacerLength;
            Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
            Console.Write("[BRDCAST] ");
            spacerLength = header.type.ToString().Length;
            spacerLength = 20 - spacerLength;
            if (spacerLength < 0)
            {
                spacerLength = 0;
            }
            Console.Write("[");
            Console.Write(header.type);
            for (int i = 0; i < spacerLength; i++)
            {
                Console.Write(" ");
            }
            Console.Write("] ");
            if (header.state == FBMessageState.Fail)
            {
                Console.Write("[STATE:  " + header.state + " ] ");
            }
            else
            {
                Console.Write("[STATE:" + header.state + "] ");
            }
            spacerLength = header.length.ToString().Length;
            spacerLength = 5 - spacerLength;
            if (spacerLength < 0)
            {
                spacerLength = 0;
            }
            Console.Write("[BODY:");
            for (int i = 0; i < spacerLength; i++)
            {
                Console.Write(" ");
            }
            Console.Write(header.length + "] ");
            Console.Write("[FROM: Client " + sessionId + "] ");
            //if (header.type == CFMessageType.Health_Check)
            //{
            //    Console.Write("[COUNT: " + healthCheckCount + "] ");
            //}
            Console.WriteLine();
        }
    }

}
