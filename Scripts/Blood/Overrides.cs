using XRL;
using Nexus.Blood;
using XRL.World;
using System.Text;
using XRL.UI;
using XRL.World.Effects;
using XRL.World.Parts;
using System.Collections.Generic;
using static XRL.World.Cell;
using Nexus.Core;
using System.Linq;

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
            if (Object.IsPlayer())
                Popup.Show("You vomit {{R sequence|blood!}}");
            bool ExitInterface = false;
            InduceVomitingEvent.Send(Object, ref ExitInterface, MessageHolder);
        }

        public static void VomitEventHandler(GameObject Object, StringBuilder MessageHolder)
        {
            ShowStrings(Object, MessageHolder);
            if (Object.CurrentCell != null && !Object.OnWorldMap())
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
                e.Liquid.ComponentLiquids["blood"] = 2; //was getting a terrible error if the key already existed, dont use .Add!
            }
            else
            {
                LiquidCovered E = new("blood", 2);
                Object.ApplyEffect(E);
                E.Liquid.ComponentLiquids.Remove("putrid");
            }
        }

        static void FindVomitPool(Cell cell)
        {
            var pool = cell.Objects.FirstOrDefault(x => x.Blueprint == "VomitPool");
            if (pool != null)
                cell.RemoveObject(pool);

        }
    }
}