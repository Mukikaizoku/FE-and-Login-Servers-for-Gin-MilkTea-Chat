
using System.Runtime.InteropServices;

public struct FBRoomRequestBody
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
    public char[] id;
    public int roomNo;
}
