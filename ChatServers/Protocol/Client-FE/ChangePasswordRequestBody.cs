using System.Runtime.InteropServices;
using System;

namespace ChatServer
{
    class ChangePasswordRequestBody
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        char[] id;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        char[] currentPassword;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        char[] newPassword;

        public ChangePasswordRequestBody(char[] id, char[] currentPassword, char[] newPassword)
        {
            this.id = new char[12];
            this.currentPassword = new char[16];
            this.newPassword = new char[16];
            Array.Copy(id, this.id, id.Length);
            Array.Copy(currentPassword, this.currentPassword, currentPassword.Length);
            Array.Copy(newPassword, this.newPassword, newPassword.Length);
        }
    }
}
