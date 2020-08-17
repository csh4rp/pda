
using ProcessDataArchiver.DataCore.DbEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Outlook = Microsoft.Office.Interop.Outlook;

namespace ProcessDataArchiver.DataCore.Infrastructure
{
    public class EmailProvider
    {
        public string From { get; set; }
        public string Message { get; set; }
        public string Subject { get; set; }
        public string Addresses { get; set; } 


        public Task SendMailAsync()
        {
            return Task.Run(() =>
            {
                
                Outlook.Application app = new Outlook.Application();
                Outlook.MailItem mailItem = app.CreateItem(Outlook.OlItemType.olMailItem);


                var acc = FindAccount(From);
                if (acc != null)
                {
                    mailItem.SendUsingAccount = acc;
                }

                mailItem.Subject = Subject;
                mailItem.Body = Message;
                mailItem.To = Addresses;
                              
                mailItem.Send();
            });
        }

        private Outlook.Account FindAccount(string email)
        {
            var acc = GetAccounts();
            int count = acc.Count;
            for (int i = 1; i <= count; i++)
            {
                if (acc[i].DisplayName.Equals(email)) 
                return acc[i];
            }
            return null;
        }

        public static bool CheckAccounts()
        {
            Outlook.Application app = new Outlook.Application();
            var accounts = app.Session.Accounts;
            return false;
        }


        public static Outlook.Accounts GetAccounts()
        {
            Outlook.Application app = new Outlook.Application();
            return app.Session.Accounts;
        }

        //public Task SendMessageAsync()
        //{
        //    return Task.Run(() =>
        //    {
        //        SmtpClient client = new SmtpClient();
        //        client.Port = Port;
        //        client.Host = Host;
        //        client.EnableSsl = true;
        //        client.DeliveryMethod = SmtpDeliveryMethod.Network;
        //        client.Credentials = new NetworkCredential(user, password);
        //        Message.From = new MailAddress(user);

        //        client.Send(Message);
        //    });
        //}


        //public static async Task<MailMessage> CreateMessage(string message, CPDataReader reader, IEnumerable<GlobalVariable> gvrs)
        //{
        //    List<int> beginIndex = new List<int>();
        //    List<int> endIndex = new List<int>();

        //    MailMessage mailMsg = new MailMessage();


        //    XDocument doc = XDocument.Parse(message);
        //    var email = doc.Element("email");
        //    var subject = email.Element("subject");
        //    var addresses = email.Element("to").Elements("address");
        //    var body = email.Element("body");

        //    string msg = body.Value;
        //    foreach (Match m in Regex.Matches(message, @"\$"))
        //    {
        //        beginIndex.Add(m.Index);
        //    }
        //    foreach (Match m in Regex.Matches(message, @"\$"))
        //    {
        //        endIndex.Add(m.Index);
        //    }


        //    var varNames = new List<string>();
        //    var comp = StringComparer.InvariantCultureIgnoreCase;
        //    foreach (Match m in Regex.Matches(message, @"(\$\w+\$)"))
        //    {
        //        varNames.Add(m.Value);
        //    }
        //    var trimNames = varNames.Select(s => s.Substring(1, s.Length - 2)).ToList();
        //    var gvToRead = gvrs.Where(g => trimNames.Contains(g.Name, comp)).ToList();
        //    await reader.ReadValuesAsync(gvToRead);

        //    for (int i = 0; i < varNames.Count; i++)
        //    {
        //        msg.Replace(varNames[i], gvToRead[i].CurrentValue.ToString());
        //    }

        //    mailMsg.Subject = subject.Value;
        //    foreach (var adr in addresses)
        //    {
        //        mailMsg.To.Add(adr.Value);
        //    }
        //    mailMsg.Body = msg;

        //    return mailMsg;
        //}
    }
}
