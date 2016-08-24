using System.Runtime.InteropServices;

struct FBLoginRequestBody
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
    public char[] id;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
    public char[] password;
}
