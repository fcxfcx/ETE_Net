using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Data;

namespace database_basic
{
    public class DatabaseAction
    {
        SqlConnection Conn = new SqlConnection("server=59.110.167.50,1433;database=TEST;uid=sa;pwd=Fcx69501;");
        //封装一个数据库字段
        public DatabaseAction()
        {
            this.Conn.Open();
        }

        public int FindUser(string str1,string str2)
            //数据库通过sqldatareader方法判定用户是否存在，密码是否正确，正确返回0，不正确返回7
        {
            int result = 7; 
            try
            {
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = Conn;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = "select * from 用户名及密码 where 用户名='" + str1 + "' and 密码='" + str2 + "'";
                SqlDataReader sdr = cmd.ExecuteReader();
                Console.WriteLine("已经发送报文");
                if(sdr.Read())
                {
                    result = 0;
                    sdr.Close();
                }
                else
                {
                    result = 7;
                    sdr.Close();
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }
            return result;
        }

        public int NewUser(string str1,string str2)
            //向数据库中添加数据，达到创建用户的目的，成功返回0，失败返回7，用户已存在返回8
        {
            int result = 7;
            try
            {
                SqlCommand cmd0 = new SqlCommand();
                cmd0.Connection = Conn;
                cmd0.CommandType = CommandType.Text;
                cmd0.CommandText = ("select * from 用户名及密码 where 用户名='" + str1 + "'");
                SqlDataReader sdr0 = cmd0.ExecuteReader();
                if (sdr0.Read())
                {
                    result = 8;
                    sdr0.Close();
                }//首先遍历数据库查看是否已存在该用户
                else
                {
                    sdr0.Close();
                    SqlCommand cmd1 = new SqlCommand();
                    cmd1.Connection = Conn;
                    cmd1.CommandType = CommandType.Text;
                    cmd1.CommandText = "insert into 用户名及密码(用户名,密码) values('" + str1 + "','" + str2 + "')";
                    cmd1.ExecuteNonQuery();

                    SqlCommand cmd2 = new SqlCommand();
                    cmd2.Connection = Conn;
                    cmd2.CommandType = CommandType.Text;
                    cmd2.CommandText = ("select * from 用户名及密码 where 用户名='" + str1 + "'");
                    SqlDataReader sdr = cmd2.ExecuteReader();
                    if (sdr.Read())
                    {
                        result = 0;
                        sdr.Close();
                    }
                    else
                    {
                        result = 7;
                        sdr.Close();
                    }
                }
            }
            catch
            {}

            return result; 
        }

        public string IfCalibrate(string str1)
            //在数据库中查询某用户的校准数据是否存在，存在返回结果，不存在返回"-1*-1*-1*-1*-1*-1"
        {
            string result = "-1*-1*-1*-1*-1*-1";
            try
            {
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = this.Conn;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = "select 校准数据 from 用户校准数据 where 用户名='" + str1 + "'";
                SqlDataReader sdr = cmd.ExecuteReader();
                Console.WriteLine("已经发送报文");
                if (sdr.Read())
                {
                    result = sdr["校准数据"].ToString();
                    sdr.Close();
                }
                else
                {
                    result = "-1*-1*-1*-1*-1*-1";
                    sdr.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return result;
        }

        public int Calibrate(string str1,string str2)
        //向数据库中添加某用户的校准数据，格式为字符串，用*隔开，成功返回0，失败返回7，已存在并更新返回8
        {
            int result = 7;
            try
            {
                SqlCommand cmd0 = new SqlCommand();
                cmd0.Connection = Conn;
                cmd0.CommandType = CommandType.Text;
                cmd0.CommandText = ("select * from 用户校准数据 where 用户名='" + str1 + "'");
                SqlDataReader sdr0 = cmd0.ExecuteReader();
                if (sdr0.Read())
                {
                    sdr0.Close();
                    SqlCommand cmd3 = new SqlCommand();
                    cmd3.Connection = Conn;
                    cmd3.CommandType = CommandType.Text;
                    cmd3.CommandText = "update 用户校准数据 set 校准数据='"+str2+"' where 用户名='" + str1 + "' ";
                    cmd3.ExecuteNonQuery();

                    SqlCommand cmd4 = new SqlCommand();
                    cmd4.Connection = Conn;
                    cmd4.CommandType = CommandType.Text;
                    cmd4.CommandText = ("select * from 用户校准数据 where 用户名='" + str1 + "' and 校准数据='"+str2+"' ");
                    SqlDataReader sdr1 = cmd4.ExecuteReader();
                    if (sdr1.Read())
                    {
                        result = 8;
                        sdr1.Close();
                    }
                    else
                    {
                        result = 7;
                        sdr1.Close();
                    }
                }//首先遍历数据库查看是否已存在该用户的校准数据，有则更新数据，没有则加入数据
                else
                {
                    sdr0.Close();
                    SqlCommand cmd1 = new SqlCommand();
                    cmd1.Connection = Conn;
                    cmd1.CommandType = CommandType.Text;
                    cmd1.CommandText = "insert into 用户校准数据(用户名,校准数据) values('" + str1 + "','" + str2 + "')";
                    cmd1.ExecuteNonQuery();

                    SqlCommand cmd2 = new SqlCommand();
                    cmd2.Connection = Conn;
                    cmd2.CommandType = CommandType.Text;
                    cmd2.CommandText = ("select * from 用户校准数据 where 用户名='" + str1 + "'");
                    SqlDataReader sdr = cmd2.ExecuteReader();
                    if (sdr.Read())
                    {
                        result = 0;
                        sdr.Close();
                    }
                    else
                    {
                        result = 7;
                        sdr.Close();
                    }
                }
            }
            catch
            { }
            return result;
        }
    }
}
