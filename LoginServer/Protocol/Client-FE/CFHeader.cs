using System.Runtime.InteropServices;

namespace LoginServer
{
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    struct CFHeader
    {
        public CFMessageType type;
        public CFMessageState state;
        public int length;

        public CFHeader(CFMessageType type, CFMessageState state, int length)
        {
            this.type = type;
            this.state = state;
            this.length = length;
        }

    }


    enum CFMessageType : short
    {
        Id_Dup = 110,
        Signup = 120,
        ChangePassword = 130,
        DeleteId = 140,

        LogIn = 210,
        LogOut = 220,

        Room_Create = 310,
        Room_Leave = 320,
        Room_Join = 330,
        Room_List = 340,
        ConnectionPass = 350,

        Chat_MSG_From_Client = 410,
        Chat_MSG_Broadcast = 420,

        Health_Check = 510
    };

    public enum CFMessageState : short
    {
        REQUEST = 100,
        SUCCESS = 200,
        FAIL = 400
    }
}
