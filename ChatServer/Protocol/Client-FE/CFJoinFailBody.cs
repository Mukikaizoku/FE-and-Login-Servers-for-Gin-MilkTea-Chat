
using System.Runtime.InteropServices;

public struct CFJoinFailBody
{
    [MarshalAs(UnmanagedType.LPArray, SizeConst = 15)]
    public char[] ip;
    public int port;
}
