using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;

namespace LoginServer
{
    class SessionManager
    {
        private IDictionary<int, Session> connectedSessions;
        private Queue<Session> sessionPool;
        private Queue<int> idCount;
        static private SessionManager instance = null;
        private int servicePort;
        private int maxSessionNum;
        private Session backEndSession;                     //Keep a special hold on the single back-end connection
        
        private SessionManager()
        {
            connectedSessions = new Dictionary<int, Session>();

            idCount = new Queue<int>();
            idCount.Enqueue(0);

        }

        public void Init(int maxSessionNum, int port)
        {
            sessionPool = new Queue<Session>(maxSessionNum);
            servicePort = port;
            this.maxSessionNum = maxSessionNum;

            //set session pool
            for (int i = 0; i < maxSessionNum; i++)
            {
                sessionPool.Enqueue(new Session());
            }
        }

        public void InitServerProtocol (Protocol protocol)
        {
            Session.InitSessionStatic(protocol);
        }

        public void Reset()
        {
            lock(connectedSessions)
            {
                lock(sessionPool)
                {
                    lock(idCount)
                    {
                        List<Session> sessionToRemove = new List<Session>();

                        foreach (KeyValuePair<int, Session> item in connectedSessions)
                        {
                            Session session = item.Value;
                            sessionToRemove.Add(session);
                        }

                        foreach (Session session in sessionToRemove)
                        {
                            RemoveSession(session);
                        }

                        idCount = new Queue<int>();
                        idCount.Enqueue(0);

                        Console.WriteLine("Session Reset");
                        Console.WriteLine("Left Sessions: " + sessionPool.Count);
                    }

                }
            }
        }

        public int GetServicePort()
        {
            return servicePort;
        }

        static public SessionManager GetInstance()
        {
            if(instance == null)
            {
                instance = new SessionManager();
            }

            return instance;
        }

        public Session GetSession(int sessionId)
        {
            if (!connectedSessions.ContainsKey(sessionId))
            {
                return null;
            }

            return connectedSessions[sessionId];
        }

        public List<Session> GetTimedoutSessions()
        {
            List<Session> timedOutSessions = new List<Session>();
            IDictionary<int, Session> temp;
            lock (connectedSessions)
            {
                temp = new Dictionary<int, Session>(connectedSessions);
            }

            foreach (KeyValuePair<int,Session> item in temp)
            {
                Session session = item.Value;
                if (session.isHealthCheckSent)
                {
                    if ((DateTime.Now - session.lastStartTime).TotalSeconds >= 5)
                    {
                        if (session.healthCheckCount > 3)
                        {
                            timedOutSessions.Add(session);
                        }
                        else
                        {
                            session.healthCheckCount++;
                            session.ResetStartTime();
                        }
                    }
                }
                else
                {
                    if ((DateTime.Now - session.lastStartTime).TotalSeconds >= 30)
                    {
                        session.healthCheckCount++;
                        timedOutSessions.Add(session);
                    }
                }
            }

            return timedOutSessions;
        }

        public List<Session> GetReadableSessions()
        {
            List<Session> readableSessions = new List<Session>();
            List<Socket> sockets = new List<Socket>();
            lock (connectedSessions)
            {
                foreach (KeyValuePair<int, Session> item in connectedSessions)
                {
                    sockets.Add(item.Value.socket);
                }
            }

            try
            {
                Socket.Select(sockets, null, null, 1000000); // wait until something comes..
            }
            catch (SocketException)
            {
                Console.WriteLine("Socket Already Closed");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            if (sockets.Count > 0)
            {
                lock (connectedSessions)
                {
                    foreach (KeyValuePair<int, Session> item in connectedSessions)
                    {
                        Session session = item.Value;
                        Socket socket = session.socket;

                        if (socket.Poll(10, SelectMode.SelectRead))
                        {
                            readableSessions.Add(session);
                        }
                    }
                }
            }

            return readableSessions;
        }

        public void RemoveClosedSessions()
        {
            List<Session> sessionToRemove = new List<Session>();
            
            lock (connectedSessions)
            {
                foreach (KeyValuePair<int, Session> item in connectedSessions)
                {
                    Session session = item.Value;
                    if(!session.isConnected)
                    {
                        sessionToRemove.Add(session);
                    }
                }
            }

            foreach (Session session in sessionToRemove)
            {
                RemoveSession(session);
            }

        }

        public Session MakeNewSession(Socket socket, bool isBackEndSession)
        {
            Session newSession = null;
            lock(connectedSessions)
            {
                lock (sessionPool)
                {
                    if(sessionPool.Count > 0)
                        newSession = sessionPool.Dequeue();
                }
            }

            if(newSession == null)
            {
                Console.WriteLine("Session pool is empty!");
                return null;
            }

            if (isBackEndSession)
            {
                newSession.InitBE(socket);
            } else
            {
                newSession.Init(socket, backEndSession);
            }

            lock (connectedSessions)
            {
                int sessionId = idCount.Dequeue();

                if (!connectedSessions.ContainsKey(sessionId + 1) && !idCount.Contains(sessionId + 1))
                {
                    lock(idCount)
                    {
                        idCount.Enqueue(sessionId + 1);
                    }
                }

                newSession.sessionId = sessionId;
                newSession.isConnected = true;


                connectedSessions.Add(newSession.sessionId, newSession);
            }

            if (isBackEndSession)
            {
                backEndSession = newSession;
            }

            //Adding an async message receival here
            newSession.StartAsyncHeaderReceive();

            return newSession;
        }




        private void RemoveSession(Session session)
        {
            lock(connectedSessions)
            {
                if(session != null && connectedSessions.ContainsKey(session.sessionId))
                {
                    Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                    Console.WriteLine(new string(session.id) +"(" + session.sessionId + ", " + session.ip + ") has exited");

                    connectedSessions.Remove(session.sessionId);

                    idCount.Enqueue(session.sessionId);
                    session.sessionId = -1;
                    try
                    {
                        if (session.socket != null)
                        {
                            session.socket.Shutdown(SocketShutdown.Both);
                            session.socket.Close();
                        }
                    }
                    catch (SocketException e)
                    {
                        Console.WriteLine("Exception throw during session.socket.Shutdown/Close.");
                        Console.WriteLine("\tAll: " + e.GetType().ToString());
                        Console.WriteLine("\tSource: " + e.Source.ToString());
                        Console.WriteLine("\tMessage: " + e.Message.ToString());
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Exception throw during session.socket.Shutdown/Close.");
                        Console.WriteLine("\tAll: " + e.GetType().ToString());
                        Console.WriteLine("\tSource: " + e.Source.ToString());
                        Console.WriteLine("\tMessage: " + e.Message.ToString());
                    }
                    sessionPool.Enqueue(session);
                }
                else
                {
                    Console.WriteLine("session " + session?.sessionId + " doesn't exist");
                }
            }
        }

        static public void ShutDown()
        {
            instance = null;
        }
    }
}
