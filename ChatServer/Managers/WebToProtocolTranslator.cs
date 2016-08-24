using System;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace ChatServer.Managers
{
    /// <summary>
    /// The WebToProtocolTranslator service provides access to translating web-socket messages to be processed by our defined chat protocol.
    /// </summary>
    class WebToProtocolTranslator : WebSocketBehavior
    {
        //Fields
        Session session;

        /// <summary>
        /// The OnOpen event creates a new client session.
        /// </summary>
        protected override void OnOpen()
        {
            session = SessionManager.GetInstance().MakeNewWebSession(this);                                                         //Create a new session for the client
            Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
            Console.WriteLine("Client (" + session.sessionId + ", " + session.ip + ")" + " is Connected via WebSocket.");           //Report to the console
            //_name = session.sessionId.ToString();
        }

        /// <summary>
        /// The OnClose event triggers upon web-socket session closure.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClose(CloseEventArgs e)
        {
            Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
            Console.WriteLine("Client (" + session.sessionId + ", " + session.ip + ")" + " WebSocket connection has closed.");       //Report to the console
            session.ConnectionCloseLogout();
           // Sessions.CloseSession(_name);
            
            //Sessions.Broadcast(String.Format("{0} got logged off...", _name));
        }

        /// <summary>
        /// The OnMessage event sends a message to the session web socket message processor method.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMessage(MessageEventArgs e)
        {
            session.ProcessWebSocketMessage(e.RawData);
        }
        
        /// <summary>
        /// The SendData event sends data back to a client in an active session.
        /// </summary>
        /// <param name="data"></param>
        public void SendData (byte[] data)
        {
            if (State == WebSocketState.Closed || State == WebSocketState.Closing || !session.IsLogedIn())
            {
                Console.WriteLine("Client (" + session.sessionId + ", " + session.ip.ToString() + ") WebSocket session already closed.");
                return;
            }
            try
            {
                Send(data);
            }
            catch (Exception e)
            {
                Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                Console.WriteLine(e.GetType() + " from Client (" + session.sessionId + ", " + session.ip.ToString() + ")");
                Console.WriteLine("\t\tMessage: " + e.Message);
            }
        }

        /// <summary>
        /// The OnError event triggers on a session's WebSocket error.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnError(ErrorEventArgs e)
        {
            Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
            Console.WriteLine(e.Exception + "Error from Client (" + session.sessionId + ", " + session.ip.ToString() + ")");
            Console.WriteLine("\t\tMessage: " + e.Message);
        }
    }
}
