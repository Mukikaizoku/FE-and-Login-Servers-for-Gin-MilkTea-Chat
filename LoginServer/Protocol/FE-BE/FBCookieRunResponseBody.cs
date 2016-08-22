using System.Runtime.InteropServices;

namespace LoginServer
{
    struct FBCookieRunResponseBody
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        public char[] id;
    }
}
