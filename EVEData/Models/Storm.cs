namespace EVEData;

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
        var storms = new List<Storm>();

        try
        {
            var sourceHTML = "https://evescoutrescue.com/home/stormtrack.php";
            var tableXPath = "/html/body/div/div[4]/div/div/div[2]/table/tbody";
            var hw = new HtmlAgilityPack.HtmlWeb();

            var doc = hw.Load(sourceHTML);
            var hnc = doc.DocumentNode.SelectNodes(tableXPath);
            var table = hnc.Descendants("tr")
                .Where(tr => tr.Elements("td").Count() > 1)
                .Select(tr => tr.Elements("td").Select(td => td.InnerText.Trim()).ToList())
                .ToList();

            foreach (var ls in table)
            {
                var s = new Storm();
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