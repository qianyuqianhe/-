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
        List<string> cssFileName = new List<string>();

        List<string> jsFilePathList = new List<string>();
        List<string> jsRelativeUrls = new List<string>();
        List<string> jsFileName = new List<string>();

        List<string> imgFilePathList = new List<string>();
        List<string> imgRelativeUrls = new List<string>();
        List<string> imgFileName = new List<string>();

        List<string> urlFilePathList = new List<string>();
        List<string> urlRelativeUrls = new List<string>();

        string text="";
        string url="";
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

            url = txtUrl.Text;
            text = GetHttpWebRequest(url);


            //提取css文件
            string[] regularCss = {
                "<link.*rel=\"Stylesheet\".*href=\"(.*?)\"",
                "<link.*rel=\"stylesheet\".*href=\"(.*?)\"",
                "<link href='(.*?)' rel='stylesheet'",
                "<link href=\"(.*?)\".*rel=\"stylesheet\">"
            };            
            foreach(string regular in regularCss)
            {
                foreach (string value in GetRegularExpressionValue(text, regular))
                {
                    cssFilePathList.Add(GetAbsoluteUrl(value, url));
                    cssRelativeUrls.Add(value);
                }
            }


            //提取js文件
            string[] regularJs = {
                "<script src=\"(.*?)\"",
                "src=\"([^\"]*?)\".*?></script>"
            };
            foreach(string regular in regularJs)
            {
                foreach (string value in GetRegularExpressionValue(text, regular))
                {
                    jsFilePathList.Add(GetAbsoluteUrl(value, url));
                    jsRelativeUrls.Add(value);
                }
            }


            //提取网页图片文件
            string[] regularImg = {
                "<img.*?src=\"(.*?)\""
            };
            foreach(string regular in regularImg)
            {
                foreach (string value in GetRegularExpressionValue(text, regular))
                {
                    imgFilePathList.Add(GetAbsoluteUrl(value, url));
                    imgRelativeUrls.Add(value);
                }
            }


            //提取css文件url里的文件，一般是背景图片和字体等文件
            string[] regularUrl = {
                "url\\('(.*?)'\\)",
                "url\\(\"(.*?)\"\\)"
            };
            foreach (string link in cssFilePathList)
            {
                string txtcss = GetHttpWebRequest(link);
                foreach(string regular in regularUrl)
                {
                    foreach (string value in GetRegularExpressionValue(txtcss, regular))
                    {
                        if (!value.StartsWith("data"))
                        {
                            urlFilePathList.Add(GetAbsoluteUrl(value, link));
                            urlRelativeUrls.Add(value);
                        }
                    }
                }
                
            }
            //开启线程下载文件并生成预处理的网页文件
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
            try {
                Uri uri = new Uri(url);
                HttpWebRequest myReq = (HttpWebRequest)WebRequest.Create(uri);
                myReq.Method = "GET";
                myReq.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/63.0.3239.84 Safari/537.36";                    
                myReq.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8";
                myReq.KeepAlive = true;
                myReq.Headers.Add("Accept-Language", "zh-CN,zh;q=0.9,en-US;q=0.8,en;q=0.7");
                myReq.Referer = url;
                myReq.AllowAutoRedirect = false;
                HttpWebResponse result = (HttpWebResponse)myReq.GetResponse();
                Stream receviceStream = result.GetResponseStream();
                StreamReader readerOfStream = new StreamReader(receviceStream, System.Text.Encoding.GetEncoding("utf-8"));
                string strHTML = readerOfStream.ReadToEnd();
                readerOfStream.Close();
                receviceStream.Close();
                result.Close();
                return strHTML;
            }
            catch(Exception e)
            {

                updateRunLog(String.Format("获取页面源码出错，发生在链接：{0}\r\n",url));
                updateRunLog(e.ToString()+"\r\n");
                return "";
            }
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
            try { 
                MatchCollection _matchCollection = Regex.Matches(text, regular);
                foreach (Match match in _matchCollection)
                {
                    value.Add(match.Groups[1].Value.ToString());
                }
            }
            catch
            {
                value = new List<string>();
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
            string filename = "";
            if (GetRegularExpressionValue(uri.LocalPath, "/([^/]*\\.[^/]*)").Count!=0)
            {
                filename = GetRegularExpressionValue(uri.LocalPath, "/([^/]*\\.[^/]*)")[0];
            }
            else
            {
                int i = url.LastIndexOf("/");
                filename=url.Substring(i);
            }
            if (path.Equals("style"))
            {
                cssFileName.Add("{dede:global.cfg_templets_skin/}/style/"+filename);
            }
            if (path.Equals("js"))
            {
                jsFileName.Add("{dede:global.cfg_templets_skin/}/js/" + filename);
            }
            if (path.Equals("images"))
            {
                imgFileName.Add("{dede:global.cfg_templets_skin/}/images/" + filename);
            }
            path += "/" + filename;
            if (!File.Exists(path))
            {
                try {
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
                catch {
                    updateRunLog(String.Format("下载文件出错，发生在链接：{0}\r\n", url));
                }
            }
        }

        private void ThreadDownload(object obj)
        {
            foreach (string url in cssFilePathList)
            {
                HttpDownload(url, "style");
            }
            updateRunLog(String.Format("下载css文件完成，共{0}个文件。\r\n",cssFilePathList.Count));

            foreach (string url in jsFilePathList)
            {
                HttpDownload(url, "js");
            }
            updateRunLog(String.Format("下载js文件完成，共{0}个文件。\r\n", jsFilePathList.Count));

            foreach (string url in imgFilePathList)
            {
                HttpDownload(url, "images");
            }
            updateRunLog(String.Format("下载图片文件完成，共{0}个文件。\r\n", imgFilePathList.Count));

            foreach (string url in urlFilePathList)
            {
                HttpDownload(url, "url");
            }
            updateRunLog(String.Format("下载url文件完成，共{0}个文件。\r\n", urlFilePathList.Count));
            //生成预处理的网页文件
            string newhtml = text;
            for (int i = 0; i < cssFileName.Count; i++)
            {
                newhtml = newhtml.Replace(cssRelativeUrls[i], cssFileName[i]);
            }
            for (int i = 0; i < jsFileName.Count; i++)
            {
                newhtml = newhtml.Replace(jsRelativeUrls[i], jsFileName[i]);
            }
            for (int i = 0; i < imgFileName.Count; i++)
            {
                newhtml = newhtml.Replace(imgRelativeUrls[i], imgFileName[i]);
            }
            File.WriteAllText("index.html", newhtml, Encoding.UTF8);
            updateRunLog("生成预处理的网页文件index.html成功!\r\n");
        }
        private void button1_Click(object sender, EventArgs e)
        {
            txtLog.Text = "";
            cssFilePathList = new List<string>();
            cssRelativeUrls = new List<string>();
            cssFileName = new List<string>();

            jsFilePathList = new List<string>();
            jsRelativeUrls = new List<string>();
            jsFileName = new List<string>();

            imgFilePathList = new List<string>();
            imgRelativeUrls = new List<string>();
            imgFileName = new List<string>();

            urlFilePathList = new List<string>();
            urlRelativeUrls = new List<string>();

            text = "";
            url = "";
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            MessageBox.Show(GetHttpWebRequest("http://www.mcsite.cn/"));
        }
    }
}
