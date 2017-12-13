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
        List<string> cssFilePathList=new List<string>();
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
            string url = txtUrl.Text;
            string text = GetHttpWebRequest(url);

            //提取css文件
            string regularCss = "href=\"(.*?\\.css.*?)\"";
            foreach(string value in GetRegularExpressionValue(text, regularCss))
            {
                cssFilePathList.Add(GetAbsoluteUrl(value,url));
                cssRelativeUrls.Add(value);
            }

            //提取js文件
            string regularJs = "src=\"(.*?)\"></script>";
            foreach (string value in GetRegularExpressionValue(text, regularJs))
            {
                jsFilePathList.Add(GetAbsoluteUrl(value, url));
                jsRelativeUrls.Add(value);
            }

            //提取网页图片文件
            string regularImg = "<img src=\"(.*?)\".*?/>";
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
    }
}
