using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ELMFilteringApp.MessageFilter
{
    public class MessageFilterBase
    {
        public static string UrlPattern = "(https?|ftp|file)://[-A-Za-z0-9+&@#/%?=~_|!:,.;]+[-A-Za-z0-9+&@#/%=~_|]";
        public static string EmailPattern = @"[a-zA-Z0-9\.\-_]+@[a-zA-Z0-9\-_]+[a-zA-Z0-9\.]+[a-zA-Z0-9\.]+";
        public static string AbbreviationPattern = "[A-Z]{2,}";
        public static string PhoneNumberPattern = "^(((\\+\\d{2}-)?0\\d{2,3}-\\d{7,8})|((\\+\\d{2}-)?(\\d{2,3}-)?([1][3,4,5,7,8][0-9]\\d{8})))";

        //public string MessageID { get; set; }
        public string MessageType { get; set; }
        public string Sender { get; set; }
        public string MessageBody { get; set; }
        public int CharacterCount { get; set; }
        public int MaxCharacterCount { get; set; }
        public Dictionary<string, List<string>> ExtraList { get; set; } = new Dictionary<string, List<string>>();

        public virtual void ProcessMessage(Dictionary<string, string> abbreviationDic, string messageBody)
        {
            ExtraList = new Dictionary<string, List<string>>();
            bool succeed = FilterSender(messageBody);
            if (!succeed)
            {
                CharacterCount = 0;
                Sender = null;
                MessageBody = null;
                return;
            }
            CalculateCharacterCount(this.MessageBody);
            ProcessAbbreviations(abbreviationDic, this.MessageBody);
            ProcessUrls();
            //CalculateCharacterCount();
        }

        public virtual string GetSubject()
        {
            return null;
        }

        public virtual bool FilterSender(string messageBody)
        {
            return false;
        }

        public virtual void FilterMessage(Dictionary<string, string> abbreviationDic, string messageBody)
        {

        }

        public virtual void ProcessAbbreviations(Dictionary<string, string> abbreviationDic, string messageBody)
        {
            MatchCollection matches = Regex.Matches(messageBody, AbbreviationPattern);
            string body = messageBody;
            foreach (Match match in matches)
            {
                if (!abbreviationDic.ContainsKey(match.Value))
                    continue;
                body = body.Replace(match.Value, $"{match.Value}<{abbreviationDic[match.Value]}>");
            }
            this.MessageBody = body;
        }

        public virtual void ProcessUrls()
        {
            if (string.IsNullOrEmpty(this.MessageBody))
                return;
            MatchCollection matches = Regex.Matches(MessageBody, UrlPattern);
            string body = MessageBody;
            List<string> urls = new List<string>();
            foreach (Match match in matches)
            {
                urls.Add(match.Value);
            }
            MessageBody = Regex.Replace(MessageBody, UrlPattern, "<URL Quarantined>");
            if (urls.Count > 0)
                ExtraList.Add("Url List", urls);
        }

        public void CalculateCharacterCount(string originalMessage)
        {
            string[] array = originalMessage.Split(new char[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            CharacterCount = array.Length;
        }

    }
}
