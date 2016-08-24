using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatServer
{
    /// <summary>
    /// The RoomManager Class organizes the rooms within a particular server.
    /// </summary>
    class RoomManager
    {
        private class Room
        {
            public List<Session> chatters;
            public int roomNo;

            public Room(int roomNo)
            {
                this.roomNo = roomNo;
                chatters = new List<Session>();
            }
        }

        private static RoomManager instance = null;
        private IDictionary<int, Room> rooms;

        private RoomManager()
        {
            rooms = new Dictionary<int, Room>();
        }
        
        public static RoomManager GetInstance()
        {
            if(instance == null)
            {
                instance = new RoomManager();
            }

            return instance;
        }

        public void Reset()
        {
            rooms.Clear();
            Console.WriteLine("Rooms Reset");
            Console.WriteLine("Left Rooms: " + rooms.Count);
        }

        
        public void MakeNewRoom(int roomNo)
        {
            try
            {
                rooms.Add(roomNo, new Room(roomNo));
            }
            catch(ArgumentException)
            {
                Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                Console.WriteLine("Room " + roomNo + "already exists.");
                return;
            }
            catch (Exception)
            {
                Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                Console.WriteLine("Room " + roomNo + "already exists.");
                return;
            }
            Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
            Console.WriteLine("************************************** Room " + roomNo + " has been created **************************************");
        }

        public void RemoveRoom(int roomNo)
        {
            if(rooms.Remove(roomNo))
            {
                Console.WriteLine("Room " + roomNo + " is removed");
            }
            else
            {
                Console.WriteLine("Room " + roomNo + " doesn't exist");
            }
        }

        public List<Session> GetUsersInRoom(int roomNo)
        {
            Room room = rooms[roomNo];
            if(room == null)
            {
                Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
                Console.WriteLine("Room " + roomNo + " doesn't exits.");
            }

            return room.chatters;
        }

        public void AddUserInRoom(Session userSession, int roomNo)
        {
            Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
            Console.WriteLine("**************************************" + new string(userSession.id) + " entered Room " + roomNo + "**************************************");
            rooms[roomNo].chatters.Add(userSession);
            userSession.roomNo = roomNo;
        }


        public void RemoveUserInRoom(Session userSession)
        {
            Console.Write("[" + DateTime.Now.ToShortTimeString() + "] ");
            Console.WriteLine("**************************************" + new string(userSession.id) + " left room " + userSession.roomNo + "**************************************");
            rooms[userSession.roomNo].chatters.Remove(userSession);
            userSession.roomNo = -1;
        }

        static public void ShutDown()
        {
            instance = null;            
        }
    }
}
