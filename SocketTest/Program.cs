using System;
using System.Net;
using System.Net.Sockets;


namespace SocketTest
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            Serv serv = new Serv();
            serv.Start("127.0.0.1",1234);


            while (true)
            {

            }
            
        }

        
    }
}
