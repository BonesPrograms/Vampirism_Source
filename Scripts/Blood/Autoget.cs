using XRL.World.Parts;
using XRL.World;
using System.Collections.Generic;
using Nexus.Core;
using XRL;

namespace Nexus.Blood
{

    /// <summary>
    /// I found base-game autoget to be inconsistent. This ensures that blood autoget always works.
    /// </summary>
    [HasGameBasedStaticCache]
    public static class Autoget //honestly i just didnt want people to complain that blood autoget doesnt work for my mod when its not my fault... lol...
    {               ///this probably isnt as good/efficient of code as the dev's autoget but it works more consistently
        static GameObject Player => The.Player;

        [GameBasedStaticCache(false)]
        public static HashSet<LiquidVolume> PureBlood;

        [GameBasedStaticCache(false, true)]
        public static GameObject[] ContainerCache = new GameObject[0];
        const int MAX = 64;
        const string Container = "WaterContainer";
        const string Blood = "blood";
        public static void Autogetter()
        {
            ValidateCache();
            if (ContainerCache.Length > 0)
            {
                FindBlood();
                if (PureBlood != null)
                {
                    AddBlood();
                    PureBlood = null;
                    //    SecretlyRearrangeBlood();
                }
            }
        }
        static void ValidateCache()
        {
            int value = 0;
            for (int i = 0; i < Player.Inventory.Objects.Count; i++)
            {
                GameObject obj = Player.Inventory.Objects[i];
                if (CheckTag(obj.GetBlueprint()))
                    value++;
            }
            if (value != ContainerCache.Length) //reduces our need to reset or re-instance the containers list over and over, which i expect to not change often
            {
                ContainerCache = new GameObject[value];
                int index = 0;
                for (int i = 0; i < Player.Inventory.Objects.Count; i++)
                {
                    GameObject obj = Player.Inventory.Objects[i];
                    if (CheckTag(obj.GetBlueprint()))
                    {
                        ContainerCache[index] = obj;
                        index++;
                    }
                    if (index >= ContainerCache.Length)
                        break;
                }
            }
        }

        static bool CheckTag(GameObjectBlueprint blueprint)
        {
            bool hidden = false;
            bool container = false;
            foreach (var obj in blueprint.Tags)
            {
                if (obj.Key == "HiddenInInventory")
                    hidden = true;
                if (obj.Key == Container)
                    container = true;
            }
            return container && !hidden;
        }

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

            for (int i = 0; i < ContainerCache.Length; i++)
            {
                if (PureBlood.Count > 0)
                {
                    GameObject container = ContainerCache[i];
                    LiquidVolume Part = container.GetPart<LiquidVolume>();
                    if (!Part.Sealed && Part.Volume < MAX)
                        CheckForStoredLiquids(Part, container);
                }
                else
                    break;
            }
        }

        static void CheckForStoredLiquids(LiquidVolume Part, GameObject Waterskin)
        {
            if ((Part.ContainsLiquid(Blood) && Part.IsPureLiquid()) || Part.Volume == 0)
            {
                LiquidVolume Pool = PureBlood.GetRandomElement();
                if (Pool.Volume > 0)
                {
                    bool math = Math(Pool, Part, out int deduction);
                    if (math && deduction > 0)
                        Collect(Pool, Part, Waterskin, deduction);
                    else if (!math)
                        Collect(Pool, Part, Waterskin, Pool.Volume);
                }
                PureBlood.Remove(Pool);
            }
        }

        //Remove(Pool) is a solution to an issue where bloodpools were being double-collected from
        //for some reason their updated volume isnt being heard, it has the same volume and they arent removed when at volume 0
        //so when it GetsRandomElement it has a chance to get a duplicate of the pool you just collected from
        //not sure if its an issue associated with the foreach over all this or maybe i should make some integer instances or a dictionary with ints

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

        static bool Math(LiquidVolume Pool, LiquidVolume Part, out int deduction)
        {
            if (Pool.Volume + Part.Volume >= MAX)
            {
                deduction = MAX - Part.Volume;
                return true;
            }
            else
            {
                deduction = default;
                return false;
            }
        }

        static void Collect(LiquidVolume Pool, LiquidVolume Part, GameObject Waterskin, int deduction)
        {
            Part.AddDrams(Blood, deduction);
            Pool.UseDrams(deduction);
            IComponent<GameObject>.AddPlayerMessage("You collect " + deduction + " drams of {{r|blood}} " + "in your " + Waterskin.ShortDisplayName + ".");
            //if (Pool?.Volume is null || Pool.Volume <= 0 || Pool.IsEmpty())
            //      PureBlood.Remove(Pool);
        }
        static void FindBlood()
        {

            if (Player.LocalCells(out var cells))
            {
                for (int i = 0; i < cells.Count; i++)
                    if (cells[i].HasObjectWithPart(nameof(LiquidVolume)))
                        DealWithLiquid(cells[i]);
            }
        }

        static void DealWithLiquid(Cell cell)
        {
            for (int i = 0; i < cell.Objects.Count; i++)
            {
                GameObject liquidSource = cell.Objects[i];
                if (!liquidSource.HasTag(Container) && $"{liquidSource}" != "FangBloodDrop" && liquidSource.TryGetPart<LiquidVolume>(out var part) && part != null && part.ContainsLiquid(Blood) && part.IsPureLiquid())
                {
                    PureBlood ??= new();
                    PureBlood.Add(part);
                }
            }
        }

    }
}