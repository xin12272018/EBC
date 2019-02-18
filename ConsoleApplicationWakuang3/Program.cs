using LitJson;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Management;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApplicationWakuang3
{
    class Program
    {
        static void Main(string[] args)
        {
            //获取本地mac地址
            ManagementClass mc = new ManagementClass("Win32_NetworkAdapterConfiguration");
            ManagementObjectCollection nics = mc.GetInstances();
            bool b = true;
            SqlConnection conn = new SqlConnection(ConfigurationManager.AppSettings["connstrds"]);
            conn.Open();
            foreach (ManagementObject nic in nics)
            {
                if (Convert.ToBoolean(nic["ipEnabled"]) == true)
                {
                    string mac = nic["MacAddress"].ToString();//Mac地址
                    while (b)
                    {
                        //挖矿
                        string strURL = "https://ebakkeservice.gyxkclub.com//yibazhaopu/ServiceWakuang.asmx/shenqingwakuang?macaddress=" + mac + "";
                        System.Net.HttpWebRequest request;
                        // 创建一个HTTP请求
                        request = (System.Net.HttpWebRequest)WebRequest.Create(strURL);
                        //request.Method="get";
                        System.Net.HttpWebResponse response;
                        response = (System.Net.HttpWebResponse)request.GetResponse();
                        System.IO.StreamReader myreader = new System.IO.StreamReader(response.GetResponseStream(), Encoding.UTF8);
                        string responseText = myreader.ReadToEnd();
                        if (!string.IsNullOrEmpty(responseText))
                        {
                            JsonData jdata = JsonMapper.ToObject(responseText);
                            if (jdata["errcode"].ToString() == "0")
                            {
                                string wktimestamp = jdata["wktimestamp"].ToString();
                                int forcecount = 0;//原力数量
                                double ebccount = 0;//易吧币数量
                                string resultbody = "[";//校验内容
                                int c = 0;
                                //统计原力和易吧币持有量
                                for (int i = 0; i < jdata["ebcaccount"].Count; i++)
                                {
                                    if (!string.IsNullOrEmpty(jdata["ebcaccount"][i]["Force"].ToString()) && int.Parse(jdata["ebcaccount"][i]["Force"].ToString()) > 0)
                                    {
                                        forcecount++;
                                    }
                                    if (!string.IsNullOrEmpty(jdata["ebcaccount"][i]["EBC"].ToString()) && jdata["ebcaccount"][i]["blockchainaddress"].ToString() != "0x5c96e1036fa8883b512d1321d129d0a55ee97344")
                                    {
                                        ebccount += double.Parse(jdata["ebcaccount"][i]["EBC"].ToString());

                                    }
                                }
                                //有原力产生时，平均分给有原力的账户
                                if (forcecount > 0)
                                {
                                    double avgebc = Math.Truncate(50.00 / forcecount * 1000000000) / 1000000000;
                                    double tebc = 0;
                                    for (int i = 0; i < jdata["ebcaccount"].Count; i++)
                                    {
                                        if (!string.IsNullOrEmpty(jdata["ebcaccount"][i]["Force"].ToString()) && int.Parse(jdata["ebcaccount"][i]["Force"].ToString()) > 0)
                                        {
                                            if (c != 0)
                                                resultbody += ",";
                                            resultbody += "{";

                                            tebc += avgebc;
                                            resultbody += "\"from\":\"0xbD8f69b07AF1135F1f8E9f9Bf0F4420034DAe49d\",\"to\":\"" + jdata["ebcaccount"][i]["blockchainaddress"].ToString() + "\",\"EBC\":\"" + avgebc + "\",\"Force\":\"-1\",\"RMB\":\"0\"";
                                            resultbody += "}";
                                            c++;
                                        }
                                    }
                                    //小数点矫正
                                    avgebc = 50.00 - tebc;
                                    if (avgebc > 0)
                                    {
                                        resultbody += ",{";
                                        resultbody += "\"from\":\"0xbD8f69b07AF1135F1f8E9f9Bf0F4420034DAe49d\",\"to\":\"0x5c96e1036fa8883b512d1321d129d0a55ee97344\",\"EBC\":\"" + avgebc + "\",\"Force\":\"0\",\"RMB\":\"0\"";
                                        resultbody += "}";
                                    }
                                }
                                else
                                {
                                    //无原力产生，20%分给持有人，80%分给系统账户
                                    double avgebc = Math.Truncate(10.00 / ebccount * 1000000000) / 1000000000;
                                    double tebc = 0;
                                    for (int i = 0; i < jdata["ebcaccount"].Count; i++)
                                    {
                                        if (!string.IsNullOrEmpty(jdata["ebcaccount"][i]["EBC"].ToString()) && double.Parse(jdata["ebcaccount"][i]["EBC"].ToString()) > 0 && jdata["ebcaccount"][i]["blockchainaddress"].ToString() != "0x5c96e1036fa8883b512d1321d129d0a55ee97344")
                                        {
                                            if (c != 0)
                                                resultbody += ",";
                                            resultbody += "{";
                                            double ebc = Math.Truncate(avgebc * double.Parse(jdata["ebcaccount"][i]["EBC"].ToString()) * 1000000000) / 1000000000;
                                            tebc += ebc;
                                            resultbody += "\"from\":\"0xbD8f69b07AF1135F1f8E9f9Bf0F4420034DAe49d\",\"to\":\"" + jdata["ebcaccount"][i]["blockchainaddress"].ToString() + "\",\"EBC\":\"" + ebc + "\",\"Force\":\"0\",\"RMB\":\"0\"";
                                            resultbody += "}";
                                            c++;
                                        }
                                    }
                                    avgebc = 50.00 - tebc;
                                    if (avgebc > 0)
                                    {
                                        resultbody += ",{";
                                        resultbody += "\"from\":\"0xbD8f69b07AF1135F1f8E9f9Bf0F4420034DAe49d\",\"to\":\"0x5c96e1036fa8883b512d1321d129d0a55ee97344\",\"EBC\":\"" + avgebc + "\",\"Force\":\"0\",\"RMB\":\"0\"";
                                        resultbody += "}";
                                    }
                                }
                                resultbody += "]";
                                //对结果进行hash256加密
                                HashAlgorithm hash = HashAlgorithm.Create("SHA256");
                                byte[] result = Encoding.Default.GetBytes(resultbody);
                                byte[] output = hash.ComputeHash(result);
                                string resulthash = BitConverter.ToString(output).Replace("-", "");
                                strURL = "https://ebakkeservice.gyxkclub.com//yibazhaopu/ServiceWakuang.asmx/wakuang?wktimestamp=" + wktimestamp + "&macaddress=" + mac + "&strhash=" + resulthash;

                                // 创建一个HTTP请求
                                request = (System.Net.HttpWebRequest)WebRequest.Create(strURL);
                                //request.Method="get";

                                response = (System.Net.HttpWebResponse)request.GetResponse();
                                myreader = new System.IO.StreamReader(response.GetResponseStream(), Encoding.UTF8);
                                responseText = myreader.ReadToEnd();
                                if (!string.IsNullOrEmpty(responseText))
                                {
                                    jdata = JsonMapper.ToObject(responseText);
                                    if (jdata["errcode"].ToString() == "0")
                                        Console.Write(DateTime.Now.ToString() + " " + "挖矿成功\r\n");
                                    else
                                        Console.Write(DateTime.Now.ToString() + " " + "挖矿失败，请检查代码\r\n");
                                }
                            }
                            else if (jdata["errcode"].ToString() == "1" || jdata["errcode"].ToString() == "3")
                            {
                                Console.Write(DateTime.Now.ToString() + " " + jdata["errmsg"].ToString() + "\r\n");
                                b = false;
                            }
                            else
                            {
                                Console.Write(DateTime.Now.ToString() + " " + jdata["errmsg"].ToString() + "\r\n");
                            }

                            //数据打包
                            strURL = "https://ebakkeservice.gyxkclub.com/yibazhaopu/servicejiedianqueren.asmx/shenqing?macaddress=" + mac + "";

                            // 创建一个HTTP请求
                            request = (System.Net.HttpWebRequest)WebRequest.Create(strURL);
                            //request.Method="get";

                            response = (System.Net.HttpWebResponse)request.GetResponse();
                            myreader = new System.IO.StreamReader(response.GetResponseStream(), Encoding.UTF8);
                            responseText = myreader.ReadToEnd();
                            if (!string.IsNullOrEmpty(responseText))
                            {
                                jdata = JsonMapper.ToObject(responseText);
                                if (jdata["errcode"].ToString() == "0")
                                {
                                    string qrtimestamp = jdata["qrtimestamp"].ToString();
                                    string body = "[";
                                    for (int i = 0; i < jdata["blockchaindetail"].Count; i++)
                                    {
                                        if (i != 0)
                                            body += ",";
                                        body += "{\"from\":\"" + jdata["blockchaindetail"][i]["from"].ToString() + "\",\"to\":\"" + jdata["blockchaindetail"][i]["to"].ToString() + "\",\"EBC\":\"" + jdata["blockchaindetail"][i]["EBC"].ToString() + "\",\"Force\":\"" + jdata["blockchaindetail"][i]["Force"].ToString() + "\",\"RMB\":\"" + jdata["blockchaindetail"][i]["RMB"].ToString() + "\",\"timestamp\":\"" + jdata["blockchaindetail"][i]["timestamp"].ToString() + "\"}";
                                    }
                                    body += "]";
                                    string hash = body;
                                    byte[] clearBytes = Encoding.UTF8.GetBytes(hash);
                                    SHA256 sha256 = new SHA256Managed();
                                    sha256.ComputeHash(clearBytes);
                                    byte[] hashedBytes = sha256.Hash;
                                    sha256.Clear();
                                    hash = "0x" + BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
                                    strURL = "https://ebakkeservice.gyxkclub.com/yibazhaopu/servicejiedianqueren.asmx/queren?qrtimestamp=" + qrtimestamp + "&macaddress="+mac+"&strhash="+hash;

                                    // 创建一个HTTP请求
                                    request = (System.Net.HttpWebRequest)WebRequest.Create(strURL);
                                    //request.Method="get";

                                    response = (System.Net.HttpWebResponse)request.GetResponse();
                                    myreader = new System.IO.StreamReader(response.GetResponseStream(), Encoding.UTF8);
                                    responseText = myreader.ReadToEnd();
                                    if (!string.IsNullOrEmpty(responseText))
                                    {
                                         jdata = JsonMapper.ToObject(responseText);
                                         if (jdata["errcode"].ToString() == "0")
                                         {
                                             Console.Write(DateTime.Now.ToString() + " " + "广播成功!\r\n");
                                         }
                                         else
                                         {
                                             Console.Write(DateTime.Now.ToString() + " " + jdata["errmsg"].ToString() + "\r\n");
                                         }
                                    }

                                }
                                else
                                {
                                    Console.Write(DateTime.Now.ToString() + " " + jdata["errmsg"].ToString() + "\r\n");
                                }
                            }

                            //备份数据
                            strURL = "https://ebakkeservice.gyxkclub.com/yibazhaopu/serviceblockchain.asmx/getList?macaddress=" + mac + "";

                            // 创建一个HTTP请求
                            request = (System.Net.HttpWebRequest)WebRequest.Create(strURL);
                            //request.Method="get";

                            response = (System.Net.HttpWebResponse)request.GetResponse();
                            myreader = new System.IO.StreamReader(response.GetResponseStream(), Encoding.UTF8);
                            responseText = myreader.ReadToEnd();
                            if (!string.IsNullOrEmpty(responseText))
                            {
                                jdata = JsonMapper.ToObject(responseText);
                                if (jdata["errcode"].ToString() == "0")
                                {
                                    if (jdata["blockchainlist"] != null)
                                    {
                                        if (jdata["blockchainlist"].Count == 0)
                                            Console.Write(DateTime.Now.ToString() + " " + "没有可备份的数据\r\n");
                                        for (int i = 0; i < jdata["blockchainlist"].Count; i++)
                                        {
                                            bool b1 = true; //备份数据是否成功标志

                                            string prevhash = jdata["blockchainlist"][i]["prevhash"].ToString();
                                            string hash = jdata["blockchainlist"][i]["hash"].ToString();


                                            SqlDataAdapter adapter = new SqlDataAdapter("select top 1 hash from blockchain where hash='" + hash + "'", conn);
                                            DataSet dsHash = new DataSet();
                                            adapter.Fill(dsHash);
                                            //判断是否重复备份
                                            if (dsHash.Tables[0].Rows.Count == 0)
                                            {
                                                string body = "[";
                                                for (int j = 0; j < jdata["blockchainlist"][i]["body"].Count; j++)
                                                {
                                                    if (j != 0)
                                                        body += ",";
                                                    body += "{\"from\":\"" + jdata["blockchainlist"][i]["body"][j]["from"].ToString() + "\",\"to\":\"" + jdata["blockchainlist"][i]["body"][j]["to"].ToString() + "\",\"EBC\":\"" + jdata["blockchainlist"][i]["body"][j]["EBC"].ToString() + "\",\"Force\":\"" + jdata["blockchainlist"][i]["body"][j]["Force"].ToString() + "\",\"RMB\":\"" + jdata["blockchainlist"][i]["body"][j]["RMB"].ToString() + "\",\"timestamp\":\"" + jdata["blockchainlist"][i]["body"][j]["timestamp"].ToString() + "\"}";
                                                }
                                                body += "]";
                                                string bhash = body;
                                                byte[] clearBytes = Encoding.UTF8.GetBytes(bhash);
                                                SHA256 sha256 = new SHA256Managed();
                                                sha256.ComputeHash(clearBytes);
                                                byte[] hashedBytes = sha256.Hash;
                                                sha256.Clear();
                                                //将加密结果转成二进制取前4位加到128位二进制最后
                                                bhash = "0x" + BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
                                                if (bhash == jdata["blockchainlist"][i]["hash"].ToString())
                                                {
                                                    //判断是否是创始区块
                                                    if (!string.IsNullOrEmpty(prevhash))
                                                    {
                                                        //非创始区块，判断父区块地址是否正确
                                                        adapter = new SqlDataAdapter("select top 1 hash from blockchain where hash='" + prevhash + "'", conn);
                                                        dsHash = new DataSet();
                                                        adapter.Fill(dsHash);
                                                        if (dsHash.Tables[0].Rows.Count == 1)
                                                        {

                                                            SqlCommand cmd = new SqlCommand("insert into blockchain(prevhash,body,hash,timestamp,cointype) values('" + jdata["blockchainlist"][i]["prevhash"].ToString() + "','" + body + "','" + jdata["blockchainlist"][i]["hash"].ToString() + "','" + jdata["blockchainlist"][i]["timestamp"].ToString() + "','EBC')", conn);
                                                            if (cmd.ExecuteNonQuery() == 1)
                                                            {
                                                                Console.Write(DateTime.Now.ToString() + " " + "insert into blockchain(prevhash,body,hash,timestamp,cointype) values('" + jdata["blockchainlist"][i]["prevhash"].ToString() + "','" + body + "','" + jdata["blockchainlist"][i]["hash"].ToString() + "','" + jdata["blockchainlist"][i]["timestamp"].ToString() + "','EBC')\r\n");
                                                                b1 = true;
                                                            }
                                                            else
                                                            {
                                                                b1 = false;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            b1 = false;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        //创始区块记录
                                                        SqlCommand cmd = new SqlCommand("insert into blockchain(prevhash,body,hash,timestamp,cointype) values(null,'" + body + "','" + jdata["blockchainlist"][i]["hash"].ToString() + "','" + jdata["blockchainlist"][i]["timestamp"].ToString() + "','EBC')", conn);
                                                        Console.Write(DateTime.Now.ToString() + " " + "insert into blockchain(prevhash,body,hash,timestamp,cointype) values(null,'" + body + "','" + jdata["blockchainlist"][i]["hash"].ToString() + "','" + jdata["blockchainlist"][i]["timestamp"].ToString() + "','EBC')\r\n");
                                                        if (cmd.ExecuteNonQuery() == 1)
                                                        {
                                                            b1 = true;
                                                        }
                                                        else
                                                        {
                                                            b1 = false;
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    Console.Write(DateTime.Now.ToString() + " " + "数据错误！\r\n");
                                                    b1 = false;
                                                }
                                            }
                                            //确认结果是否正确
                                            if (b)
                                                strURL = "https://ebakkeservice.gyxkclub.com/yibazhaopu/serviceblockchain.asmx/queren?macaddress=" + mac + "&zhuangtai=已确认&hash=" + hash;
                                            else
                                                strURL = "https://ebakkeservice.gyxkclub.com/yibazhaopu/serviceblockchain.asmx/queren?macaddress=" + mac + "&zhuangtai=确认失败&hash=" + hash;

                                            // 创建一个HTTP请求
                                            request = (System.Net.HttpWebRequest)WebRequest.Create(strURL);
                                            //request.Method="get";

                                            response = (System.Net.HttpWebResponse)request.GetResponse();
                                            myreader = new System.IO.StreamReader(response.GetResponseStream(), Encoding.UTF8);
                                            responseText = myreader.ReadToEnd();
                                            Console.Write(DateTime.Now.ToString() + " " + "备份成功!" + "\r\n");

                                        }
                                    }
                                }
                                else
                                {
                                    Console.Write(DateTime.Now.ToString() + " " + jdata["errmsg"].ToString() + "\r\n");
                                }
                            }

                            myreader.Close();

                            Thread.Sleep(60000);//请不要随便改变这个值，1分钟内访问网络超过60次会被系统永久屏蔽

                        }
                        else
                        {
                            Console.Write(DateTime.Now.ToString() + " " + "请检查网络后，运行程序" + "\r\n");
                            b = false;
                        }

                    }
                }
            }

        }
    }
}
