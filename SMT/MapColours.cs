using System.ComponentModel;
using System.Windows.Media;

namespace SMT
{
    public class MapColours
    {
        [Category("Incursion / 入侵")]
        [DisplayName("Active Incursion / 活跃入侵")]
        public Color ActiveIncursionColour { get; set; }

        [Category("Character / 角色")]
        [DisplayName("Highlight / 高亮")]
        public Color CharacterHighlightColour { get; set; }

        [Category("Character / 角色")]
        [DisplayName("Text / 文本颜色")]
        public Color CharacterTextColour { get; set; }

        [Category("Character / 角色")]
        [DisplayName("OfflineText / 离线文本颜色")]
        public Color CharacterOfflineTextColour { get; set; }

        [Category("Character / 角色")]
        [DisplayName("Fleet Member Text / 舰队成员文本颜色")]
        public Color FleetMemberTextColour { get; set; }

        [Category("Character / 角色")]
        [DisplayName("Text Size / 文本大小")]
        public int CharacterTextSize { get; set; }

        [Category("Gates / 星门")]
        [DisplayName("Constellation / 星座")]
        public Color ConstellationGateColour { get; set; }

        [Category("Gates / 星门")]
        [DisplayName("Region / 星域")]
        public Color RegionGateColour { get; set; }

        [Category("SOV / 主权")]
        [DisplayName("Constellation Highlight / 星座高亮")]
        public Color ConstellationHighlightColour { get; set; }

        [Category("ESI Data / ESI 数据")]
        [DisplayName("Overlay / 覆盖层")]
        public Color ESIOverlayColour { get; set; }

        [Category("Jump Bridges / 跳桥")]
        [DisplayName("Friendly / 友方")]
        public Color FriendlyJumpBridgeColour { get; set; }

        [Category("Jump Bridges / 跳桥")]
        [DisplayName("Disabled / 禁用")]
        public Color DisabledJumpBridgeColour { get; set; }

        [Category("Universe / 宇宙全景")]
        [DisplayName("System / 星系")]
        public Color UniverseSystemColour { get; set; }

        [Category("Universe / 宇宙全景")]
        [DisplayName("Constellation Gate / 星座星门")]
        public Color UniverseConstellationGateColour { get; set; }

        [Category("Universe / 宇宙全景")]
        [DisplayName("Region Gate / 星域星门")]
        public Color UniverseRegionGateColour { get; set; }

        [Category("Universe / 宇宙全景")]
        [DisplayName("Gate / 星门")]
        public Color UniverseGateColour { get; set; }

        [Category("Universe / 宇宙全景")]
        [DisplayName("System Text / 星系文本")]
        public Color UniverseSystemTextColour { get; set; }

        [Category("Universe / 宇宙全景")]
        [DisplayName("Map Background / 地图背景")]
        public Color UniverseMapBackgroundColour { get; set; }

        [Category("Systems / 星系")]
        [DisplayName("In Region / 区域内")]
        public Color InRegionSystemColour { get; set; }

        [Category("Systems / 星系")]
        [DisplayName("In Region Text / 区域内文本")]
        public Color InRegionSystemTextColour { get; set; }

        [Category("Intel / 情报")]
        [DisplayName("Clear / 清除")]
        public Color IntelClearOverlayColour { get; set; }

        [Category("Intel / 情报")]
        [DisplayName("Warning / 警告")]
        public Color IntelOverlayColour { get; set; }

        [Category("Navigation / 导航")]
        [DisplayName("In Range / 范围内")]
        public Color JumpRangeInColour { get; set; }

        [Category("Navigation / 导航")]
        [DisplayName("In Range Highlight / 范围内高亮")]
        public Color JumpRangeInColourHighlight { get; set; }

        [Category("Navigation / 导航")]
        [DisplayName("Jump Overlap Highlight / 跳跃重叠高亮")]
        public Color JumpRangeOverlapHighlight { get; set; }

        [Category("General / 常规")]
        [DisplayName("Map Background / 地图背景")]
        public Color MapBackgroundColour { get; set; }

        [Browsable(false)]
        public string Name { get; set; }

        [Category("Gates / 星门")]
        [DisplayName("Normal / 普通")]
        public Color NormalGateColour { get; set; }

        [Category("Systems / 星系")]
        [DisplayName("Out of Region / 区域外")]
        public Color OutRegionSystemColour { get; set; }

        [Category("Systems / 星系")]
        [DisplayName("Out of Region Text / 区域外文本")]
        public Color OutRegionSystemTextColour { get; set; }

        [Category("Popup / 悬浮窗")]
        [DisplayName("Background / 背景")]
        public Color PopupBackground { get; set; }

        [Category("Popup / 悬浮窗")]
        [DisplayName("Text / 文本颜色")]
        public Color PopupText { get; set; }

        [Category("General / 常规")]
        [DisplayName("Region Marker Zoomed / 星域标记(缩放)")]
        public Color RegionMarkerTextColour { get; set; }

        [Category("General / 常规")]
        [DisplayName("Region Marker / 星域标记")]
        public Color RegionMarkerTextColourFull { get; set; }

        [Category("General / 常规")]
        [DisplayName("Selected System / 选中星系")]
        public Color SelectedSystemColour { get; set; }

        [Category("General / 常规")]
        [DisplayName("System Popup / 星系悬浮窗")]
        public bool ShowSystemPopup { get; set; }

        [Category("SOV / 主权")]
        [DisplayName("Structure Vulnerable / 建筑脆弱")]
        public Color SOVStructureVulnerableColour { get; set; }

        [Category("SOV / 主权")]
        [DisplayName("Structure Vulnerable Soon / 建筑即将脆弱")]
        public Color SOVStructureVulnerableSoonColour { get; set; }

        [Category("Systems / 星系")]
        [DisplayName("Outline / 轮廓")]
        public Color SystemOutlineColour { get; set; }

        [Category("Systems / 星系")]
        [DisplayName("Name Size / 名称大小")]
        public int SystemTextSize { get; set; }

        [Category("Systems / 星系")]
        [DisplayName("Name Subtext Size / 名称副文本大小")]
        public int SystemSubTextSize { get; set; }

        [Category("Wormholes / 虫洞")]
        [DisplayName("Thera Entrance (Region) / Thera入口(星域)")]
        public Color TheraEntranceRegion { get; set; }

        [Category("Wormholes / 虫洞")]
        [DisplayName("Thera Entrance (System) / Thera入口(星系)")]
        public Color TheraEntranceSystem { get; set; }

        [Category("Wormholes / 虫洞")]
        [DisplayName("Turnur Entrance (System) / Turnur入口(星系)")]
        public Color ThurnurEntranceSystem { get; set; }

        [Category("Wormholes / 虫洞")]
        [DisplayName("Turnur Entrance (Region) / Turnur入口(星域)")]
        public Color ThurnurEntranceRegion { get; set; }

        [Browsable(false)]
        public bool UserEditable { get; set; }

        [Category("Zkill / 击杀板")]
        [DisplayName("Data Overlay / 数据覆盖层")]
        public Color ZKillDataOverlay { get; set; }

        public static Color GetSecStatusColour(double secStatus, bool GradeTrueSec)
        {
            Color secCol = (Color)ColorConverter.ConvertFromString("#FF8D3264");

            if(GradeTrueSec && secStatus < 0.0)
            {
                secCol.R = (byte)(28 + (1.0 - (secStatus / -1.0)) * 113);
                secCol.G = (byte)(10 + (1.0 - (secStatus / -1.0)) * 40);
                secCol.B = (byte)(20 + (1.0 - (secStatus / -1.0)) * 80);
            }

            if(secStatus > 0.05)
            {
                secCol = (Color)ColorConverter.ConvertFromString("#FF722020");
            }

            if(secStatus > 0.15)
            {
                secCol = (Color)ColorConverter.ConvertFromString("#FFBC1117");
            }

            if(secStatus > 0.25)
            {
                secCol = (Color)ColorConverter.ConvertFromString("#FFCE440F");
            }

            if(secStatus > 0.35)
            {
                secCol = (Color)ColorConverter.ConvertFromString("#FFDC6D07");
            }

            if(secStatus > 0.45)
            {
                secCol = (Color)ColorConverter.ConvertFromString("#FFF3FD82");
            }

            if(secStatus > 0.55)
            {
                secCol = (Color)ColorConverter.ConvertFromString("#FF71E554");
            }

            if(secStatus > 0.65)
            {
                secCol = (Color)ColorConverter.ConvertFromString("#FF60D9A3");
            }

            if(secStatus > 0.75)
            {
                secCol = (Color)ColorConverter.ConvertFromString("#FF4ECEF8");
            }

            if(secStatus > 0.85)
            {
                secCol = (Color)ColorConverter.ConvertFromString("#FF3A9AEB");
            }

            if(secStatus > 0.95)
            {
                secCol = (Color)ColorConverter.ConvertFromString("#FF2C74E0");
            }

            return secCol;
        }

        public override string ToString() => Name;
    }
}