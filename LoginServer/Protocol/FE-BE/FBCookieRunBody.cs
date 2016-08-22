using System.Runtime.InteropServices;

namespace LoginServer
{
    struct FBCookieRunBody
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        public char[] id;
        public int cookie;
    }
}
