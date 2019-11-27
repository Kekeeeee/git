using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ELMFilteringApp.MessageFilter
{
    public class TweetMessageFilter : MessageFilterBase
    {
        public Dictionary<string, int> HashTags { get; set; } = new Dictionary<string, int>();
        public HashSet<string> TwitterIDs { get; set; } = new HashSet<string>();

        public TweetMessageFilter()
        {
            MessageType = "Tweets";
            MaxCharacterCount = 140;
        }

        public override bool FilterSender(string messageBody)
        {
            if (string.IsNullOrEmpty(messageBody))
                return false;
            string idStr = messageBody.Substring(0, 16);
            if (!idStr.StartsWith("@"))
                return false;
            string[] array = idStr.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            Sender = array[0];
            this.MessageBody = messageBody.Substring(idStr.Length);
            ProcessHashTags(this.MessageBody);
            return true;
        }

        private void ProcessHashTags(string originMessage)
        {
            HashTags.Clear();
            TwitterIDs.Clear();
            string[] array = originMessage.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var item in array)
            {
                if (item.StartsWith("#"))
                {
                    if (!HashTags.ContainsKey(item))
                        HashTags[item] = 0;
                    HashTags[item]++;
                    continue;
                }
                if (item.StartsWith("@"))
                {
                    TwitterIDs.Add(item);
                }
            }
            var tags = HashTags.OrderByDescending(p => p.Value);
            List<string> trendingList = new List<string>();
            foreach (var item in tags)
            {
                trendingList.Add($"count:{item.Value.ToString().PadLeft(2)},{item.Key}");
            }
            ExtraList.Add("Trending List", trendingList);
            ExtraList.Add("Mention List", TwitterIDs.ToList());
        }
    }
}
