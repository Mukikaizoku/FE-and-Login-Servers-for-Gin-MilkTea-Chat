using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct FBHeader
{
    public FBMessageType type;
    public FBMessageState state;
    public int length;
    public int sessionId;
}

public enum FBMessageType : short
{
    Id_Dup = 110,               //Depreciated
    Signup = 120,               //Depreciated
    ChangePassword = 130,
    DeleteId = 140,

    Login = 210,                //Depreciated
    Logout = 220,               //Depreciated

    Room_Create = 310,          //Request to create a room in the DB
    Room_Leave = 320,           //Request to remove a user from a room in the DB
    Room_Join = 330,            //Request to add a user from a room in the DB
    Room_List = 340,            //Request to the current list of rooms in the DB
    Room_Delete = 350,          //Request to delete a room in the DB

    Chat_Count = 410,           //Send a request to update the chat message count for a particular user

    Health_Check = 510,
    Cookie_Run = 520,

    Connection_Info = 610
};

public enum Protocol : short
{
    Tcp = 1,
    Web = 3
}

public enum FBMessageState : short
{
    Request = 100,
    Success = 200,
    Fail = 400
}


