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
        public string Name;

        [Browsable(false)]
        public bool UserEditable ;


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

        

    }
}
