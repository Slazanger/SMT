using Newtonsoft.Json;

namespace ESI.NET.Models.Character
{
    public class Industry
    {
        [JsonProperty("hacking_successes")]
        public long HackingSuccesses { get; set; }

        [JsonProperty("jobs_cancelled")]
        public long JobsCancelled { get; set; }

        [JsonProperty("jobs_completed_copy_blueprint")]
        public long JobsCompletedCopyBlueprint { get; set; }

        [JsonProperty("jobs_completed_invention")]
        public long JobsCompletedInvention { get; set; }

        [JsonProperty("jobs_completed_manufacture")]
        public long JobsCompletedManufacture { get; set; }

        [JsonProperty("jobs_completed_manufacture_asteroid")]
        public long JobsCompletedManufactureAsteroid { get; set; }

        [JsonProperty("jobs_completed_manufacture_asteroid_quantity")]
        public long JobsCompletedManufactureAsteroidQuantity { get; set; }

        [JsonProperty("jobs_completed_manufacture_charge")]
        public long JobsCompletedManufactureCharge { get; set; }

        [JsonProperty("jobs_completed_manufacture_charge_quantity")]
        public long JobsCompletedManufactureChargeQuantity { get; set; }

        [JsonProperty("jobs_completed_manufacture_commodity")]
        public long JobsCompletedManufactureCommodity { get; set; }

        [JsonProperty("jobs_completed_manufacture_commodity_quantity")]
        public long JobsCompletedManufactureCommodityQuantity { get; set; }

        [JsonProperty("jobs_completed_manufacture_deployable")]
        public long JobsCompletedManufactureDeployable { get; set; }

        [JsonProperty("jobs_completed_manufacture_deployable_quantity")]
        public long JobsCompletedManufactureDeployableQuantity { get; set; }

        [JsonProperty("jobs_completed_manufacture_drone")]
        public long JobsCompletedManufactureDrone { get; set; }

        [JsonProperty("jobs_completed_manufacture_drone_quantity")]
        public long JobsCompletedManufactureDroneQuantity { get; set; }

        [JsonProperty("jobs_completed_manufacture_implant")]
        public long JobsCompletedManufactureImplant { get; set; }

        [JsonProperty("jobs_completed_manufacture_implant_quantity")]
        public long JobsCompletedManufactureImplantQuantity { get; set; }

        [JsonProperty("jobs_completed_manufacture_module")]
        public long JobsCompletedManufactureModule { get; set; }

        [JsonProperty("jobs_completed_manufacture_module_quantity")]
        public long JobsCompletedManufactureModuleQuantity { get; set; }

        [JsonProperty("jobs_completed_manufacture_other")]
        public long JobsCompletedManufactureOther { get; set; }

        [JsonProperty("jobs_completed_manufacture_other_quantity")]
        public long JobsCompletedManufactureOtherQuantity { get; set; }

        [JsonProperty("jobs_completed_manufacture_ship")]
        public long JobsCompletedManufactureShip { get; set; }

        [JsonProperty("jobs_completed_manufacture_ship_quantity")]
        public long JobsCompletedManufactureShipQuantity { get; set; }

        [JsonProperty("jobs_completed_manufacture_structure")]
        public long JobsCompletedManufactureStructure { get; set; }

        [JsonProperty("jobs_completed_manufacture_structure_quantity")]
        public long JobsCompletedManufactureStructureQuantity { get; set; }

        [JsonProperty("jobs_completed_manufacture_subsystem")]
        public long JobsCompletedManufactureSubsystem { get; set; }

        [JsonProperty("jobs_completed_manufacture_subsystem_quantity")]
        public long JobsCompletedManufactureSubsystemQuantity { get; set; }

        [JsonProperty("jobs_completed_material_productivity")]
        public long JobsCompletedMaterialProductivity { get; set; }

        [JsonProperty("jobs_completed_time_productivity")]
        public long JobsCompletedTimeProductivity { get; set; }

        [JsonProperty("jobs_started_copy_blueprint")]
        public long JobsStartedCopyBlueprint { get; set; }

        [JsonProperty("jobs_started_invention")]
        public long JobsStartedInvention { get; set; }

        [JsonProperty("jobs_started_manufacture")]
        public long JobsStartedManufacture { get; set; }

        [JsonProperty("jobs_started_material_productivity")]
        public long JobsStartedMaterialProductivity { get; set; }

        [JsonProperty("jobs_started_time_productivity")]
        public long JobsStartedTimeProductivity { get; set; }

        [JsonProperty("reprocess_item")]
        public long ReprocessItem { get; set; }

        [JsonProperty("reprocess_item_quantity")]
        public long ReprocessItemQuantity { get; set; }
    }
}
