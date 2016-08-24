using System.Runtime.InteropServices;
using System;

namespace ChatServer
{
    struct ConnectionPassRequestBody
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        public char[] id;
        public int cookie;

        public ConnectionPassRequestBody(char[] id, int cookie)
        {
            this.id = new char[12];
            Array.Copy(id, this.id, id.Length);
            this.cookie = cookie;
        }
    }
}
