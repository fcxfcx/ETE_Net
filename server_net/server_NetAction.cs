using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using database_basic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;

namespace server_net
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public class NetAction
    {
        //字段
        private const string IP = "172.17.82.143";
        private const int Port = 8885;
        private readonly DatabaseAction _myDatabase = new DatabaseAction();//数据库操作对象
        //委托
        private delegate void BasicAction(object o);
        //字典
        private readonly Dictionary<int, BasicAction> _actionDict = new Dictionary<int, BasicAction>();
        private readonly Dictionary<string, Socket> _clientSocket = new Dictionary<string, Socket>();
        private readonly Dictionary<string, Thread> _clientThread = new Dictionary<string, Thread>();//利用键值对，通过用户IP找用户对应的处理线程
        //方法
        private static void SendString(Socket s, string str)
        {
            var i = str.Length;
            if (i == 0)
                return;
            i *= 2;
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
        public void WatchBegin()
        {
            var myIp = IPAddress.Parse(IP);
            var socketWatch = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socketWatch.Bind(new IPEndPoint(myIp, Port));
            socketWatch.Listen(100);//开启监听队列
            Console.WriteLine("监听开始");
            ThreadPool.QueueUserWorkItem(Listen, socketWatch);//向线程池里加入Listen
            Console.ReadKey();
        }//服务器端主要的方法（一键开始服务）
        [SuppressMessage("ReSharper", "FunctionNeverReturns")]
        private void Listen(object o)//等待客户端的连接，创建一个负责通信的socket
        {
            while (true)
            {
                try
                {
                    if (!(o is Socket socketWatch)) continue;
                    var newSocket = socketWatch.Accept();
                    Console.WriteLine("连接成功：{0}", newSocket.RemoteEndPoint);
                    if (_clientSocket.ContainsKey(newSocket.RemoteEndPoint.ToString())) continue;
                    _clientSocket.Add(newSocket.RemoteEndPoint.ToString(), newSocket);
                    var newThread = new Thread(Define) {IsBackground = true};
                    newThread.Start(newSocket.RemoteEndPoint.ToString());//以当前客户端的IP和端口号为对象，创建对应服务
                    _clientThread.Add(newSocket.RemoteEndPoint.ToString(), newThread);
                }
                catch
                {
                    // ignored
                }
            }
        }
        private void Define(object o)
        {
            var defineSocket = _clientSocket[o as string ?? throw new InvalidOperationException("The object of Define must be a string ")];
            var clientNow = (string) o;//把当前服务的客户端IP截取下来方便关闭线程和Socket
            var type = new byte[4];
            BasicAction login = Login;
            BasicAction register = Register;
            BasicAction sendfile = SendFile;
            BasicAction streamer = EyeStream;
            _actionDict.Add(1, login);
            _actionDict.Add(2, register);
            _actionDict.Add(5, sendfile);
            _actionDict.Add(4, streamer);
            while (true)
            {
                try
                {
                    
                    defineSocket.Receive(type);
                    var i = BitConverter.ToInt32(type, 0);
                    Console.WriteLine("收到请求为：{0}", i);
                    type = BitConverter.GetBytes(1);
                    defineSocket.Send(type);
                    Console.WriteLine("已告知允许发送数据");
                    _actionDict[i](defineSocket);
                }
                catch (ThreadAbortException)
                { }
                catch
                {
                    // ignored
                }
            }
        }//负责通信的socket将鉴定请求种类并执行相关操作
        private void Login(object o)//执行登陆的操作
        {
            var manageSocket = o as Socket;
            try
            {

                var username = ReceiveString(manageSocket);
                Console.WriteLine("收到登陆用户名：{0}", username);
                var password = ReceiveString(manageSocket);
                Console.WriteLine("收到登陆密码：{0}", password);
                var database = _myDatabase.FindUser(username, password);
                Console.WriteLine("处理结果为：{0}", database);
                var result = BitConverter.GetBytes(database);
                manageSocket?.Send(result);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
        private void Register(object o)//执行注册的操作
        {
            var manageSocket = o as Socket;
            try
            {

                var username = ReceiveString(manageSocket);
                Console.WriteLine("收到注册用户名：{0}", username);
                var password = ReceiveString(manageSocket);
                Console.WriteLine("收到注册密码：{0}", password);
                var database = _myDatabase.NewUser(username, password);
                Directory.CreateDirectory(@"C:\ETEUserData\" + username);//在注册的同时创建一个储存用户数据的文件夹
                Console.WriteLine("处理结果为：{0}", database);
                var result = BitConverter.GetBytes(database);
                manageSocket?.Send(result);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
        private static void SendFile(object o)//发送文章给客户端的操作
        {
            var manageSocket = o as Socket;
            try
            {
                var filename = ReceiveString(manageSocket);
                var filepath = @"C:\ETEUserData\EnglishText\" + filename+".txt";
                var result = File.Exists(filepath) ? File.ReadAllText(filepath) : "文件不存在";
                SendString(manageSocket, result);
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }
        }
        private void EyeStream(object o)//接收眼球流数据并创建文件储存数据
        {
            var manageSocket = o as Socket;
            try
            {
                var username = ReceiveString(manageSocket);
                Console.WriteLine("收到用户名：{0}", username);
                var xs = ReceiveString(manageSocket);
                Console.WriteLine("收到x轴数据：{0}",xs );
                var ys = ReceiveString(manageSocket);
                Console.WriteLine("收到y轴数据：{0}", ys);
                var times = _myDatabase.SearchTimes(username);
                byte[] result;
                if(times == -1)//若出现了问题则返回-1
                {
                    result = BitConverter.GetBytes(-1);
                }
                else
                {
                    var filename = times.ToString().PadLeft(4, '0');
                    var filepath = @"C:\ETEUserData\" + username + @"\"+filename+".txt";
                    var fs = new FileStream(filepath, FileMode.OpenOrCreate,FileAccess.ReadWrite, FileShare.ReadWrite);
                        var myString = "x轴眼球数据流:" + xs + "\ny轴眼球数据流:" + ys;
                        var data = Encoding.UTF8.GetBytes(myString);
                        fs.Write(data,0,data.Length);
                        fs.Flush();
                        fs.Close();
                        var fi = new FileInfo(filepath);
                    if(fi.Length ==0)
                    {
                        _myDatabase.AddTimes(username);//使当前用户的已阅读篇目加一
                        result = BitConverter.GetBytes(0);

                    }
                    else
                    {
                        result = BitConverter.GetBytes(7);
                    }
                    Console.WriteLine("处理结果为：{0}",BitConverter.ToInt32(result,0)); //通过数据库创建，告知客户端，连接问题返回-1，成功返回0，创建失败返回7
                }
                manageSocket?.Send(result);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
      }
    }

