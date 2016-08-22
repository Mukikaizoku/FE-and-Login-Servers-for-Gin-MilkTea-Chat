 using System.Runtime.InteropServices;
 struct FBConnectionInfoBody
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)]
    public char[] ip;
    public int port;
    public Protocol protocol;
}
