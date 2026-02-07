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

        static readonly string[] strings = { "You vomit!", "You vomit {{R sequence|blood!}}" };
        public static void Water(ref int Water) => Water = _WATER;
        public static void Vomit(GameObject Object)
        {
            StringBuilder MessageHolder = new();
            if (Object?.IsPlayer() ?? false)
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
            if (value)
                MessageHolder.Replace(strings[0], strings[1]);
            else
                IComponent<GameObject>.AddPlayerMessage($"{Object.t()} vomits" + " {{R|blood!}}");
        }

        static void CreateVomitObjects(GameObject Object)
        {
            Object.CurrentCell.AddObject("BloodVomitPool");
            if (Object.TryGetEffect<LiquidCovered>(out var e))
            {
                e.Liquid.ComponentLiquids.Remove("putrid");
                e.Liquid.ComponentLiquids.Add("blood", 2);
            }
            LiquidCovered E = new("blood", 2);
            Object.ApplyEffect(E);
            E.Liquid.ComponentLiquids.Remove("putrid");
        }

        static void FindVomitPool(Cell CurrentCell)
        {
            for (int i = CurrentCell.Objects.Count - 1; i >= 0; i--)
            {
                GameObject obj = CurrentCell.Objects[i];
                if ($"{obj}" == "VomitPool")
                    CurrentCell.RemoveObject(obj);
            }

        }
    }
}