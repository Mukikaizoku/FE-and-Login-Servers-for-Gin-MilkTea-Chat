using System.Runtime.InteropServices;

// struct for Client ~ BE Header
[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct CBHeader
{
    public CBMessageType type; // message type
    public CBMessageState state; // message status
    public int length; // body length 

    public int Length
    {
        get { return this.length; }
        set { this.length = value; }
    }

    public CBMessageType Type
    {
        get { return this.type; }
        set { this.type = value; }
    }

    public CBMessageState State
    {
        get { return this.state; }
        set { this.state = value; }
    }
}
// 
public enum CBMessageType : short
{

    Total_Room_Count = 110, // type for request total room number in application
    FE_User_Status = 210, // type for request FE' user number
    Chat_Ranking = 310, // type for request chatting ranking 
    Login = 410,
    Logout = 420,
    Health_Check = 510
};

public enum CBMessageState : short
{
    Request = 100, // state that this message is 'Request'
    Success = 200, // state that this message is Response messgae with 'Success'
    Fail = 400 // state that this message is Response message with 'Fail'
}
