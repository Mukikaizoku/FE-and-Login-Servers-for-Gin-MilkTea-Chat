using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace LoginServer
{
    /// <summary>
    /// The Session class holds the information for one connection/client (including the connection with the back-end server). Its methods serve to receive and send messages.
    /// </summary>
    class Session
    {
        //Properties
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

        public static void InitSessionStatic (Protocol protocol)
        {
            serverType = protocol;
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
                if (isBackEndSession)
                {
                    Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                    Console.WriteLine("*****Connection with back-end server Suddenly Abrupted******");
                    Console.WriteLine("*****Initializing back-end reconnect. . .******");
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
                            Console.WriteLine("\t>>Reinitializing server functions. . .");
                            isBackEndConnected = true;
                        }
                    }
                    StartAsyncHeaderReceive();
                    return;
                }
                Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                Console.WriteLine("*****Connection from " + sessionId + " Suddenly Abrupted******");
                socket.Close();
                return;
            }

            if (isBackEndSession)
            {
                headerBE = (FBHeader)Serializer.ByteToStructure(headerByte, typeof(FBHeader));
                Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                Console.WriteLine("Message (" + bytesReceived + " bytes) Received from Back-End.");
                Console.Write("\t\t Type: " + headerBE.type);
                Console.Write("\t\t State: " + headerBE.state);
                Console.WriteLine("\t\t Length: " + headerBE.length);
                bodyLength = headerBE.length;
            }
            else
            {
                header = (CFHeader)Serializer.ByteToStructure(headerByte, typeof(CFHeader));
                Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                Console.WriteLine("Message (" + bytesReceived + " bytes) Received from Client " + sessionId);
                Console.Write("\t\t Type: " + header.type);
                Console.Write("\t\t State: " + header.state);
                Console.WriteLine("\t\t Length: " + header.length);
                bodyLength = header.length;

                if (header.type == CFMessageType.ConnectionPass && header.state == CFMessageState.SUCCESS)
                {
                    Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                    Console.WriteLine("Connection Passing succeeded by Client " + sessionId + "!");
                    return;
                }
            }

            if (undefinedMessageCounter > 2)
            {
                Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                Console.WriteLine("*****Undefined Message Spamming from a Connection " + sessionId + " ******");
                Console.WriteLine("\t *****Messages received from a Connection " + sessionId + " Halted ******");
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
                Console.WriteLine();
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

            Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
            Console.WriteLine(">>Received CF " + header.type + " from " + sessionId);

            switch (type)
            {
                case CFMessageType.Signup:
                    undefinedMessageCounter = 0;
                    SignUpMessage(header, body);
                    break;

                case CFMessageType.Id_Dup:
                case CFMessageType.LogIn:
                case CFMessageType.DeleteId:
                case CFMessageType.ChangePassword:
                    undefinedMessageCounter = 0;
                    LoginMessage(header, body);
                    break;

                //Inappropriate Message Types --- These message types are only for the FE Server
                case CFMessageType.LogOut:
                case CFMessageType.Room_Create:
                case CFMessageType.Room_Join:
                case CFMessageType.Room_Leave:
                case CFMessageType.Room_List:
                case CFMessageType.Chat_MSG_From_Client:
                case CFMessageType.Chat_MSG_Broadcast:
                    undefinedMessageCounter = 0;
                    InvalidMessage(header, body);
                    break;

                case CFMessageType.Health_Check:
                    undefinedMessageCounter = 0;
                    ResetStartTime();
                    healthCheckCount = 0;
                    isHealthCheckSent = false;
                    break;

                default:
                    undefinedMessageCounter++;
                    Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                    Console.Write(">>Undefined Message Type from " + sessionId);
                    Console.WriteLine("\t (Type: " + header.type.ToString() + ")");
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

            Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
            Console.WriteLine("**Received FB " + header.type + " from Gin back-end");

            switch (type)
            {
                case FBMessageType.Signup:
                    undefinedMessageCounter = 0;
                    SignupMessage(header, body);
                    break;
                case FBMessageType.Id_Dup:
                case FBMessageType.Login:
                case FBMessageType.Logout:
                case FBMessageType.DeleteId:
                case FBMessageType.ChangePassword:
                    undefinedMessageCounter = 0;
                    LoginMessage(header, body);
                    break;
                case FBMessageType.Room_Create:
                case FBMessageType.Room_Join:
                case FBMessageType.Room_Leave:
                case FBMessageType.Room_List:
                case FBMessageType.Room_Delete:
                    undefinedMessageCounter = 0;
                    InvalidMessage(header, body);
                    break;

                case FBMessageType.Health_Check:
                    undefinedMessageCounter = 0;
                    backEndSession.ResetStartTime();
                    backEndSession.isHealthCheckSent = false;
                    backEndSession.healthCheckCount = 0;
                    break;

                case FBMessageType.Connection_Info:
                    undefinedMessageCounter = 0;
                    ConnectionInfo();
                    break;
                default:
                    undefinedMessageCounter++;
                    Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                    Console.WriteLine("**Undefined Message Type from Gin back-end");
                    Console.WriteLine("\t (Type: " + header.type.ToString() + ")");
                    break;
            }
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
            Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
            Console.WriteLine("CHECK Session ID " + requestHeader.sessionId.ToString() + " to Gin Back-End Server. . .");

            byte[] headerByte = Serializer.StructureToByte(requestHeader);
            Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
            Console.WriteLine(">>Forwarding " + requestHeader.type.ToString() + " to Gin Back-End Server. . .");
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
                Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                Console.WriteLine("!!Client " + sessionId + " signed up successfully!");
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
            Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
            Console.WriteLine(">>Forwarding " + requestHeader.type + " to Client " + clientSession.sessionId + ". . .");
            
            SendData(clientSession, headerByte);
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
                case CFMessageType.DeleteId:
                    {
                        requestHeader.type = FBMessageType.DeleteId;
                        break;
                    }
                case CFMessageType.ChangePassword: 
                    {
                        requestHeader.type = FBMessageType.ChangePassword;
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
            Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
            Console.WriteLine(">>Forwarding " + requestHeader.type.ToString() + " to Gin Back-End Server. . .");
            SendData(backEndSession, headerByte);

            //Body Forwarding
            byte[] bodyByte = null;

            switch (header.type)
            {
                case CFMessageType.DeleteId:
                case CFMessageType.ChangePassword:
                    {
                        bodyByte = body;
                        break;
                    }
                default:
                    CFLoginRequestBody requestFromClient = (CFLoginRequestBody)Serializer.ByteToStructure(body, typeof(CFLoginRequestBody));

                    FBLoginRequestBody requestBody = new FBLoginRequestBody();
                    requestBody.id = requestFromClient.id;
                    requestBody.password = requestFromClient.password;

                    bodyByte = Serializer.StructureToByte(requestBody);
                    break;
            }

            SendData(backEndSession, bodyByte);
        }
        private void LoginMessage(FBHeader header, byte[] body)
        {
            Session clientSession = SessionManager.GetInstance().GetSession(header.sessionId);

            CFHeader responseHeader = new CFHeader();
            CFLoginResponseBody responseBody = new CFLoginResponseBody();
            responseHeader.length = 0;

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
                            Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                            Console.WriteLine("!!Id " + sessionId + " not duplicated");
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
                            Console.WriteLine(">>" + new string (responseFromBackEnd.id) + " login-in credentials authorized.");
                            //Transfer the back-end's response data to a body that the client can read
                            responseHeader.length = Marshal.SizeOf<CFLoginResponseBody>();
                            responseBody.ip = responseFromBackEnd.ip;
                            responseBody.port = responseFromBackEnd.port;
                            responseBody.protocolType = responseFromBackEnd.protocolType;
                            responseBody.cookie = responseFromBackEnd.cookie;


                            Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                            Console.WriteLine(">>Forwarding " + responseHeader.type.ToString() + " to Client " + clientSession.sessionId + ". . .");
                            Console.Write("\t\t>>IP " + new string (responseBody.ip));
                            Console.WriteLine("\t>>Port " + responseBody.port.ToString());
                            Console.Write("\t\t>>Proto " + responseBody.protocolType.ToString());
                            Console.WriteLine("\t\t>>cookie " + responseBody.cookie.ToString());
                        }
                        else if (header.state == FBMessageState.Fail)
                        {
                            Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                            Console.WriteLine("!!Client " + sessionId + "'s Login Fail");
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

                            Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                            Console.WriteLine("!!Client " + sessionId + " is already Logged Out");
                            return;
                        }
                        if (header.state == FBMessageState.Success)
                        {
                            Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                            Console.WriteLine("!!Client " + new string(clientSession.id) + " is logged out");
                            clientSession.LogOut();
                            if (clientSession.IsInRoom())
                            {
                                RoomManager.GetInstance().RemoveUserInRoom(clientSession);
                            }
                        }
                        responseHeader.type = CFMessageType.LogOut;
                        break;
                    }
                case FBMessageType.DeleteId:
                    {
                        responseHeader.type = CFMessageType.DeleteId;
                        if (header.state == FBMessageState.Success)
                        {
                            Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                            Console.WriteLine("!!Client " + sessionId + "'s Delete Account Request Success");
                        } else
                        {
                            Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                            Console.WriteLine("!!Client " + sessionId + "'s Delete Account Request Fail");
                        }
                        break;
                    }
                case FBMessageType.ChangePassword:
                    {
                        responseHeader.type = CFMessageType.ChangePassword;
                        if (header.state == FBMessageState.Success)
                        {
                            Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                            Console.WriteLine("!!Client " + sessionId + "'s Change Password Request Success");
                        }
                        else
                        {
                            Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                            Console.WriteLine("!!Client " + sessionId + "'s Change Password Request Fail");
                        }
                        break;
                    }
                default:
                    Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                    Console.WriteLine("**Undefined Login Message Type from Client " + clientSession.sessionId);
                    Console.WriteLine();
                    return;

            }


            byte[] headerByte = Serializer.StructureToByte(responseHeader);
            byte[] bodyByte = Serializer.StructureToByte(responseBody);

            headerByte = CombineMessagesForClient(headerByte, bodyByte, null);
            SendData(clientSession, headerByte);
        }

        private void InvalidMessage (CFHeader header, byte[] body)
        {
            CFHeader responseHeader = new CFHeader();
            responseHeader.type = header.type;
            responseHeader.state = CFMessageState.FAIL;

            responseHeader.length = Marshal.SizeOf<CFRoomResponseBody>();

            byte[] failHeaderByte = Serializer.StructureToByte(responseHeader);

            CFRoomResponseBody failResponseBody = new CFRoomResponseBody();

            byte[] failBodyByte = Serializer.StructureToByte(failResponseBody);

            Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
            Console.WriteLine("**Responding " + responseHeader.type.ToString() + " fail to Client " + sessionId + ". . .");
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

            Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
            Console.WriteLine("**Responding " + responseHeader.type.ToString() + " fail to Client " + sessionId + ". . .");
            failHeaderByte = CombineMessagesForClient(failHeaderByte, failBodyByte, null);
            SendData(this, failHeaderByte);
            return;
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
            info.protocol = Protocol.Tcp;

            byte[] body = Serializer.StructureToByte(info);
            header.length = body.Length;
            header.sessionId = -1;

            byte[] headerByte = Serializer.StructureToByte(header);
            backEndSession.socket.Send(headerByte);

            backEndSession.socket.Send(body);
        }

        private bool SendData(Session session, byte[] buf)
        {
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
                Console.WriteLine(e.ToString());
                session.LogOut();
                return false;
            }
            return true;
        }
        
        public void ProcessTimeoutSession()
        {
            if (socket == backEndSession.socket)
            {
                if (!backEndSession.isHealthCheckSent)
                {
                    FBHeader header = new FBHeader();

                    header.type = FBMessageType.Health_Check;
                    header.state = FBMessageState.Request;
                    header.length = 0;
                    header.sessionId = backEndSession.sessionId;

                    byte[] headerByte = Serializer.StructureToByte(header);
                    Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                    Console.WriteLine(">>Sending " + header.type.ToString() + " to Gin Back-End Server. . .");
                    SendData(backEndSession, headerByte);
                    backEndSession.healthCheckCount = 0;
                    backEndSession.ResetStartTime();
                }
                else
                {
                    backEndSession.isConnected = false;
                }
            } else
            {
                if (!isHealthCheckSent)
                {
                    CFHeader header = new CFHeader();

                    header.type = CFMessageType.Health_Check;
                    header.state = CFMessageState.REQUEST;
                    header.length = 0;

                    byte[] headerByte = Serializer.StructureToByte(header);
                    Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                    Console.WriteLine(">>Sending " + header.type.ToString() + " to Client " + sessionId + ". . .");
                    SendData(this, headerByte);
                    ResetStartTime();
                    isHealthCheckSent = true;
                }
                else
                {
                    ConnectionCloseLogout();
                    isConnected = false;
                }
            }
            Console.WriteLine();
        }

        private void ConnectionCloseLogout()
        {
            CFHeader fakeHeader = new CFHeader();
            fakeHeader.type = CFMessageType.LogOut;
            fakeHeader.state = CFMessageState.REQUEST;
            fakeHeader.length = Marshal.SizeOf(typeof(CFLoginRequestBody));

            CFLoginRequestBody fakeBody = new CFLoginRequestBody();
            fakeBody.id = id;

            byte[] fakeBodyByte = Serializer.StructureToByte(fakeBody);

            LoginMessage(fakeHeader, fakeBodyByte);
        }

        private byte[] CombineMessagesForClient(byte[] one, byte[] two, byte[] three)
        {
            byte[] combinedByte = null;
            if (one == null)
            {
                return null;
            }
            else if (two == null)
            {
                return one;
            }
            else if (three == null)
            {
                combinedByte = new byte[one.Length + two.Length];
                Array.Copy(one, 0, combinedByte, 0, one.Length);
                Array.Copy(two, 0, combinedByte, one.Length, two.Length);
            }
            else
            {
                combinedByte = new byte[one.Length + two.Length + three.Length];
                Array.Copy(one, 0, combinedByte, 0, one.Length);
                Array.Copy(two, 0, combinedByte, one.Length, two.Length);
                Array.Copy(three, 0, combinedByte, two.Length, three.Length);
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
                Console.WriteLine("\tUnusually short WebSocket from Client " + sessionId);
                return;
            }
            
            Array.Copy(data, headerByte, headerSize);
            header = (CFHeader)Serializer.ByteToStructure(headerByte, typeof(CFHeader));
            Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
            Console.WriteLine("Web Socket Message Received from Client " + sessionId);
            Console.Write("\t\t Type: " + header.type);
            Console.Write("\t\t State: " + header.state);
            Console.WriteLine("\t\t Length: " + header.length);
            bodyLength = header.length;

            if (data.Length < headerSize + bodyLength)
            {
                Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                Console.WriteLine("Body data from WebSocket incomplete from Client " + sessionId);
                return;
            }
            Console.WriteLine();

            body = new byte[bodyLength];
            Array.Copy(data, headerSize, body, 0, bodyLength);

            ProcessMessage(header, body);
        }

        /******Web Socket Functions End******/
    }

}
