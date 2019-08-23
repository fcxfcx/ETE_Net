using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using database_basic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;

namespace server_net
{
    public class NetAction
    {
        private static string IP = "172.17.82.143";
        private static int Port = 8885;
        private int gettype;
        private new int GetType { get => gettype; set => gettype = value; }
        DatabaseAction mydatabase = new DatabaseAction();//数据库操作对象

        private Dictionary<string, Socket> ClientSocket = new Dictionary<string, Socket>();
        private Dictionary<string, Thread> ClientThread = new Dictionary<string, Thread>();//利用键值对，通过用户IP找用户对应的处理线程

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

        public void WatchBegin()
        {
            IPAddress myip = IPAddress.Parse(IP);
            Socket socketwatch = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socketwatch.Bind(new IPEndPoint(myip, Port));
            socketwatch.Listen(100);//开启监听队列
            Console.WriteLine("监听开始");
            ThreadPool.QueueUserWorkItem(Listen, socketwatch);//向线程池里加入Listen
            Console.ReadKey();
        }//服务器端主要的方法（一键开始服务）

        public void Listen(object o)//等待客户端的连接，创建一个负责通信的socket（newsocket）
        {
            while (true)
            {
                try
                {
                    byte[] temp = new byte[1024];
                    Socket socketwatch = o as Socket;
                    Socket newsocket = socketwatch.Accept();
                    Console.WriteLine("连接成功：{0}", newsocket.RemoteEndPoint.ToString());
                    if (ClientSocket.ContainsKey(newsocket.RemoteEndPoint.ToString()) == false)//先判断服务器端是否存在当前IP的服务socket
                    {
                        ClientSocket.Add(newsocket.RemoteEndPoint.ToString(), newsocket);
                        Thread newthread = new Thread(Define);
                        newthread.IsBackground = true;
                        newthread.Start(newsocket.RemoteEndPoint.ToString());//以当前客户端的IP和端口号为对象，创建对应服务
                        ClientThread.Add(newsocket.RemoteEndPoint.ToString(), newthread);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

            }
        }

        private void Define(object o)
        {
            Socket definesocket = ClientSocket[o as string];
            string clientNow = o as string;//把当前服务的客户端IP截取下来方便关闭线程和Socket
            byte[] type = new byte[4];
            while (true)
            {
                try
                {
                    int i = definesocket.Receive(type);
                    GetType = BitConverter.ToInt32(type, 0);
                    Console.WriteLine("收到请求为：{0}", GetType);
                    switch (GetType)
                    {
                        case 1:
                            Console.WriteLine("开始回应登陆请求");
                            type = BitConverter.GetBytes(1);//给客户端发送消息表示已经接收到操作类型的信息
                            definesocket.Send(type);
                            Console.WriteLine("已告知允许发送数据");
                            Login(definesocket);
                            break;
                        case 2:
                            Console.WriteLine("开始回应注册请求");
                            type = BitConverter.GetBytes(1);//给客户端发送消息表示已经接收到操作类型的信息
                            definesocket.Send(type);
                            Console.WriteLine("已告知允许发送数据");
                            Register(definesocket);
                            break;
                        case 3:
                            Console.WriteLine("客户端选择断开连接");
                            ClientSocket.Remove(definesocket.RemoteEndPoint.ToString());//如果客户端选择断开则去除对应的socket连接和线程
                            definesocket.Shutdown(SocketShutdown.Both);
                            definesocket.Close();
                            ClientThread[clientNow].Abort();
                            ClientThread.Remove(definesocket.RemoteEndPoint.ToString());
                            break;
                        case 4:
                            Console.WriteLine("开始回应上传眼球流数据请求");
                            type = BitConverter.GetBytes(1);//给客户端发送消息表示已经接收到操作类型的信息
                            definesocket.Send(type);
                            Console.WriteLine("已告知允许发送数据");
                            EyeStream(definesocket);
                            break;
                        case 5:
                            Console.WriteLine("开始回应发送文章内容请求");
                            type = BitConverter.GetBytes(1);
                            definesocket.Send(type);
                            Console.WriteLine("已告知允许发送数据");
                            SendFile(definesocket);
                            break;
                    }
                }
                catch (ThreadAbortException e)
                { }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }//负责通信的newsocket将鉴定请求种类并执行相关操作

        private void Login(object o)//执行登陆的操作
        {
            string username;
            string password;
            Socket managesocket = o as Socket;
            byte[] result = new byte[4];
            int database;
            try
            {

                username = ReceiveString(managesocket);
                Console.WriteLine("收到登陆用户名：{0}", username);
                password = ReceiveString(managesocket);
                Console.WriteLine("收到登陆密码：{0}", password);
                database = mydatabase.FindUser(username, password);
                Console.WriteLine("处理结果为：{0}", database);
                result = BitConverter.GetBytes(database);//通过数据库查找，告知客户端，连接问题返回-1，登陆成功返回0，用户名或密码不正确返回7
                managesocket.Send(result);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private void Register(object o)//执行注册的操作
        {
            string username;
            string password;
            Socket managesocket = o as Socket;
            byte[] result = new byte[4];
            int database;
            try
            {

                username = ReceiveString(managesocket);
                Console.WriteLine("收到注册用户名：{0}", username);
                password = ReceiveString(managesocket);
                Console.WriteLine("收到注册密码：{0}", password);
                database = mydatabase.NewUser(username, password);
                Directory.CreateDirectory(@"D:\UserDataTest\" + username);//在注册的同时创建一个储存用户数据的文件夹
                Console.WriteLine("处理结果为：{0}", database);
                result = BitConverter.GetBytes(database);//通过数据库创建，告知客户端，连接问题返回-1，成功返回0，数据库创建失败返回7，已存在返回8
                managesocket.Send(result);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private void SendFile(object o)//发送文章给客户端的操作
        {
            string filename;
            Socket managesocket = o as Socket;
            string result="失败";
            try
            {
                filename = ReceiveString(managesocket);
                string filepath = @"D:\UserDataTest\EnglishText\" + filename+".txt";
                if(File.Exists(filepath))
                {
                    result = File.ReadAllText(filepath);
                }
                else
                {
                    result = "文件不存在";
                }
                SendString(managesocket, result);
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private void EyeStream(object o)//接收眼球流数据并创建文件储存数据
        {
            string username;
            string xs;
            string ys;
            int times,Result;
            string filename;
            Socket managesocket = o as Socket;
            byte[] result = new byte[4];
            try
            {
                username = ReceiveString(managesocket);
                Console.WriteLine("收到用户名：{0}", username);
                xs = ReceiveString(managesocket);
                Console.WriteLine("收到x轴数据：{0}",xs );
                ys = ReceiveString(managesocket);
                Console.WriteLine("收到y轴数据：{0}", ys);
                times = mydatabase.SearchTimes(username);
                if(times == -1)//若出现了问题则返回-1
                {
                    result = BitConverter.GetBytes(-1);
                }
                else
                {
                    filename = times.ToString().PadLeft(4, '0');//文件名格式为当前已读篇数（如0012）
                    string filepath = @"D:\UserDataTest\" + username + @"\"+filename+".txt";
                    FileStream fs = new FileStream(filepath, FileMode.OpenOrCreate,FileAccess.ReadWrite, FileShare.ReadWrite);
                        string mystring = "x轴眼球数据流:" + xs + "\ny轴眼球数据流:" + ys;
                        byte[] data = Encoding.UTF8.GetBytes(mystring);
                        fs.Write(data,0,data.Length);
                        fs.Flush();
                        fs.Close();
                        Result = 0;
                    FileInfo fi = new FileInfo(filepath);
                    if(fi.Length ==0)
                    {
                        mydatabase.AddTimes(username);//使当前用户的已阅读篇目加一
                        Result = 7;

                    }
                    else
                    {
                        Result = 0;
                    }
                    Console.WriteLine("处理结果为：{0}", Result);
                    result = BitConverter.GetBytes(Result);//通过数据库创建，告知客户端，连接问题返回-1，成功返回0，数据库创建失败返回7，已存在返回8
                }
                managesocket.Send(result);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
      }
    }

