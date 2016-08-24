// containing single rank information
// will be used as struct array to provide Ranking information
using System.Runtime.InteropServices;
public struct CBRanking
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
    public char[] id; // user id
    public int rank; // user's rnak 
    public int score; // user's score
}
