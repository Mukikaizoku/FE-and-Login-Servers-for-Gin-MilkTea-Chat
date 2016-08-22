// struct for monitoring FE 
// struct contains information for 1 FE 
// FE Monitoring information will be sent to Client as struct CBFEUserStatus array 
using System.Runtime.InteropServices;
public struct CBFEUserStatus
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)]
    public char[] ip;
    public int port;

    public int num;


}
