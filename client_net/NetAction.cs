using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace client_net
{
    public class NetAction
    {
        //常量部分
        private static string IP = "127.0.0.1";//服务器公网IP地址
        private static int Port = 8885;//服务器开启的对应监听端口号
        private Socket Connectsocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);//设置一下请求连接的socket属性
        private int sendtype;
        public int Sendtype { get => sendtype; set => sendtype = value; }//封装一个string类型的变量作为提示服务器端执行何操作


        //方法部分
        private void SendString(Socket s, string str)
        {
            int i = str.Length;
            if (i == 0) return;
            else i *= 2;
            byte[] datasize = new byte[4];
            datasize = BitConverter.GetBytes(i);
            byte[] sendbytes = Encoding.Unicode.GetBytes(str);
            try
            {
                NetworkStream netstream = new NetworkStream(s);
                netstream.WriteTimeout = 10000;
                netstream.Write(datasize, 0, 4);
                netstream.Write(sendbytes, 0, sendbytes.Length);
                netstream.Flush();
            }
            catch { }
        }//封装好的发送字符串的方法，可以保证逐句发送且完整发送

        private string ReceiveString(Socket s)
        {
            string result = "";
            try
            {
                NetworkStream netstream = new NetworkStream(s);
                netstream.ReadTimeout = 10000;
                byte[] datasize = new byte[4];
                netstream.Read(datasize, 0, 4);
                int size = BitConverter.ToInt32(datasize, 0);
                byte[] message = new byte[size];
                int dataleft = size;
                int start = 0;
                while (dataleft > 0)
                {
                    int recv = netstream.Read(message, start, dataleft);
                    start += recv;
                    dataleft -= recv;
                }
                result = Encoding.Unicode.GetString(message);
            }
            catch { }
            return result;
        }//接收一句字符串的方法

        private bool IsConnected(Socket a)//该方法用于判断socket是否处于连接状态
        {
                bool blockingState = a.Blocking;
                try
                {
                    byte[] tmp = new byte[1];
                    a.Blocking = false;
                    a.Send(tmp, 0, 0);
                    return true;
                }
                catch (SocketException e)
                {
                    // 产生 10035 == WSAEWOULDBLOCK 错误，说明被阻止了，但是还是连接的
                    if (e.NativeErrorCode.Equals(10035))
                        return true;
                    else
                        return false;
                }
                finally
                {
                    a.Blocking = blockingState;    // 恢复状态
                }
            }

        public int Connect()//与服务器创建连接，成功返回0，失败返回1
        {
            IPAddress ip = IPAddress.Parse(IP);
            try
            {
                Connectsocket.Connect(new IPEndPoint(ip, Port));
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }
            if (IsConnected(Connectsocket) == true)
                return 0;
            else
                return 1;
        }

        public int DisConnect()//操作类型3：与服务器断开连接，成功返回0，失败返回1
        {
            int result = 1;
            if (IsConnected(Connectsocket) == true)//首先会判断连接是否存在，防止误操作,若连接存在，则断开连接释放内存
            {
                try
                {
                    Sendtype = 3;
                    byte[] type = new byte[4];
                    type = BitConverter.GetBytes(Sendtype);
                    Connectsocket.Send(type);
                    Connectsocket.Shutdown(SocketShutdown.Both); Connectsocket.Close();
                    return 0;
                }
                catch
                {}
            }
            return result;
        }
       
        public int Login(string username,string password)
        {
            int result=0;//如果返回值是0则说明连接出现问题
            byte[] callback = new byte[4];
            int returnmsg = 0;
            if (IsConnected(Connectsocket)==true)
            {
                try
                {
                    Sendtype =1;
                    Connectsocket.Send(BitConverter.GetBytes(Sendtype));
                    Connectsocket.Receive(callback);
                    returnmsg = BitConverter.ToInt32(callback, 0);//在确认服务器收到了操作种类信息后再传值
                    if (returnmsg == 1)
                    {
                        SendString(Connectsocket, username);
                        Console.WriteLine("已发送用户名：{0}",username);
                        SendString(Connectsocket, password);
                        Console.WriteLine("已发送密码：{0}",password);
                        Connectsocket.Receive(callback);
                        result = BitConverter.ToInt32(callback, 0);
                    }
                }
                catch(Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            return result;

        }
        //操作类型1：登录请求，返回值为登陆结果，0为连接问题，1为登陆成功，2为用户名或密码错误

        public int Register(string username, string password)
        {
            int result = 0;//如果返回值是0则说明连接出现问题
            byte[] callback = new byte[4];
            int returnmsg = 0;
            if (IsConnected(Connectsocket) == true)
            {
                try
                {
                    Sendtype = 2;
                    Connectsocket.Send(BitConverter.GetBytes(Sendtype));
                    Connectsocket.Receive(callback);
                    returnmsg = BitConverter.ToInt32(callback, 0);//在确认服务器收到了操作种类信息后再传值
                    if(returnmsg==1)
                    {
                        SendString(Connectsocket, username);
                        Console.WriteLine("已发送用户名：{0}",username);
                        SendString(Connectsocket, password);
                        Console.WriteLine("已发送密码：{0}",password);
                        Connectsocket.Receive(callback);
                        result = BitConverter.ToInt32(callback, 0);
                    }
                }
                catch(Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            return result;
        }
        //操作类型2：注册请求，返回值为注册结果，0为连接问题，1为登陆成功，2为数据库创建失败，3为用户已存在

        public string IfCalibrate(string username)
        {
            string result = "0";//如果返回值是"0"则说明连接出现问题
            byte[] callback = new byte[4];
            int returnmsg = 0;
            if (IsConnected(Connectsocket) == true)
            {
                try
                {
                    Sendtype = 4;
                    Connectsocket.Send(BitConverter.GetBytes(Sendtype));
                    Connectsocket.Receive(callback);
                    returnmsg = BitConverter.ToInt32(callback, 0);//在确认服务器收到了操作种类信息后再传值
                    if (returnmsg == 1)
                    {
                        SendString(Connectsocket, username);
                        Console.WriteLine("已发送用户名：{0}", username);
                       result = ReceiveString(Connectsocket);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            return result;
        }
        //操作类型4：查询用户的校准数据，无数据返回"-1*-1*-1*-1*-1*-1"，有数据则返回数据

        public int Calibrate(string username,string data)
        {
            int result = 0;//如果返回值是0则说明连接出现问题
            byte[] callback = new byte[4];
            int returnmsg = 0;
            if (IsConnected(Connectsocket) == true)
            {
                try
                {
                    Sendtype = 5;
                    Connectsocket.Send(BitConverter.GetBytes(Sendtype));
                    Connectsocket.Receive(callback);
                    returnmsg = BitConverter.ToInt32(callback, 0);//在确认服务器收到了操作种类信息后再传值
                    if (returnmsg == 1)
                    {
                        SendString(Connectsocket, username);
                        Console.WriteLine("已发送用户名：{0}", username);
                        SendString(Connectsocket, data);
                        Console.WriteLine("已发送校准数据：{0}", data);
                        Connectsocket.Receive(callback);
                        result = BitConverter.ToInt32(callback, 0);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            return result;
        }
        //操作类型5：上传用户的校准数据，0为连接问题，1为成功，2为失败
    }
}
