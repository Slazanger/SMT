using System.Collections.Generic;
using System.Linq;

namespace SMT.EVEData
{
    public class Storm
    {
        public string Region { get; set; }
        public string System { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }

        public List<string> StrongArea { get; set; }

        public List<string> WeakArea { get; set; }

        public static List<Storm> GetStorms()
        {
            List<Storm> storms = new List<Storm>();

            try
            {
                string sourceHTML = "https://evescoutrescue.com/home/stormtrack.php";
                string tableXPath = "/html/body/div/div[4]/div/div/div[2]/table/tbody";
                HtmlAgilityPack.HtmlWeb hw = new HtmlAgilityPack.HtmlWeb();

                HtmlAgilityPack.HtmlDocument doc = hw.Load(sourceHTML);
                HtmlAgilityPack.HtmlNodeCollection hnc = doc.DocumentNode.SelectNodes(tableXPath);
                List<List<string>> table = hnc.Descendants("tr")
                    .Where(tr => tr.Elements("td").Count() > 1)
                    .Select(tr => tr.Elements("td").Select(td => td.InnerText.Trim()).ToList())
                    .ToList();

                foreach (List<string> ls in table)
                {
                    Storm s = new Storm();
                    s.Region = ls[0];
                    s.System = ls[1];
                    s.Type = ls[3];
                    s.Name = ls[2];

                    storms.Add(s);
                }
            }
            catch
            {
            }

            return storms;
        }
    }
}