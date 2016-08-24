using System;
using System.Runtime.InteropServices;

namespace ChatServer
{
    struct CFChatResponseBody
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        public char[] id;
        public DateTime date;
        public int msgLen; //lenght of next body 

        public CFChatResponseBody(char[] id, DateTime date, int len)
        {
            this.id = new char[12];
            Array.Copy(id, this.id, id.Length);
            this.date = date;
            msgLen = len;
        }
    }
}
