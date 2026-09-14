namespace EVEData.Utils.EveOPreview
{
    public class EveOAlertClient
    {
        public const string MessageType = "AlertClient";
        public const int AlertTypeIntel = 0;
        public const int AlertTypeKill = 1;
        public const int AlertTypeDecloak = 2;
        public const int AlertTypeFaction = 3;
        public const int AlertTypeMiningOver = 4;

        public string Client { get; set; }
        public string SystemName { get; set; }
        public int AlertType { get; set; }
        public int AlertJumps { get; set; }
        public string AlertText { get; set; }
    }
}
