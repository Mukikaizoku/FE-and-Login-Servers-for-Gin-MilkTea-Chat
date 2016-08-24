using System.Runtime.InteropServices;
using System;

namespace LoginServer
{
    //[StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct ConnectionPassRequestBody
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        public char[] id;
        //[MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        //public byte[] cookie;
        public int cookie;

        public ConnectionPassRequestBody(char[] id, int cookie)
        {
            this.id = new char[12];
            Array.Copy(id, this.id, id.Length);
            this.cookie = cookie;
        }
    }
}
