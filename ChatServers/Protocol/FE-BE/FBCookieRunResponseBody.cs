using System.Runtime.InteropServices;

namespace ChatServer
{
    struct FBCookieRunResponseBody
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        public char[] id;
    }
}
