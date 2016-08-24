using System.Runtime.InteropServices;

struct SignRequestBody{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)] char[] id;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)] char[] password;
    bool isDummy;
}
