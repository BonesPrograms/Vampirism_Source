using XRL.World.Parts;
using XRL.World.Effects;
using XRL.World;
using XRL.UI;
using System.Collections.Generic;

namespace Nexus.Blood
{

    /// <summary>
    /// I found base-game autoget to be inconsistent. This ensures that blood autoget always works.
    /// </summary>
    class Autoget //honestly i just didnt want people to complain that blood autoget doesnt work for my mod when its not my fault... lol...
    {               ///this probably isnt as good/efficient of code as the dev's autoget but it works more consistently
        readonly GameObject Player;
        readonly List<LiquidVolume> PureBlood = new();
        int Volume;
        bool Found;
        const int MAX = 64;
        const string Container = "WaterContainer";
        const string Blood = "blood";
        public Autoget(GameObject Player) => this.Player = Player;

        public void Autogetter()
        {
            FindBlood();
            if (Found is true)
                AddBlood(Player.Inventory.GetObjectsWithTag(Container));

        }

        void ReduceLiquid(int amount)
        {
            foreach (LiquidVolume Part in PureBlood)
                Part.UseDrams(amount);
        }

        void AddBlood(List<GameObject> waterskins)
        {
            foreach (GameObject Waterskin in waterskins)
            {
                LiquidVolume Part = Waterskin.GetPart<LiquidVolume>();
                if (!Part.Sealed)
                    CheckForStoredLiquids(Part, Waterskin);
            }
        }

        void CheckForStoredLiquids(LiquidVolume Part, GameObject Waterskin)
        {
            if (Part.ContainsLiquid(Blood))
            {
                if (Part.IsPureLiquid())
                    CheckFor64(Part, Waterskin);
            }
            else if (Part.Volume == 0)
                CheckVolumeAfterAdd(Part, Waterskin);
        }

        void CheckVolumeAfterAdd(LiquidVolume Part, GameObject Waterskin)
        {
            if (Volume > 0)
            {
                int VolumeAfterAdd = Part.Volume + Volume;
                if (VolumeAfterAdd >= MAX)
                    Collect(Part, Waterskin, Volume - (VolumeAfterAdd - MAX));
                else
                    Collect(Part, Waterskin);
            }
        }

        void Collect(LiquidVolume Part, GameObject Waterskin, int NewVolume)
        {
            Part.AddDrams(Blood, NewVolume);
            ReduceLiquid(NewVolume);
            IComponent<GameObject>.AddPlayerMessage("You collect " + NewVolume + " drams of {{r|blood}} " + "in your " + Waterskin.ShortDisplayName + ".");
            Volume -= NewVolume;
        }

        void Collect(LiquidVolume Part, GameObject Waterskin)
        {
            Part.AddDrams(Blood, Volume);
            ReduceLiquid(Volume);
            IComponent<GameObject>.AddPlayerMessage("You collect " + Volume + " drams of {{r|blood}} " + " in your " + Waterskin.ShortDisplayName + ".");
            Volume = 0;
        }


        void CheckFor64(LiquidVolume Part, GameObject Waterskin)
        {
            if (Part.Volume < MAX)
                CheckVolumeAfterAdd(Part, Waterskin);
        }
        void FindBlood()
        {
            foreach (Cell cell in Player?.CurrentCell?.GetLocalAdjacentCells())
                if (cell.HasObjectWithPart(nameof(LiquidVolume)))
                    DealWithLiquid(cell);
        }

        void DealWithLiquid(Cell cell)
        {
            foreach (GameObject liquidSource in cell.GetObjectsWithPart(nameof(LiquidVolume)))
                if (!liquidSource.HasTag(Container))
                {
                    LiquidVolume Part = liquidSource.GetPart<LiquidVolume>();
                    if (Part.ContainsLiquid(Blood))
                        ExamineLiquid(Part);
                }
        }


        void ExamineLiquid(LiquidVolume Part)
        {
            if (Part.IsPureLiquid())
            {
                Volume += Part.Volume;
                PureBlood.Add(Part);
                Found = true;
            }
        }


    }
}