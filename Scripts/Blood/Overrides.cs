using XRL;
using Nexus.Blood;
using XRL.World;
using System.Text;
using XRL.UI;
using XRL.World.Effects;
using XRL.World.Parts;
using System.Collections.Generic;

namespace Nexus.Blood
{
    static class Overrides
    {
        const int _WATER = 35000;
        public static void Water(ref int Water) => Water = _WATER;
        public static void Vomit(GameObject Object)
        {
            StringBuilder MessageHolder = new();
            if (Object.IsPlayer())
                Popup.Show("You vomit {{R sequence|blood!}}");
            bool ExitInterface = false;
            InduceVomitingEvent.Send(Object, ref ExitInterface, MessageHolder);
        }

        public static void VomitEventHandler(GameObject Object, StringBuilder MessageHolder)
        {
            ShowStrings(Object, MessageHolder);
            if (Object?.CurrentCell is not null && !Object.OnWorldMap())
            {
                FindVomitPool(Object.CurrentCell);
                CreateVomitObjects(Object);
            }

        }

        static void ShowStrings(GameObject Object, StringBuilder MessageHolder)
        {
            bool value = Object.IsPlayer();
            string[] strings = { value ? "You vomit!" : "vomits everywhere!", value ? "You vomit {{R sequence|blood!}}" : "vomits {{R sequence|blood!}}" };
            MessageHolder.Replace(strings[0], strings[1]);
        }

        static void CreateVomitObjects(GameObject Object)
        {
            Object.CurrentCell.AddObject("BloodVomitPool");
            LiquidCovered e = new("blood", 2);
            Object.ApplyEffect(e);
            e.Liquid.ComponentLiquids.Remove("putrid");
        }

        static void FindVomitPool(Cell CurrentCell)
        {
            List<GameObject> pools = CurrentCell.GetObjectsWithPart(nameof(LiquidVolume));
            foreach (GameObject pool in pools)
                if ($"{pool}" == "VomitPool")
                    CurrentCell.RemoveObject(pool);
        }
    }
}