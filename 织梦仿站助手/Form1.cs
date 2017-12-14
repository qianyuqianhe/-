using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace 织梦仿站助手
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        //声明一个委托类型，该委托类型无输入参数和输出参数  
        public delegate void ProcessDelegate();
        //子线程委托更新主界面UI
        private void updateRunLog(string log)
        {
            //子线程更新主线程UI
            //实例化一个委托变量，使用匿名方法构造  
            ProcessDelegate showProcess = delegate ()
            {
                txtLog.Text += log;
            };
            txtLog.Invoke(showProcess);
        }
        List<string> cssFilePathList = new List<string>();
        List<string> cssRelativeUrls = new List<string>();
        List<string> jsFilePathList = new List<string>();
        List<string> jsRelativeUrls = new List<string>();
        List<string> imgFilePathList = new List<string>();
        List<string> imgRelativeUrls = new List<string>();
        List<string> urlFilePathList = new List<string>();
        List<string> urlRelativeUrls = new List<string>();
        /// <summary>
        /// 提取按钮的单击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnOk_Click(object sender, EventArgs e)
        {
            if (!Directory.Exists("style"))
            {
                Directory.CreateDirectory("style");
            }
            if (!Directory.Exists("js"))
            {
                Directory.CreateDirectory("js");
            }
            if (!Directory.Exists("images"))
            {
                Directory.CreateDirectory("images");
            }
            if (!Directory.Exists("url"))
            {
                Directory.CreateDirectory("url");
            }

            string url = txtUrl.Text;
            string text = GetHttpWebRequest(url);

            //提取css文件
            string regularCss = "<link.*rel=\"Stylesheet\".*href=\"(.*?)\"|<link.*rel=\"stylesheet\".*href=\"(.*?)\"";
            foreach (string value in GetRegularExpressionValue(text, regularCss))
            {
                MessageBox.Show(value);
                cssFilePathList.Add(GetAbsoluteUrl(value, url));
                cssRelativeUrls.Add(value);
            }

            //提取js文件
            string regularJs = "<script src=\"(.*?)\"|src=\"(.*?)\"></script>";
            foreach (string value in GetRegularExpressionValue(text, regularJs))
            {
                jsFilePathList.Add(GetAbsoluteUrl(value, url));
                jsRelativeUrls.Add(value);
            }

            //提取网页图片文件
            string regularImg = "<img.*?src=\"(.*?)\"";
            foreach (string value in GetRegularExpressionValue(text, regularImg))
            {
                imgFilePathList.Add(GetAbsoluteUrl(value, url));
                imgRelativeUrls.Add(value);
            }

            //提取css文件url里的文件，一般是背景图片和字体等文件
            foreach (string link in cssFilePathList)
            {
                string txtcss = GetHttpWebRequest(link);
                string regularUrl = "url\\((.*?)\\)";
                foreach (string value in GetRegularExpressionValue(txtcss, regularUrl))
                {
                    urlFilePathList.Add(GetAbsoluteUrl(value, url));
                    urlRelativeUrls.Add(value);
                }
            }
            //开启线程下载文件
            Thread DownloadFile = new Thread(new ParameterizedThreadStart(ThreadDownload));
            DownloadFile.Start();


        }
        /// <summary>
        /// 获取网页源码
        /// </summary>
        /// <param name="url">网页地址</param>
        /// <returns>页面源码</returns>
        private string GetHttpWebRequest(string url)
        {
            Uri uri = new Uri(url);
            HttpWebRequest myReq = (HttpWebRequest)WebRequest.Create(uri);
            myReq.UserAgent = "User-Agent:Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705";
            myReq.Accept = "*/*";
            myReq.KeepAlive = true;
            myReq.Headers.Add("Accept-Language", "zh-cn,en-us;q=0.5");
            HttpWebResponse result = (HttpWebResponse)myReq.GetResponse();
            Stream receviceStream = result.GetResponseStream();
            StreamReader readerOfStream = new StreamReader(receviceStream, System.Text.Encoding.GetEncoding("utf-8"));
            string strHTML = readerOfStream.ReadToEnd();
            readerOfStream.Close();
            receviceStream.Close();
            result.Close();
            return strHTML;
        }
        /// <summary>
        /// 通过正则表达式匹配内容
        /// </summary>
        /// <param name="text">待匹配的内容</param>
        /// <param name="regular">正则表达式</param>
        /// <returns>匹配结果的集合</returns>
        private List<string> GetRegularExpressionValue(string text, string regular)
        {
            List<string> value = new List<string>();
            MatchCollection _matchCollection = Regex.Matches(text, regular);
            foreach (Match match in _matchCollection)
            {
                value.Add(match.Groups[1].Value.ToString());
            }
            return value;
        }
        /// <summary>
        /// 将相对路径转换为绝对路径
        /// </summary>
        /// <param name="relativeUrl">相对路径</param>
        /// <param name="baseUrl">相对的绝对路径</param>
        /// <returns>处理过后的绝对路径</returns>
        private String GetAbsoluteUrl(String relativeUrl, string baseUrl)
        {
            Uri baseUri = new Uri(baseUrl);
            Uri absoluteUri = new Uri(baseUri, relativeUrl);
            return absoluteUri.ToString();
        }
        /// <summary>
        /// http下载文件
        /// </summary>
        /// <param name="url">下载文件地址</param>
        /// <param name="path">文件存放文件夹路径</param>
        /// <returns></returns>
        public void HttpDownload(string url, string path)
        {
            Uri uri = new Uri(url);
            string filename = GetRegularExpressionValue(uri.LocalPath, "/([^/]*\\.[^/]*)")[0];
            path += "/" + filename;
            if (!File.Exists(path))
            {
                FileStream fs = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                // 设置参数
                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                //发送请求并获取相应回应数据
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                //直到request.GetResponse()程序才开始向目标网页发送Post请求
                Stream responseStream = response.GetResponseStream();
                //创建本地文件写入流
                //Stream stream = new FileStream(tempFile, FileMode.Create);
                byte[] bArr = new byte[1024];
                int size = responseStream.Read(bArr, 0, (int)bArr.Length);
                while (size > 0)
                {
                    //stream.Write(bArr, 0, size);
                    fs.Write(bArr, 0, size);
                    size = responseStream.Read(bArr, 0, (int)bArr.Length);
                }
                //stream.Close();
                fs.Close();
                responseStream.Close();
            }
        }

        private void ThreadDownload(object obj)
        {
            foreach (string url in cssFilePathList)
            {
                //HttpDownload(url, "style");
            }
            updateRunLog(String.Format("下载css文件完成，共{0}个文件",cssFilePathList.Count));

            foreach (string url in jsFilePathList)
            {
                HttpDownload(url, "js");
            }
            updateRunLog(String.Format("下载js文件完成，共{0}个文件", jsFilePathList.Count));

            foreach (string url in imgFilePathList)
            {
                HttpDownload(url, "images");
            }
            updateRunLog(String.Format("下载图片文件完成，共{0}个文件", imgFilePathList.Count));

            foreach (string url in urlFilePathList)
            {
                HttpDownload(url, "url");
            }
            updateRunLog(String.Format("下载url文件完成，共{0}个文件", urlFilePathList.Count));
        }
        private void button1_Click(object sender, EventArgs e)
        {
            HttpDownload("https://common.cnblogs.com/script/jquery.js?12121212", "");
        }
    }
}
