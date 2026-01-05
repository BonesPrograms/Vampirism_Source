using XRL.World.Parts;
using XRL.World.Effects;
using XRL.World;
using XRL.UI;
using System.Collections.Generic;
using System.Linq;

namespace Nexus.Blood
{

    /// <summary>
    /// I found base-game autoget to be inconsistent. This ensures that blood autoget always works.
    /// </summary>
    class Autoget //honestly i just didnt want people to complain that blood autoget doesnt work for my mod when its not my fault... lol...
    {               ///this probably isnt as good/efficient of code as the dev's autoget but it works more consistently
        readonly GameObject Player;
        readonly List<LiquidVolume> PureBlood = new();
        List<GameObject> containers = new();
        const int MAX = 64;
        const string Container = "WaterContainer";
        const string Blood = "blood";
        public Autoget(GameObject Player) => this.Player = Player;

        public void Autogetter()
        {
            containers = Player.Inventory.GetObjectsWithTag(Container);
            if (containers.Count != 0)
            {
                FindBlood();
                if (PureBlood.Count != 0)
                {
                    AddBlood();
               //    SecretlyRearrangeBlood();
                }
            }
        }

        void SecretlyRearrangeBlood() //solution for unsolved issue with my current system where blood is not pooled into a single container but is spread out over all of them
        {
            List<LiquidVolume> pools = new();
            foreach (GameObject obj in containers)
            {
                LiquidVolume part = obj.GetPart<LiquidVolume>();
                if (!part.Sealed && part.ContainsLiquid(Blood) && part.IsPureLiquid() && part.Volume < MAX)
                    pools.Add(part);
            }
            int total = 0;
            foreach (LiquidVolume obj in pools)
            {
                total += obj.Volume;
                obj.UseDrams(obj.Volume);
            }
            int addition = total;
            bool toomuch = total >= MAX;
            while (total > 0)
            {
                foreach (LiquidVolume obj in pools)
                {
                    while (toomuch)
                    {
                        addition = total - MAX <= 0 ? total : total - MAX;
                        obj.AddDrams(Blood, addition);
                       // cmd.msg($"{addition} added while total {total} > 0");
                        total -= addition;
                        if (total <= 0)
                            break;
                    }
                    if (!toomuch && total > 0)
                    {
                        obj.AddDrams(Blood, addition);
                      //  cmd.msg($"{addition} added");
                        total -= addition;
                    }
                }
            }
        }
        void AddBlood()
        {
            foreach (GameObject waterskin in containers)
            {
                if (PureBlood.Count != 0)
                {
                    LiquidVolume Part = waterskin.GetPart<LiquidVolume>();
                    if (!Part.Sealed && Part.Volume < MAX)
                        CheckForStoredLiquids(Part, waterskin);
                }
            }
        }

        void CheckForStoredLiquids(LiquidVolume Part, GameObject Waterskin)
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
        void FindBlood()
        {
            List<Cell> cells = Player?.CurrentCell?.GetLocalAdjacentCells();
            if (cells is not null)
            {
                foreach (Cell cell in cells)
                    if (cell.HasObjectWithPart(nameof(LiquidVolume)))
                        DealWithLiquid(cell);
            }
        }

        void DealWithLiquid(Cell cell)
        {
            List<GameObject> liquids = cell.GetObjectsWithPart(nameof(LiquidVolume));
            foreach (GameObject liquidSource in liquids)
            {
                if (!liquidSource.HasTag(Container) && $"{liquidSource}" != "FangBloodDrop")
                {
                    LiquidVolume part = liquidSource.GetPart<LiquidVolume>();
                    if (part.ContainsLiquid(Blood) && part.IsPureLiquid() && !PureBlood.Contains(part))
                        PureBlood.Add(part);
                }
            }
        }

    }
}