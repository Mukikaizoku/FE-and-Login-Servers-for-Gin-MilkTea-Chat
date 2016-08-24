using System.Runtime.InteropServices;
using System;

namespace LoginServer
{
    struct CFLoginRequestBody
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        public char[] id;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public char[] password;

        public CFLoginRequestBody(char[] id, char[] password)
        {
            this.id = new char[12];
            this.password = new char[16];
            Array.Copy(id, this.id, id.Length);
            Array.Copy(password, this.password, password.Length);
        }
    }
}