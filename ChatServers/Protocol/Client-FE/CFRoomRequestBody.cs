 using System.Runtime.InteropServices;


namespace ChatServer
{
    struct CFRoomRequestBody
    {
        public int roomNo;
        public CFRoomRequestBody(int roomNo)
        {
            this.roomNo = roomNo;
        }
    }
}