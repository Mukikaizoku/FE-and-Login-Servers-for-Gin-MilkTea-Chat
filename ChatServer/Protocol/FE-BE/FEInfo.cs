using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LunkerRedis.src.Frame.FE_BE
{
    public class FEInfo
    {
        private string name;
        private string ip;
        private int port;
        private short type;


        public FEInfo() { }

        public FEInfo(string name, string ip, int port)
        {
            this.name = name;
            this.ip = ip;
            this.port = port;

        }

        public string Name
        {
            get { return this.name; }
            set { this.name = value; }
        }

        public string Ip
        {
            get { return this.ip; }
            set { this.ip = value; }
        }
        public int Port
        {
            get { return this.port; }
            set { this.port = value; }
        }
        public short Type
        {
            get { return this.type; }
            set { this.type = value; }
        }
    }
}
