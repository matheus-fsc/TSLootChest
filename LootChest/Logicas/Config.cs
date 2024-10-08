using System;
using Newtonsoft.Json;

namespace LootChest.Logicas
{
    public class Config
    {       
            public bool PositionLog { get; set; } = true;
            public bool CommandAddChest { get; set; } = true;
            public bool CommandRemoveChest { get; set; } = true;
            public bool CanBreakLootChests { get; set; } = false;

        public Config()
        {
            if (System.IO.File.Exists("LootChestCongig.json")){

            }
            else
                  System.IO.File.WriteAllText("LootChestConfig.json", JsonConvert.SerializeObject(this, Formatting.Indented));
            }

            
    }
}
