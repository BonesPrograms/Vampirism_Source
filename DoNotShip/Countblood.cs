using XRL;
using XRL.Wish;
using XRL.World;
using XRL.World.Parts;
using XRL.World.Effects;
using System.Collections.Generic;
using System.Linq;

namespace Nexus.Wish
{

    [HasWishCommand]
    static class CountWish
    {
        [WishCommand("countblood")]

        public static void CommandCountBlood()
        {
            GameObject obj = The.Player;
            Count wish = new(obj);
            wish.Sort();
            wish.SortInventory();
            wish.Display();
        }
    }

    class Count
    {
        static int count = 0;
        Dictionary<LiquidVolume, int> purepools = new();

        Dictionary<LiquidVolume, int> badpools = new();

        Dictionary<LiquidVolume, int> inventorypools = new();
        List<GameObject> objects = new();

        List<GameObject> inventory = new();
        GameObject Object;
        int compound;
        int badcompound;
        int inventorycompound;
        public Count(GameObject Object)
        {
            this.Object = Object;
        }
        public void Display()
        {
            count++;
            string Count = $"\n{count}\n";
            cmd.msg(Count);
            MetricsManager.LogInfo(Count);
            cmd.msg($"{compound} COMPOUND PURE VALUE");
            cmd.msg($"{badcompound} BAD COMPOUND");
            cmd.msg($"{inventorycompound} INVENTORY COMPOUND");
            MetricsManager.LogInfo($"{compound} COMPOUND PURE VALUE");
            MetricsManager.LogInfo($"{badcompound} BAD COMPOUND");
            MetricsManager.LogInfo($"{inventorycompound} INVENTORY COMPOUND");
            cmd.msg("\nPUREPOOLS IN ZONE");
            MetricsManager.LogInfo("\nPUREPOOLS IN ZONE");
            foreach (var obj in purepools)
            {
                cmd.msg($"{obj.Key} key, {obj.Value} vol, {obj.Key?.ParentObject} parentobject");
                MetricsManager.LogInfo($"{obj.Key} key, {obj.Value} vol, {obj.Key?.ParentObject} parentobject");
            }
            MetricsManager.LogInfo("\nINVENTORY POOLS");
            cmd.msg("\nINVENTORY POOLS");
            foreach (var obj in inventorypools)
            {
                cmd.msg($"{obj.Key} key, {obj.Value} vol, {obj.Key?.ParentObject} parentobject");
                MetricsManager.LogInfo($"{obj.Key} key, {obj.Value} vol, {obj.Key?.ParentObject} parentobject");
            }
        }

        public void SortInventory()
        {
            inventory = Object.Inventory.GetObjectsWithTag("WaterContainer");
            foreach (var obj in inventory)
            {
                LiquidVolume volume = obj.GetPart<LiquidVolume>();
                if (volume.IsPureLiquid() && volume.ContainsLiquid("blood"))
                {
                    inventorypools.Add(volume, volume.Volume);
                    inventorycompound += volume.Volume;
                }
            }
        }
        public void Sort()
        {
            objects = Object.CurrentZone.GetObjectsWithPart(nameof(LiquidVolume));
            objects.ForEach(x =>
            {
                LiquidVolume volume = x.GetPart<LiquidVolume>();
                if (volume.IsPureLiquid() && volume.ContainsLiquid("blood"))
                {
                    purepools.Add(volume, volume.Volume);
                    compound += volume.Volume;
                }
                else if (volume.ContainsLiquid("blood") && !volume.IsPureLiquid())
                {
                    badpools.Add(volume, volume.Volume);
                    badcompound += volume.Volume;
                }
            });
        }
    }

}