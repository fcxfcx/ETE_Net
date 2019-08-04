using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using client_net;

namespace 客户端程序
{
    class Program
    {
        static void Main(string[] args)
        {
            string username,xs;
            string password,ys;
            int function;
            int connect,result=0;
            string Result = "";
            string data = "";
            NetAction newclient = new NetAction();
            connect = newclient.Connect();
            if (connect == 0) Console.WriteLine("连接成功");
            Console.WriteLine("请选择要使用的功能：");
            Console.WriteLine("1.登陆  2.注册 3.断开连接 4.上传眼球数据流 5.请求文章数据");
            while (true)
            {
                function = Convert.ToInt32(Console.ReadLine());
                switch (function)
                {
                    case 1:
                        Console.WriteLine("请输入用户名");
                        username = Console.ReadLine();
                        Console.WriteLine("请输入密码");
                        password = Console.ReadLine();
                        result = newclient.Login(username, password);
                        if (result == -1) Console.WriteLine("连接出现问题");
                        if (result == 0) Console.WriteLine("登陆成功");
                        if (result == 7) Console.WriteLine("用户名或密码错误！");
                        break;
                    case 2:
                        Console.WriteLine("请输入用户名");
                        username = Console.ReadLine();
                        Console.WriteLine("请输入密码");
                        password = Console.ReadLine();
                        result  = newclient.Register(username, password);
                        if (result == -1) Console.WriteLine("连接出现问题");
                        if (result == 0) Console.WriteLine("注册成功");
                        if (result == 7) Console.WriteLine("数据库出现问题，注册失败，请重试");
                        if (result == 8) Console.WriteLine("用户名已存在，请重新选择用户名注册");
                        break;
                    case 3:
                        newclient.DisConnect();
                        Console.WriteLine("已断开连接，请关闭程序");
                        Console.ReadKey();
                        break;
                    case 4:
                        Console.WriteLine("请输入用户名：");
                        username = Console.ReadLine();
                        Console.WriteLine("请输入x轴数据");
                        xs = Console.ReadLine();
                        Console.WriteLine("请输入y轴数据");
                        ys = Console.ReadLine();
                        result = newclient.EyeStream(username, xs, ys);
                        if (result == -1) Console.WriteLine("连接出现问题");
                        if (result == 0) Console.WriteLine("上传成功");
                        if (result == 7) Console.WriteLine("服务器出现问题，上传失败，请重试");
                        break;
                    case 5:
                        Console.WriteLine("请输入文章名：");
                        data = Console.ReadLine();
                        Result = newclient.AskFile(data);
                        Console.WriteLine(Result);
                        break;
                }
            }
        }
    }
}
