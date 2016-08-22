using System.Runtime.InteropServices;
namespace ChatServer
{
    struct CFLoginResponseBody
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)]
        public char[] ip;
        public int port;
        public Protocol protocolType;
        public int cookie;
    }
}