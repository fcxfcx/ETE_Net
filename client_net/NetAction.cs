using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace client_net
{
    public class NetAction
    {
        //常量部分
        private const string IP = "59.110.167.50"; //服务器公网IP地址
        private const int Port = 8885; //服务器开启的对应监听端口号
        private readonly Socket _connectSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);//设置一下请求连接的socket属性
        private int _sendType;


        //方法部分
        private static void SendString(Socket s, string str)
        {
            var i = str.Length;
            if (i == 0) return;
            else i *= 2;
            var dataSize = BitConverter.GetBytes(i);
            var sendBytes = Encoding.Unicode.GetBytes(str);
            try
            {
                var netStream = new NetworkStream(s) {WriteTimeout = 10000};
                netStream.Write(dataSize, 0, 4);
                netStream.Write(sendBytes, 0, sendBytes.Length);
                netStream.Flush();
            }
            catch
            {
                // ignored
            }
        }//封装好的发送字符串的方法，可以保证逐句发送且完整发送

        private static string ReceiveString(Socket s)
        {
            var result = "";
            try
            {
                var netStream = new NetworkStream(s) {ReadTimeout = 10000};
                var dataSize = new byte[4];
                netStream.Read(dataSize, 0, 4);
                var size = BitConverter.ToInt32(dataSize, 0);
                var message = new byte[size];
                var dataLeft = size;
                var start = 0;
                while (dataLeft > 0)
                {
                    var receive = netStream.Read(message, start, dataLeft);
                    start += receive;
                    dataLeft -= receive;
                }
                result = Encoding.Unicode.GetString(message);
            }
            catch
            {
                // ignored
            }

            return result;
        }//接收一句字符串的方法

        private static bool IsConnected(Socket a)//该方法用于判断socket是否处于连接状态
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
                _connectSocket.Connect(new IPEndPoint(ip, Port));
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }
            if (IsConnected(_connectSocket) == true)
                return 0;
            else
                return 1;
        }

        public int DisConnect()//操作类型3：与服务器断开连接，成功返回0，失败返回1
        {
            int result = 1;
            if (IsConnected(_connectSocket) == true)//首先会判断连接是否存在，防止误操作,若连接存在，则断开连接释放内存
            {
                try
                {
                    _sendType = 3;
                    byte[] type = new byte[4];
                    type = BitConverter.GetBytes(_sendType);
                    _connectSocket.Send(type);
                    _connectSocket.Shutdown(SocketShutdown.Both); _connectSocket.Close();
                    return 0;
                }
                catch
                {}
            }
            return result;
        }
       
        public int Login(string username,string password)
        {
            int result=-1;//如果返回值是-1则说明连接出现问题
            byte[] callback = new byte[4];
            int returnmsg = 0;
            if (IsConnected(_connectSocket)==true)
            {
                try
                {
                    _sendType =1;
                    _connectSocket.Send(BitConverter.GetBytes(_sendType));
                    _connectSocket.Receive(callback);
                    returnmsg = BitConverter.ToInt32(callback, 0);//在确认服务器收到了操作种类信息后再传值
                    if (returnmsg == 1)
                    {
                        SendString(_connectSocket, username);
                        Console.WriteLine("已发送用户名：{0}",username);
                        SendString(_connectSocket, password);
                        Console.WriteLine("已发送密码：{0}",password);
                        _connectSocket.Receive(callback);
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
        //操作类型1：登录请求，返回值为登陆结果，-1为连接问题，0为登陆成功，7为用户名或密码错误

        public int Register(string username, string password)
        {
            int result = -1;//如果返回值是-1则说明连接出现问题
            byte[] callback = new byte[4];
            int returnmsg = 0;
            if (IsConnected(_connectSocket) == true)
            {
                try
                {
                    _sendType = 2;
                    _connectSocket.Send(BitConverter.GetBytes(_sendType));
                    _connectSocket.Receive(callback);
                    returnmsg = BitConverter.ToInt32(callback, 0);//在确认服务器收到了操作种类信息后再传值
                    if(returnmsg==1)
                    {
                        SendString(_connectSocket, username);
                        Console.WriteLine("已发送用户名：{0}",username);
                        SendString(_connectSocket, password);
                        Console.WriteLine("已发送密码：{0}",password);
                        _connectSocket.Receive(callback);
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
        //操作类型2：注册请求，返回值为注册结果，-1为连接问题，0为登陆成功，7为数据库创建失败，8为用户已存在

        public int EyeStream(string username, string xs, string ys)
        {
            int result = -1;//如果返回值是-1则说明连接出现问题
            byte[] callback = new byte[4];
            int returnmsg = 0;
            if (IsConnected(_connectSocket) == true)
            {
                try
                {
                    _sendType = 4;
                    _connectSocket.Send(BitConverter.GetBytes(_sendType));
                    _connectSocket.Receive(callback);
                    returnmsg = BitConverter.ToInt32(callback, 0);//在确认服务器收到了操作种类信息后再传值
                    if (returnmsg == 1)
                    {
                        SendString(_connectSocket, username);
                        Console.WriteLine("已发送用户名：{0}", username);
                        SendString(_connectSocket, xs);
                        Console.WriteLine("已发送x轴眼球数据：{0}", xs);
                        SendString(_connectSocket, ys);
                        Console.WriteLine("已发送y轴眼球数据：{0}", ys);
                        _connectSocket.Receive(callback);
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
        //操作类型4：上传眼球数据流到服务器，返回值为结果，-1为连接问题，0为上传成功，7为上传失败

        public string AskFile(string filename)
        {
            string result = "失败";//如果返回值是-1则说明连接出现问题
            byte[] callback = new byte[4];
            int returnmsg = 0;
            if (IsConnected(_connectSocket) == true)
            {
                try
                {
                    _sendType = 5;
                    _connectSocket.Send(BitConverter.GetBytes(_sendType));
                    _connectSocket.Receive(callback);
                    returnmsg = BitConverter.ToInt32(callback, 0);//在确认服务器收到了操作种类信息后再传值
                    if (returnmsg == 1)
                    {
                        SendString(_connectSocket, filename);
                        Console.WriteLine("已发送文章名：{0}", filename);
                        result = ReceiveString(_connectSocket);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            return result;
        }
        //操作类型5：上传所需文章索引（当前设定为文章名），返回文章内容，失败返回“失败”，文章不存在返回“文章不存在”

    }
}
