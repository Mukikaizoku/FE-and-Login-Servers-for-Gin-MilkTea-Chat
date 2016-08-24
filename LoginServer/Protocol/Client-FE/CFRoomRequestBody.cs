 using System.Runtime.InteropServices;


namespace LoginServer
{
    struct CFRoomRequestBody
    {
        int roomNo;
        public CFRoomRequestBody(int roomNo)
        {
            this.roomNo = roomNo;
        }
    }
}