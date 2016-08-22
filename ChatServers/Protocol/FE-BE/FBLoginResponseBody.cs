
using System.Runtime.InteropServices;

public struct FBLoginResponseBody
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
    public char[] id;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)]
    public char[] ip;
    public int port;
    public Protocol protocolType;
    public int cookie;
}
