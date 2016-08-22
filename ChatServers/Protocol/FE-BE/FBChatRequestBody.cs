
using System.Runtime.InteropServices;

public struct FBChatRequestBody
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
    public char[] id;
}

