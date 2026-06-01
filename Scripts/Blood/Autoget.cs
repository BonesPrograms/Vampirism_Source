using XRL.World.Parts;
using XRL.World;
using System.Collections.Generic;
using VampirismSys.Extensions;
using XRL;
using System.Linq;
using System;
using XRL.World.Parts.Mutation;

namespace VampirismSys.Blood
{

    /// <summary>
    /// I found base-game autoget to be inconsistent. This ensures that blood autoget always works.
    /// </summary>
    [HasGameBasedStaticCache]
    public static class Autoget //honestly i just didnt want people to complain that blood autoget doesnt work for my mod when its not my fault... lol...
    {               ///this probably isnt as good/efficient of code as the dev's autoget but it works more consistently
        static GameObject Player => The.Player; //according to recent reports it also blows up peoples game more consistently...

        [GameBasedStaticCache(false)]
        static List<LiquidVolume> PureLiquid = new();

        [GameBasedStaticCache(false, true)]
        static List<LiquidVolume> Containers = new();

        const string Container = "WaterContainer";

        const string LiquidType = "blood";

        static bool FoundBlood => PureLiquid.Count > 0;

        public static void Clear()
        {
            PureLiquid = new();
            Containers = new();
        }
        public static void Autogetter()
        {
            ValidateCache();
            if (Containers.Count > 0)
            {
                FindBlood();
                if (FoundBlood)
                {
                    AddBlood();
                    PureLiquid = new();
                }
            }
        }
        static void ValidateCache()
        {
            var inventory = Player.Inventory.Objects;
            Containers = inventory.Where(x => x != null && CheckTag(x.GetBlueprint())).Select(x => x.GetPart<LiquidVolume>()).ToList();
        } //if you dont resync this every turn then containers will go null
        static bool CheckTag(GameObjectBlueprint blueprint)
        {
            bool valid = false;
            foreach (var obj in blueprint.Tags.Keys)
            {
                if (obj == "HiddenInInventory")
                    return false;
                if (obj == Container)
                    valid = true;
            }
            return valid;
        }


        //dont worry about this crazy shit
        //i havent deleted it just incase it needs to make a return tho
        //however this issue isnt a thing i notice anymore
        //this code barely worked anyways its trash can you even really figure out whats going on at a first glance?

        // void SecretlyRearrangeBlood() //solution for unsolved issue with my current system where blood is not pooled into a single container but is spread out over all of them
        // {
        //     List<LiquidVolume> pools = new();
        //     foreach (GameObject obj in containers)
        //     {
        //         LiquidVolume part = obj.GetPart<LiquidVolume>();
        //         if (!part.Sealed && part.ContainsLiquid(Blood) && part.IsPureLiquid() && part.Volume < MAX)
        //             pools.Add(part);
        //     }
        //     int total = 0;
        //     foreach (LiquidVolume obj in pools)
        //     {
        //         total += obj.Volume;
        //         obj.UseDrams(obj.Volume);
        //     }
        //     int addition = total;
        //     bool toomuch = total >= MAX;
        //     while (total > 0)
        //     {
        //         foreach (LiquidVolume obj in pools)
        //         {
        //             while (toomuch)
        //             {
        //                 addition = total - MAX <= 0 ? total : total - MAX;
        //                 obj.AddDrams(Blood, addition);
        //                 // cmd.msg($"{addition} added while total {total} > 0");
        //                 total -= addition;
        //                 if (total <= 0)
        //                     break;
        //             }
        //             if (!toomuch && total > 0)
        //             {
        //                 obj.AddDrams(Blood, addition);
        //                 //  cmd.msg($"{addition} added");
        //                 total -= addition;
        //             }
        //         }
        //     }
        // }
        static void AddBlood()
        {
            // Containers.Where(x => x?.ParentObject == null).SafeForEach(x => Containers.Remove(x));
            Containers.TakeWhile(x => FoundBlood).Where(WantsToBeFilled).ForEach(x => FillContainer(x, x.ParentObject));
        }
        static bool WantsToBeFilled(LiquidVolume x)
         =>
            !x.Sealed
            && x.Volume < x.MaxVolume
            && (x.Volume == 0 || (x.ContainsLiquid(LiquidType) && x.IsPureLiquid()));
        static void FillContainer(LiquidVolume part, GameObject waterskin)
        {
            LiquidVolume pool = PureLiquid.GetRandomElement();
            if (pool.Volume > 0)
            {
                bool overflowing = CheckForOverflow(pool, part, out int deduction);
                if (overflowing && deduction > 0)
                    Collect(pool, part, waterskin, deduction);
                else if (!overflowing)
                    Collect(pool, part, waterskin, pool.Volume);
            }
            PureLiquid.Remove(pool);
        }

        //Remove(Pool) is a solution to an issue where bloodpools were being double-collected from
        //for some reason their updated volume isnt being heard, it has the same volume and they arent removed when at volume 0
        //so when it GetsRandomElement it has a chance to get a duplicate of the pool you just collected from
        //not sure if its an issue associated with the foreach over all this
        //(This is an old issue before I started using LINQ and havent debugged it since i fixed it because working on autoget isnt very fun) 
        //(I am really bad at math and holding numbers in my head)

        //pool vol of 10
        //part vol of 60
        //60+10 = 70
        //70 >= 64
        //64 - 60 = 4

        //pool vol of 5
        //part vol of 60
        //60+5 = 66
        //66 >= 64
        //64 - 60 = 4

        ///pool vol 150
        /// part vol 60
        /// 150+60 = 210
        /// 210 >= 64
        /// 64 - 60 = 4


        //yeah im really bad at math i had to proof and re-code this like 10 times

        static bool CheckForOverflow(LiquidVolume pool, LiquidVolume part, out int deduction)
        {
            if (pool.Volume + part.Volume >= part.MaxVolume)
            {
                deduction = part.MaxVolume - part.Volume;
                return true;
            }
            else
            {
                deduction = default;
                return false;
            }
        }

        static void Collect(LiquidVolume pool, LiquidVolume part, GameObject waterskin, int deduction)
        {
            part.AddDrams(LiquidType, deduction);
            pool.UseDrams(deduction);
            IComponent<GameObject>.AddPlayerMessage("You collect " + deduction + " drams of {{r|blood}} " + "in your " + waterskin.ShortDisplayName + ".");
            //if (Pool?.Volume is null || Pool.Volume <= 0 || Pool.IsEmpty())
            //      PureBlood.Remove(Pool);
        }
        static void FindBlood()
        {
            if (Player.LocalCells(out var cells))
            {
                var objs = cells.Where(x => x.HasObjectWithPart(nameof(LiquidVolume))).SelectMany(x => x.Objects);
                DealWithLiquid(objs);
            }
        }
        static void DealWithLiquid(IEnumerable<GameObject> objects)
        {
            var foundBlood = objects.Select(x => x?.GetPart<LiquidVolume>()).Where(WantsToBeCollected);
            PureLiquid = new(foundBlood);
        }
        static bool WantsToBeCollected(LiquidVolume x)
          =>
            x != null
            && x.ContainsLiquid(LiquidType)
            && x.IsPureLiquid()
            && IsLiquidPool(x.ParentObject.GetBlueprint());

        static bool IsLiquidPool(GameObjectBlueprint x) => x.InheritsFrom("Water") && x.HasTag("Pool") && x.Name != "FangBloodDrop";

    }
}