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
            session = SessionManager.GetInstance().MakeNewWebSession(this);                                     //Create a new session for the client
            Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
            Console.WriteLine("Client(" + session.sessionId + ", " + session.ip + ")" + " is Connected");       //Report to the console
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
            Send(data);
        }
    }
}
