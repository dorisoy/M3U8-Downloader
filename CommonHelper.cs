using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.IO;

namespace M3U8_Downloader
{
    public partial class CommonHelper
    {
        /// <summary>
        /// 写入日志文件
        /// </summary>
        /// <param name="input"></param>
        /// <param name="fn"></param>
        public static void WriteLog(string input, string fn)
        {

            ///指定日志文件的目录
            string logPath = System.AppDomain.CurrentDomain.BaseDirectory + "\\log\\";
            if (!Directory.Exists(logPath))
            {
                Directory.CreateDirectory(logPath);
            }
            if (string.IsNullOrEmpty(fn))
                fn = "RS";
            string fname = logPath + "" + fn + "_" + DateTime.Now.ToString("yyyyMMdd") + ".txt";
            ///定义文件信息对象
            //FileInfo finfo = new FileInfo(fname);
            ///创建只写文件流
            using (FileStream fs = new FileStream(fname, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite))
            {
                ///根据上面创建的文件流创建写数据流
                StreamWriter w = new StreamWriter(fs, Encoding.UTF8);
                ///设置写数据流的起始位置为文件流的末尾
                w.BaseStream.Seek(0, SeekOrigin.End);
                //w.Write("【" + fn + "】");
                ///写入当前系统时间并换行
                //w.Write("{0} {1} \r\n", DateTime.Now.ToLongDateString(), DateTime.Now.ToLongTimeString());
                ///写入------------------------------------“并换行
                ///写入日志内容并换行
                w.Write(input + "\r\n");
                ///清空缓冲区内容，并把缓冲区内容写入基础流
                w.Flush();
                ///关闭写数据流
                w.Close();

                fs.Close();
            }
        }

        public static void WriteFile(string input,string fName)
        {

            ///指定日志文件的目录
            string logPath = System.AppDomain.CurrentDomain.BaseDirectory + "Files\\";
            if (!Directory.Exists(logPath))
            {
                Directory.CreateDirectory(logPath);
            }
            string fname = logPath + "" + fName + ".txt";
            ///定义文件信息对象
            //FileInfo finfo = new FileInfo(fname);
            //File.OpenRead(file)
            ///创建只写文件流
            using (FileStream fs = new FileStream(fname, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite))
            {
                ///根据上面创建的文件流创建写数据流
                StreamWriter w = new StreamWriter(fs, Encoding.UTF8);
                ///设置写数据流的起始位置为文件流的末尾
                w.BaseStream.Seek(0, SeekOrigin.End);
                //w.Write("【" + fn + "】");
                ///写入当前系统时间并换行
                //w.Write("{0} {1} \r\n", DateTime.Now.ToLongDateString(), DateTime.Now.ToLongTimeString());
                ///写入------------------------------------“并换行
                ///写入日志内容并换行
                w.Write(input + "\r\n");
                ///清空缓冲区内容，并把缓冲区内容写入基础流
                w.Flush();
                ///关闭写数据流
                w.Close();
                //
                fs.Close();
            }

            //using (TextReader input = new StreamReader(new FileStream(@"C:\Test.properties", FileMode.Open), Encoding.UTF8))
            //{
            //    using (TextWriter output = new StreamWriter(new FileStream(@"C:\Test2.lmx",
            //              FileMode.Create), Encoding.UTF8))
            //    {
            //        int BufferSize = 8096;
            //        char[] buffer = new char[i];
            //        int len;

            //        while ((len = input.Read(buffer, 0, i)) > 0)
            //        {
            //            output.Write(buffer, 0, len);
            //        }

            //        input.Close();
            //    }
            //}

        }

        public static void WriteMonToPath(string input)
        {

            string fname = System.AppDomain.CurrentDomain.BaseDirectory + "WSS.mon";
            ///定义文件信息对象
            //FileInfo finfo = new FileInfo(fname);
            //File.OpenRead(file)
            ///创建只写文件流
            using (FileStream fs = new FileStream(fname, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
            {
                ///根据上面创建的文件流创建写数据流
                StreamWriter w = new StreamWriter(fs, Encoding.UTF8);
                ///设置写数据流的起始位置为文件流的末尾
                w.BaseStream.Seek(0, SeekOrigin.End);
                //w.Write("【" + fn + "】");
                ///写入当前系统时间并换行
                //w.Write("{0} {1} \r\n", DateTime.Now.ToLongDateString(), DateTime.Now.ToLongTimeString());
                ///写入------------------------------------“并换行
                ///写入内容并换行
                w.Write(input + "\r\n");
                ///清空缓冲区内容，并把缓冲区内容写入基础流
                w.Flush();
                ///关闭写数据流
                w.Close();
                //
                fs.Close();
            }
        }


        /// <summary>
        /// 生成随机数字
        /// </summary>
        /// <param name="length">生成长度</param>
        /// <returns></returns>
        public static string GenerateNumber(int Length)
        {
            return Number(Length, false);
        }

        /// <summary>
        /// 生成随机数字
        /// </summary>
        /// <param name="Length">生成长度</param>
        /// <param name="Sleep">是否要在生成前将当前线程阻止以避免重复</param>
        /// <returns></returns>
        public static string Number(int Length, bool Sleep)
        {
            if (Sleep)
                System.Threading.Thread.Sleep(3);
            string result = "";
            System.Random random = new Random();
            for (int i = 0; i < Length; i++)
            {
                result += random.Next(10).ToString();
            }
            return result;
        }

        /// <summary>
        /// FormatFileSize
        /// </summary>
        /// <param name="fileSize"></param>
        /// <returns></returns>
        public static String FormatFileSize(Int64 fileSize)
        {
            if (fileSize < 0)
            {
                throw new ArgumentOutOfRangeException("fileSize");
            }
            else if (fileSize >= 1024 * 1024 * 1024)
            {
                return string.Format("{0:########0.00} GB", ((Double)fileSize) / (1024 * 1024 * 1024));
            }
            else if (fileSize >= 1024 * 1024)
            {
                return string.Format("{0:####0.00} MB", ((Double)fileSize) / (1024 * 1024));
            }
            else if (fileSize >= 1024)
            {
                return string.Format("{0:####0.00} KB", ((Double)fileSize) / 1024);
            }
            else
            {
                return string.Format("{0} bytes", fileSize);
            }
        }

        public static void WriteCacheFiles(string input, string fpath, Encoding encoding)
        {
            //using (FileStream fs = new FileStream(fname, FileMode.Open, FileAccess.Write, FileShare.ReadWrite))
            //允许覆盖
            using (FileStream fs = new FileStream(fpath, FileMode.Create, FileAccess.Write))
            {
                if (encoding == null)
                    throw new ArgumentNullException("encoding");
                ///根据上面创建的文件流创建写数据流
                StreamWriter w = new StreamWriter(fs);
                ///设置写数据流的起始位置为文件流的末尾
                w.BaseStream.Seek(0, SeekOrigin.End);
                w.Write(input);
                ///清空缓冲区内容，并把缓冲区内容写入基础流
                w.Flush();
                ///关闭写数据流
                w.Close();
                //
                fs.Close();
            }
        }

        public static void WriteLogToFile(string input, string fpath, Encoding encoding)
        {
            using (FileStream fs = new FileStream(fpath, FileMode.Open, FileAccess.Write, FileShare.ReadWrite))
            {
                ///根据上面创建的文件流创建写数据流
                StreamWriter w = new StreamWriter(fs, Encoding.UTF8);
                ///设置写数据流的起始位置为文件流的末尾
                w.BaseStream.Seek(0, SeekOrigin.End);
                ///写入------------------------------------“并换行
                ///写入内容并换行
                w.Write(input + "\r\n");
                ///清空缓冲区内容，并把缓冲区内容写入基础流
                w.Flush();
                ///关闭写数据流
                w.Close();
                //
                fs.Close();
            }
        }
    }

}