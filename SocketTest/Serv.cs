using System;
using System.Net;
using System.Net.Sockets;
namespace SocketTest
{
    public class Serv
    {

        public Socket listenfd;

        public Conn[] conns;

        public int maxConn = 50;
        /// <summary>
        /// 连接池索引获取
        /// </summary>
        /// <returns></returns>
        public int NewIndex()
        {
            if (conns == null) return -1;

            for (int i = 0; i < conns.Length; i++)
            {
                if (conns[i] == null)
                {
                    conns[i] = new Conn();
                    return i;
                }
                else if (conns[i].isUse == false) return i;
            }
            return -1;
        }

        public Serv()
        {
        }
        /// <summary>
        /// 服务器启动
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        public void Start(string host, int port)
        {
            conns = new Conn[maxConn];
            for (int i = 0; i < maxConn; i++)
            {
                conns[i] = new Conn();

            }

            listenfd = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            IPAddress ipAdr = IPAddress.Parse(host);
            IPEndPoint ipEp = new IPEndPoint(ipAdr,port);
            listenfd.Bind(ipEp);
            listenfd.Listen(maxConn);
            listenfd.BeginAccept(AcceptCb,null);

            Console.WriteLine("[服务器] 启动成功");
        }
        /// <summary>
        /// 连接应答回调
        /// </summary>
        /// <param name="ar"></param>
        private void AcceptCb(IAsyncResult ar)
        {
            try
            {
                Socket socket = listenfd.EndAccept(ar);
                int index = NewIndex();
                if (index < 0)
                {
                    socket.Close();
                    Console.Write("[警告]连接已满");


                }
                else
                {
                    Conn conn = conns[index];
                    conn.Init(socket);

                    string adr = conn.GetAdress();
                    Console.WriteLine("客户端连接[" + adr + "] conn池ID：" + index);
                    conn.socket.BeginReceive(conn.readBuff, conn.buffCount, conn.BuffRemain(), SocketFlags.None, ReceiveCb, conn);

                }

                listenfd.BeginAccept(AcceptCb, null);
            }
            catch (Exception e)
            {
                Console.WriteLine("AcceptCb失败："+e.Message);
            } 
        }
        /// <summary>
        /// 接收回调
        /// </summary>
        /// <param name="ar"></param>
        private void ReceiveCb(IAsyncResult ar)
        {
            Conn conn = (Conn)ar.AsyncState;

            try
            {
                int count = conn.socket.EndReceive(ar);
                if (count <= 0)
                {
                    Console.WriteLine("收到[" + conn.GetAdress() + "]断开连接");
                    conn.Close();
                    return;
                }
                //处理接收到的数据
                string str = System.Text.Encoding.UTF8.GetString(conn.readBuff, 0, count);

                Console.WriteLine("收到[" + conn.GetAdress() + "]数据：" + str);
                str = conn.GetAdress() + ":" + str;
                byte[] bytes = System.Text.Encoding.Default.GetBytes(str);

                //给所有客户端发送消息
                for (int i = 0; i < conns.Length; i++)
                {
                    if (conns[i] == null) continue;
                    if (!conns[i].isUse) continue;

                    Console.WriteLine("将消息传播给" + conns[i].GetAdress());

                    conns[i].socket.Send(bytes);
                }
                //开始继续接收
                conn.socket.BeginReceive(conn.readBuff, conn.buffCount, conn.BuffRemain(), SocketFlags.None, ReceiveCb, conn);
            }
            catch (Exception e)
            {
                Console.WriteLine("收到["+conn.GetAdress()+"]断开连接");
                conn.Close();
            }
        }
    }
}
