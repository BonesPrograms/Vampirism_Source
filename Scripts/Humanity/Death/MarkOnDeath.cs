using XRL;
using XRL.World;
using XRL.World.Parts;
using System.Linq;
using System.Collections.Generic;
using VampirismSys.Core;
using VampirismSys.Properties;
using VampirismSys.Extensions;
using VampirismSys.Core;

namespace VampirismSys.Death
{
    internal static class MarkOnDeath //mainly for embrace, but also handles dropping of motes
    {
        internal static bool ShowDebug = false;
        internal static bool FreeMote;

        internal static void Check(GameObject Dying, bool isvampire)
        {
            MarkOnDeath.MarkForEmbrace(Dying, isvampire); //we always have this run even if the player isnt a vampire, incase they become one later on
            if (!isvampire)
                MarkOnDeath.DropMote(Dying);
        }
        static void DropMote(GameObject Dying)
        {
            if (FreeMote || WikiRng.Next(1, 5000) <= 1)
                Dying.CurrentCell.AddObject("MoteOfHumanity");
        }

        static void MarkForEmbrace(GameObject Dying, bool isvampire) //only "feedable" targets can become vampires, but deathhandler only exists as a part on feedable objects, so the check is already done
        {                                   //corpse objects whose source object didnt have this part wont have the property at all and thus will not be embraceable
            var obj = Dying.CurrentCell.Objects.FirstOrDefault(x => x.PropertyEquals("SourceBlueprint", Dying.Blueprint));
            if (obj != null)                                    //i want to note we used to check for SourceID, but not every corpse object has a source id property
                DetermineEmbraceability(obj, Dying, isvampire);
            else if (ShowDebug)
                DebugFailedEmbrace(Dying.CurrentCell);
        }
        static void DebugFailedEmbrace(Cell cell)
        {
            var corpses = cell.Objects.Select(x => x.GetPart<Corpse>()).Where(x => x != null);
            int count = corpses.Count();
            if (count == 0)
                return;
            MetricsManager.LogModError(XRL.ModManager.GetMod("vampirism"), $"Error: Object died, but could not find a corpse object with matching source blueprint in cell.\n Corpse Object Data output to Player.Log. Corpse count: {count}");
            LogCorpses(corpses);
        }

        static void LogCorpses(IEnumerable<Corpse> corpses)
        {
            foreach (var corpse in corpses)
            {
                for (int i = 0; i < 5; i++)
                    MetricsManager.LogInfo("\n");

                MetricsManager.LogInfo($"corpse blueprint: {corpse.CorpseBlueprint}, burnt corpse blueprint: {corpse.BurntCorpseBlueprint}, vaporized corpse blueprint: {corpse.VaporizedCorpseBlueprint}");
                MetricsManager.LogInfo($"{corpse.ParentObject.DisplayName}, {corpse.ParentObject.Blueprint}, {corpse.ParentObject.ID}.\n\nProperties\n");
                corpse.ParentObject.Property.ForEach(x => MetricsManager.LogInfo($"{x.Key}, {x.Value}"));
                MetricsManager.LogInfo($"\n\nIntProperties\n");
                corpse.ParentObject.IntProperty.ForEach(x => MetricsManager.LogInfo($"{x.Key}, {x.Value}"));

            }
        }
        // we had a problem where wished Bears' corpses would not be selected for embraceability the bear having an ID (did you know that bear corpses also have a property that reveals the bear's hidden true name?)
        //bears consistently did not write a sourceID property to their corpse, though rarely they actually would, it is more consistent
        //that they didnt, this also occured with wished snapjaws, so the old check would never find their corpse and skip embrace marking
        //considering that corpses are indiscernible from one another to the player, i realized it doesnt matter anyways which corpse is selected as long as it appears
        //to be the same corpse as the one the object would normally drop (for cases where an object dies on a cell that already has a corpse that matches their corpse blueprint)
        static void DetermineEmbraceability(GameObject obj, GameObject Dying, bool isvampire)
        {
            if (isvampire)
            {
                IComponent<GameObject>.AddPlayerMessage($"{Dying.t()} burns to ashes!");
                obj.SetStringProperty(Flags.Embrace.EMBRACEABLE, Flags.FALSE);
            }
            else if (Dying.TryGetPart(out Corpse corpse))
                CompareBlueprints(Dying, obj, corpse);
        }

        static void CompareBlueprints(GameObject Dying, GameObject obj, Corpse corpse)
        {
            //  if (obj.Blueprint == corpse.CorpseBlueprint)
            if (obj.Blueprint == "Ash" || obj.Blueprint == corpse.BurntCorpseBlueprint || obj.Blueprint == corpse.VaporizedCorpseBlueprint)
                obj.SetStringProperty(Flags.Embrace.EMBRACEABLE, Flags.FALSE);
            else
            {
                obj.SetIntProperty(Flags.Embrace.LEVEL_ON_DEATH, Dying.Level);
                obj.SetStringProperty(Flags.Embrace.EMBRACEABLE, Flags.TRUE);
                obj.AddPart(new EmbraceableObject(Dying));
            }
        }
    }
}