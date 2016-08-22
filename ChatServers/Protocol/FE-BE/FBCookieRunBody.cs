using System.Runtime.InteropServices;

namespace ChatServer
{
    struct FBCookieRunBody
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        public char[] id;
        public int cookie;
    }
}
