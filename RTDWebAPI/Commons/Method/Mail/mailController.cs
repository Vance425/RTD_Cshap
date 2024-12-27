using Microsoft.Extensions.Configuration;
using NLog;
using RTDDAC;
using System;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace RTDWebAPI.Commons.Method.Mail
{
    public class MailController
    {
        public IConfiguration Config { get; set; }
        public DBTool DB { get; set; }
        public ILogger Logger { get; set; }
        private string MailMode { get; set; }
        private string SmtpServer { get; set; }
        private string AccountId { get; set; }
        private string AccountPwd { get; set; }
        public MailMessage MailMsg { get; set; }
        public string SendMail()
        {
            MailMode = Config["MailSetting:Mode"];
            SmtpServer = Config["MailSetting:smtpServer"];
            AccountId = Config["MailSetting:username"];
            AccountPwd = Config["MailSetting:password"];

            string tmpMessage = "";

            try
            {
                switch(MailMode)
                {
                    case "UseZj":
                        SendMailUseZj(out tmpMessage);
                        break;
                    case "UseGmail":
                        SendMailUseGmail(out tmpMessage);
                        break;
                    case "Localhost":
                    default:
                        SendMailLocalhost(out tmpMessage);
                        break;
                }
            }
            catch(Exception ex)
            { }

            return tmpMessage;
        }

        public bool SendMailLocalhost(out string _message)
        {
            bool bResult = false;

            try
            {
                _message = "";

                //MailMessage msg = new MailMessage();
                //msg.To.Add("blinda12@ms4.hinet.net");
                ////msg.To.Add("b@b.com");可以發送給多人
                ////msg.CC.Add("c@c.com");
                ////msg.CC.Add("c@c.com");可以抄送副本給多人 
                ////這裡可以隨便填，不是很重要
                //msg.From = new MailAddress("XXX@gmail.com", "小魚", Encoding.UTF8);
                ///* 上面3個參數分別是發件人地址（可以隨便寫），發件人姓名，編碼*/
                //msg.Subject = "測試標題";//郵件標題
                //msg.SubjectEncoding = Encoding.UTF8;//郵件標題編碼
                //msg.Body = "測試一下"; //郵件內容
                //msg.BodyEncoding = Encoding.UTF8;//郵件內容編碼 
                //msg.Attachments.Add(new Attachment(@"D:\test2.docx"));  //附件
                //msg.IsBodyHtml = true;//是否是HTML郵件 
                                      //msg.Priority = MailPriority.High;//郵件優先級 

                SmtpClient client = new SmtpClient();
                //client.Credentials = new NetworkCredential("XXX@gmail.com", "****"); //這裡要填正確的帳號跟密碼
                client.Credentials = new NetworkCredential(this.AccountId, this.AccountPwd); //這裡要填正確的帳號跟密碼
                client.Host = this.SmtpServer; //"smtp.gmail.com"; //設定smtp Server
                client.Port = 25; //設定Port
                client.EnableSsl = false; //gmail預設開啟驗證
                client.Send(this.MailMsg); //寄出信件
                client.Dispose();
                this.MailMsg.Dispose();
                _message = string.Format("SendMailLocalhost: {0}", "郵件寄送成功！");
                this.Logger.Info(_message);
            }
            catch (Exception ex)
            {
                _message = string.Format("SendMailLocalhost [Exception]: {0}", ex.Message);
                this.Logger.Info(_message);
            }

            return bResult;
        }

        public bool SendMailUseZj(out string _message)
        {
            bool bResult = false;

            //MailMessage msg = new MailMessage();
            //msg.To.Add("blinda12@ms4.hinet.net"); 
            //msg.To.Add("blinda12@ms4.hinet.net"); 
            ///* 
            //* msg.To.Add("[email protected]"); 
            //* msg.To.Add("[email protected]"); 
            //* msg.To.Add("[email protected]");可以傳送給多人 
            //*/ 
            //msg.CC.Add("blinda12@ms4.hinet.net"); 
            ///* 
            //* msg.CC.Add("[email protected]"); 
            //* msg.CC.Add("[email protected]");可以抄送給多人 
            //*/ 
            //msg.From = new MailAddress("[email protected]", "AlphaWu", System.Text.Encoding.UTF8);
            ///* 上面3個引數分別是發件人地址（可以隨便寫），發件人姓名，編碼*/
            //msg.Subject = "這是測試郵件";//郵件標題 
            //msg.SubjectEncoding = Encoding.UTF8;//郵件標題編碼 
            //msg.Body = "郵件內容";//郵件內容 
            //msg.BodyEncoding = Encoding.UTF8;//郵件內容編碼 
            //msg.IsBodyHtml = false;//是否是HTML郵件 
            //msg.Priority = MailPriority.High;//郵件優先順序 

            SmtpClient client = new SmtpClient();
            client.Credentials = new NetworkCredential("[email protected]", "userpass"); 
            //在zj.com註冊的郵箱和密碼 
            client.Host = "smtp.zj.com"; 
            object userState = this.MailMsg; 
            try 
            { 
                client.SendAsync(this.MailMsg, userState);
                //簡單一點兒可以client.Send(msg); 
                _message = string.Format("SendMailUseZj: {0}", "郵件寄送成功！");
                this.Logger.Info(_message);
            } 
            catch (SmtpException ex) 
            {
                bResult = false;
                _message = string.Format("SendMailUseZj: 傳送郵件出錯 [Exception]: {0}", ex.Message);
                this.Logger.Info(_message);
            }

            return bResult;
        }
        public bool SendMailUseGmail(out string _message)
        {
            bool bResult = false;
            //MailMessage msg = new MailMessage();
            //msg.To.Add("blinda12@ms4.hinet.net"); 
            //msg.To.Add("blinda12@ms4.hinet.net"); 
            ///* 
            //* msg.To.Add("[email protected]"); 
            //* msg.To.Add("[email protected]"); 
            //* msg.To.Add("[email protected]");可以傳送給多人 
            //*/ 
            //msg.CC.Add("blinda12@ms4.hinet.net"); 
            ///* 
            //* msg.CC.Add("[email protected]"); 
            //* msg.CC.Add("[email protected]");可以抄送給多人 
            //*/ 
            //msg.From = new MailAddress("[email protected]", "AlphaWu", Encoding.UTF8);
            ///* 上面3個引數分別是發件人地址（可以隨便寫），發件人姓名，編碼*/
            //msg.Subject = "這是測試郵件";//郵件標題 
            //msg.SubjectEncoding = Encoding.UTF8;//郵件標題編碼 
            //msg.Body = "郵件內容";//郵件內容 
            //msg.BodyEncoding = Encoding.UTF8;//郵件內容編碼 
            //msg.IsBodyHtml = false;//是否是HTML郵件 
            //msg.Priority = MailPriority.High;//郵件優先順序 
            SmtpClient client = new SmtpClient();
            client.Credentials = new NetworkCredential(this.AccountId,this.AccountPwd); 
            //上述寫你的GMail郵箱和密碼 
            client.Port = 587;//Gmail使用的埠 
            client.Host = "smtp.gmail.com"; 
            client.EnableSsl = true;//經過ssl加密 
            object userState = this.MailMsg; 
            try 
            {
                client.SendAsync(this.MailMsg, userState);
                //簡單一點兒可以client.Send(msg); 
                _message = string.Format("SendMailUseGmail: {0}", "郵件寄送成功！");
                this.Logger.Info(_message);
            } 
            catch (SmtpException ex) 
            {
                bResult = false;
                _message = string.Format("SendMailUseGmail: 傳送郵件出錯 [Exception]: {0}", ex.Message);
                this.Logger.Info(_message);
            }
            return bResult;
        }
    }
}
