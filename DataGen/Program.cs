using SMT.EVEData;

namespace DataGen
{
    internal static class Program
    {
        private static SMT.EVEData.EveManager EM { get; set; }

        private static void Main(string[] args)
        {
            // Data Creation
            Console.WriteLine("Creating SMT Data");

            // Initialise the Main Mananger
            EM = new(SMT.EVEData.EveAppConfig.SMT_VERSION);
            EveManager.Instance = EM;
            string inputDataFolder = AppDomain.CurrentDomain.BaseDirectory + @"..\..\..\..\..\EVEData\";
            string outputDataFolder = AppDomain.CurrentDomain.BaseDirectory + @"..\..\..\..\..\EVEData\";

            // Re-Create data
            EM.CreateFromScratch(inputDataFolder, outputDataFolder);

            // now save off custom SVG's for debug purposes
            WriteDebugSVGs(outputDataFolder);
        }

        private static void WriteDebugSVGs(string outputFolder)
        {
            foreach(MapRegion mr in EM.Regions)
            {
                SvgNet.Elements.SvgSvgElement svgRootElement = new SvgNet.Elements.SvgSvgElement(1050, 800);

                Dictionary<string, SvgNet.Elements.SvgRectElement> systemElementMap = new Dictionary<string, SvgNet.Elements.SvgRectElement>();

                foreach(MapSystem s in mr.MapSystems.Values)
                {
                    SvgNet.Elements.SvgRectElement sre = new SvgNet.Elements.SvgRectElement((float)s.Layout.X -5 , (float)s.Layout.Y -5, 10, 10);
                    sre["Type"] = "system";
                    sre["Name"] = s.Name;
                    sre["ID"] = s.ActualSystem.ID;
                    sre["Region"] = s.Region;

                    systemElementMap[s.Name] = sre;

                    SvgNet.Elements.SvgTextElement srtText = new(s.Name, (float)s.Layout.X, (float)s.Layout.Y);

                    svgRootElement.AddChild(sre);
                    svgRootElement.AddChild(srtText);
                }

                // add all the lines

                foreach(MapSystem s in mr.MapSystems.Values)
                {
                    SvgNet.Elements.SvgRectElement from = systemElementMap[s.Name];
                    foreach(string jumpSys in s.ActualSystem.Jumps)
                    {
                        if(!mr.MapSystems.ContainsKey(jumpSys))
                        {
                            continue;
                        }

                        SvgNet.Elements.SvgRectElement to = systemElementMap[jumpSys];

                        SvgNet.Elements.SvgLineElement le = new SvgNet.Elements.SvgLineElement(from.X, from.Y, to.X, to.Y);
                        SvgNet.Types.SvgStyle lineStyle = new SvgNet.Types.SvgStyle();
                        SvgNet.Types.SvgColor sc = new SvgNet.Types.SvgColor("blue");

                        lineStyle.Set("stroke", sc);
                        lineStyle.Set("fill", sc);
                        le.Style = lineStyle;

                        svgRootElement.AddChild(le);
                    }
                }

                string svgStr = svgRootElement.WriteSVGString(false);
                string filePath = $"{outputFolder}/data/SourceMaps/exported/{mr.DotLanRef}_layout.svg";
                using(StreamWriter outputFile = new StreamWriter(filePath))
                {
                    outputFile.WriteLine(svgStr);
                }
            }

            // write a region map :
            {
                SvgNet.Elements.SvgSvgElement svgRootElement = new SvgNet.Elements.SvgSvgElement(2100, 1600);

                foreach(MapRegion mr in EM.Regions)
                {
                    SvgNet.Elements.SvgRectElement sre = new SvgNet.Elements.SvgRectElement((float)mr.UniverseViewX, (float)mr.UniverseViewY, 5, 5);
                    sre["Type"] = "region";
                    sre["Name"] = mr.Name;

                    SvgNet.Elements.SvgTextElement srtText = new(mr.Name, (float)mr.UniverseViewX, (float)mr.UniverseViewY);

                    svgRootElement.AddChild(sre);
                    svgRootElement.AddChild(srtText);
                }

                string svgStr = svgRootElement.WriteSVGString(false);
                string filePath = $"{outputFolder}/data/SourceMaps/exported/region_layout.svg";
                using(StreamWriter outputFile = new StreamWriter(filePath))
                {
                    outputFile.WriteLine(svgStr);
                }
            }
        }
    }
}