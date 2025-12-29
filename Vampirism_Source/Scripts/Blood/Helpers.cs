using XRL;
using Nexus.Blood;
using XRL.World;
using System.Text;
using XRL.UI;
using XRL.World.Effects;
using XRL.World.Parts;
using System;

namespace Nexus.Blood
{
    public static class Helpers
    {
        const int _WATER_THRESH = 35000;
        public static void ResetWater(ref int Water) => Water = Water < _WATER_THRESH ? _WATER_THRESH : Water;
        public static void Vomit(GameObject Object)
        {
            StringBuilder MessageHolder = new();
            Popup.Show("You vomit {{R sequence|blood!}}");
            bool ExitInterface = false;
            InduceVomitingEvent.Send(Object, ref ExitInterface, MessageHolder);
        }

        public static void VomitEventHandler(GameObject Object, StringBuilder MessageHolder)
        {
            ShowStrings(Object, MessageHolder);
            if (Object.CurrentCell is not null && !Object.OnWorldMap())
            {
                FindVomitPool(Object.CurrentCell);
                CreateVomitObjects(Object);
            }

        }

     static void ShowStrings(GameObject Object, StringBuilder MessageHolder)
        {
            if (Object.IsPlayer())
                MessageHolder.Replace("You vomit!", "You vomit {{R sequence|blood!}}");
            else
                MessageHolder.Replace("vomits everywhere!", "vomits {{R sequence|blood!}}");
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
            foreach (GameObject pool in CurrentCell.GetObjectsWithPart(nameof(LiquidVolume)))
                if ($"{pool}" is "VomitPool")
                    CurrentCell.RemoveObject(pool);
        }
    }
}