using System;
using System.Runtime.InteropServices;

namespace LoginServer
{
    class CFRemoveAcountRequestBody
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        char[] id;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        char[] password;

        public CFRemoveAcountRequestBody(char[] id, char[] password)
        {
            this.id = new char[12];
            this.password = new char[16];
            Array.Copy(id, this.id, id.Length);
            Array.Copy(password, this.password, password.Length);
        }
    }
}
