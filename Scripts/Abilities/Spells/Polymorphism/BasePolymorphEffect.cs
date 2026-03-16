using System;
using VampirismSys.Core;
using XRL.World.Parts;
using System.Collections.Generic;
using System.Reflection;

namespace XRL.World.Effects
{

    [Serializable]
    public abstract class BasePolymorphEffect : IBeastScribedEffect
    {
        protected GameObjectBlueprint Blueprint { get => _blueprint; init { _blueprint = value; } }
        protected string FormName { get => _formName; init { _formName = value; } }
        protected string TargetFaction { get => _targetFaction; init { _targetFaction = value; } }
        protected int FactionFeeling { get => _factionFeeling; init { _factionFeeling = value; } }

        int _factionFeeling;

        string _targetFaction;

        string _formName;

        string _oldTile;

        string _oldDisplayName;

        string _oldColorString;

        string _oldRenderString;

        string _originalBlueprint;

        string _lastDescriptionShort;

        [NonSerialized]
        GameObjectBlueprint _blueprint;

        List<GameObject> EquippedObjects;

        GameObject PreservedObject;

        public BasePolymorphEffect()
        {
            Duration = 9999;
        }

        void VerifyInitialization()
        {
            if (Blueprint == null)
                throw new Exception($"Blueprint not assigned in {GetType().Name}'s constructor!");
            if (!Blueprint.DescendsFrom("Creature"))
                throw new Exception($"Blueprint assigned in {GetType().Name} does not descend from Creature!");
        }
        public override sealed bool Apply(GameObject Object)
        {
            VerifyInitialization();
            Transform();
            return true;
        }

        public override sealed void Remove(GameObject Object)
        {
            Revert();
        }

        //Always call base.Revert and base.Transform FIRST, before any of your additional modifications
        //such as adding new parts, mutations, firing events, or whatever it is you want to do
        public virtual void Transform()
        {
            Suppress(true);
            EquippedObjects = UnequipAndGet();
            SaveLook();
            ChangeLook();
            ChangeBody();
            ChangeDescription();
            ChangeBlueprint();
            Object.Body.UpdateBodyParts();
            AutoEquip();
            Suppress(false);
            VerifyFormName();
            AddPlayerMessage($"You assume the form of a {FormName}.");
            base.Object.ParticleBlip("&K-", 10, 0L);
            if (TargetFaction != null)
                base.Object.Brain.AddFactionFeeling(TargetFaction, FactionFeeling);
        }
        public virtual void Revert()
        {
            Suppress(true);
            Unequip();
            RevertLook();
            RevertDescription();
            RevertBlueprint();
            RevertBody();
            TryReEquip();
            Suppress(false);
            AddPlayerMessage("You revert to your true form.");
            base.Object.ParticleBlip("&K-", 10, 0L);
            if (TargetFaction != null)
                base.Object.Brain.SubtractFactionFeeling(TargetFaction, FactionFeeling);
        }

        #region Transformation

        void VerifyFormName()
        {
            if (FormName.IsNullOrEmpty())
            {
                _formName = Object.DisplayName;
                MetricsManager.LogModInfo(ModManager.GetMod("vampirism"), $"{GetType().Name}: FormName not assigned, defaulting to displayname.");
            }
        }
        void ChangeLook()
        {
            GamePartBlueprint render = Blueprint.GetPart(nameof(Parts.Render));
            base.Object.DisplayName = render.GetParameterString("DisplayName");
            base.Object.Render.Tile = render.GetParameterString("Tile");
            base.Object.Render.ColorString = render.GetParameterString("TileColor");
            base.Object.Render.RenderString = render.GetParameterString("DetailColor");
        }
        void SaveLook()
        {
            _oldColorString = base.Object.Render.ColorString;
            _oldRenderString = base.Object.Render.RenderString;
            _oldTile = base.Object.Render.Tile;
            _oldDisplayName = base.Object.DisplayName;
        }

        void ChangeBody()
        {
            PreservedObject = Object.DeepCopy(CopyID: true);
            base.Object.Body.Anatomy = Blueprint.GetPartParameter<string>(nameof(Body), "Anatomy");
        }

        void ChangeBlueprint()
        {
            _originalBlueprint = Object.Blueprint;
            Object.SetBlueprint(Blueprint); //final piece of the puzzle, this allows you to get bat sounds which are stored as tags and only accessible through their blueprint
        }

        void ChangeDescription()
        {
            if (VerifyObject())
            {
                var description = base.Object.GetPart<Description>();
                _lastDescriptionShort = description.Short;
                description.Short = Blueprint.GetPartParameter<string>(nameof(Description), "Short");
            }
        }

        void AutoEquip()
        {
            foreach (var obj in EquippedObjects)
                if (obj != null)
                    Object.AutoEquip(obj);
        }

        void TryReEquip()
        {
            for (int i = 0; i < EquippedObjects.Count; i++)
            {
                GameObject obj = EquippedObjects[i];
                if (VerifyHasObject(obj))
                {
                    Object.AutoEquip(obj);
                }
            }
        }
        List<GameObject> UnequipAndGet()
        {
            List<GameObject> equipped = new();
            Object.SafeForeachEquippedObject(delegate (GameObject obj)
            {
                if (obj != null)
                {
                    equipped.Add(obj);
                    obj.ForceUnequip(true);
                }
            });
            return equipped;
        }

        #endregion

        #region Reversion

        void RevertLook()
        {
            base.Object.Render.ColorString = _oldColorString;
            base.Object.Render.RenderString = _oldRenderString;
            base.Object.Render.Tile = _oldTile;
            base.Object.DisplayName = _oldDisplayName;
        }

        void RevertBody()
        {

            Object.Body = null;
            Object.RemovePart<Body>();
            Object.Body = Object.AddPart(PreservedObject.Body);
            PreservedObject.Body.ParentObject = Object;
            PreservedObject.Body = null;
            PreservedObject = null;
            Object.Body.UpdateBodyParts();
        }

        void RevertDescription()
        {
            if (VerifyObject())
            {
                var Description = base.Object.GetPart<Description>();
                Description.Short = _lastDescriptionShort;
            }
        }

        void RevertBlueprint()
        {
            Object.SetBlueprint(GameObjectFactory.Factory.Blueprints[_originalBlueprint]);
        }

        bool VerifyHasObject(GameObject obj)
        {
            return obj != null && Object.Inventory.InventoryContains(obj);//|| currentlyEquipped.Contains(obj);
        }

        void Unequip()
        {
            List<GameObject> currentlyEquipped = Object.GetEquippedObjects();
            for (int i = 0; i < currentlyEquipped.Count; i++)
                currentlyEquipped[i].ForceUnequip(true);
        }

        #endregion

        bool VerifyObject()
        {
            DeathHandler.Security();
            return base.Object != DeathHandler.Player; //so that we do not reset your description, only npc body descriptions
        }

        public static void Suppress(bool value)
        {
            UI.Popup.Suppress = value;
            Messages.MessageQueue.Suppress = value;
        }

    }
}