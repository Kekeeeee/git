using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ELMFilteringApp.MessageFilter
{
    public class SmsMessageFilter : MessageFilterBase
    {
        public SmsMessageFilter()
        {
            MessageType = "SMS";
            MaxCharacterCount = 140;
        }

        public override bool FilterSender(string messageBody)
        {
            Match match = Regex.Match(messageBody, PhoneNumberPattern);
            if (!match.Success)
                return false;
            if (!messageBody.StartsWith(match.Value))
                return false;
            Sender = match.Value;
            this.MessageBody = messageBody.Substring(match.Value.Length);
            return true;
        }

        public override void FilterMessage(Dictionary<string, string> abbreviationDic, string messageBody)
        {
            string regPattern = "^(((\\+\\d{2}-)?0\\d{2,3}-\\d{7,8})|((\\+\\d{2}-)?(\\d{2,3}-)?([1][3,4,5,7,8][0-9]\\d{8})))";
            Match match = Regex.Match(messageBody, regPattern);
            if (!match.Success)
                return;
            if (!messageBody.StartsWith(match.Value))
                return;
            Sender = match.Value;

            messageBody = messageBody.Substring(match.Value.Length);
            regPattern = "[A-Z]{2,}";
            MatchCollection matches = Regex.Matches(messageBody, regPattern);
            foreach (Match item in matches)
            {
                if (abbreviationDic.ContainsKey(item.Value))
                    messageBody.Replace(item.Value, $"{item.Value}<{abbreviationDic[item.Value]}>");
            }
            MessageBody = messageBody;
        }
    }
}
