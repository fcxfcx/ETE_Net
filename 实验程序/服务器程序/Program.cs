using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using server_net;

namespace 服务器程序
{
    class Program
    {
        static void Main(string[] args)
        {
            NetAction newserver = new NetAction();
            newserver.WatchBegin();
        }
    }
}
