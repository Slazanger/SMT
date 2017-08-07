using System.ComponentModel;
using System.Windows.Media;

namespace SMT
{
    public class MapColours
    {
        public override string ToString()
        {
            return Name;
        }

        [Browsable(false)]
        public string Name { get; set; }

        [Browsable(false)]
        public bool UserEditable { get; set; }

        [Category("Jump Bridges")]
        [DisplayName("Friendly")]
        public Color FriendlyJumpBridgeColour { get; set; }

        [Category("Jump Bridges")]
        [DisplayName("Hostile ")]
        public Color HostileJumpBridgeColour { get; set; }

        [Category("Systems")]
        [DisplayName("Name Size")]
        public int SystemTextSize { get; set; }

        [Category("Systems")]
        [DisplayName("Outline")]
        public Color SystemOutlineColour { get; set; }

        [Category("Systems")]
        [DisplayName("In Region")]
        public Color InRegionSystemColour { get; set; }

        [Category("Systems")]
        [DisplayName("In Region Text")]
        public Color InRegionSystemTextColour { get; set; }

        [Category("Systems")]
        [DisplayName("Out of Region")]
        public Color OutRegionSystemColour { get; set; }

        [Category("Systems")]
        [DisplayName("Out of Region Text")]
        public Color OutRegionSystemTextColour { get; set; }

        [Category("Gates")]
        [DisplayName("Normal")]
        public Color NormalGateColour { get; set; }

        [Category("Gates")]
        [DisplayName("Constellation")]
        public Color ConstellationGateColour { get; set; }

        [Category("General")]
        [DisplayName("Map Background")]
        public Color MapBackgroundColour { get; set; }

        [Category("General")]
        [DisplayName("Selected System")]
        public Color SelectedSystemColour { get; set; }

        [Category("General")]
        [DisplayName("System Popup")]
        public bool ShowSystemPopup { get; set; }

        [Category("Character")]
        [DisplayName("Highlight")]
        public Color CharacterHighlightColour { get; set; }

        [Category("Character")]
        [DisplayName("Text")]
        public Color CharacterTextColour { get; set; }

        [Category("Character")]
        [DisplayName("Text Size")]
        public int CharacterTextSize { get; set; }

        [Category("ESI Data")]
        [DisplayName("Overlay")]
        public Color ESIOverlayColour { get; set; }

        [Category("Intel")]
        [DisplayName("Overlay")]
        public Color IntelOverlayColour { get; set; }

        static public Color GetSecStatusColour(double secStatus)
        {
            /*
                #FF2FEFEF	1.0
                #FF48F0C0	0.9
                #FF00EF47	0.8
                #FF00F000	0.7
                #FF8FEF2F	0.6
                #FFEFEF00	0.5
                #FFD77700	0.4
                #FFF06000	0.3
                #FFF04800	0.2
                #FFD73000	0.1
                #FFF00000	0.0
            */

            Color secCol = (Color)ColorConverter.ConvertFromString("#FFF00000");

            if (secStatus > 0.1)
            {
                secCol = (Color)ColorConverter.ConvertFromString("#FFD73000");
            }

            if (secStatus > 0.2)
            {
                secCol = (Color)ColorConverter.ConvertFromString("#FFF04800");
            }

            if (secStatus > 0.3)
            {
                secCol = (Color)ColorConverter.ConvertFromString("#FFF06000");
            }

            if (secStatus > 0.4)
            {
                secCol = (Color)ColorConverter.ConvertFromString("#FFD77700");
            }

            if (secStatus > 0.5)
            {
                secCol = (Color)ColorConverter.ConvertFromString("#FFEFEF00");
            }

            if (secStatus > 0.6)
            {
                secCol = (Color)ColorConverter.ConvertFromString("#FF8FEF2F");
            }

            if (secStatus > 0.7)
            {
                secCol = (Color)ColorConverter.ConvertFromString("#FF00F000");
            }

            if (secStatus > 0.8)
            {
                secCol = (Color)ColorConverter.ConvertFromString("#FF00EF47");
            }

            if (secStatus > 0.9)
            {
                secCol = (Color)ColorConverter.ConvertFromString("#FF48F0C0");
            }

            if (secStatus > 0.99)
            {
                secCol = (Color)ColorConverter.ConvertFromString("#FF2FEFEF");
            }

            return secCol;
        }
    }
}