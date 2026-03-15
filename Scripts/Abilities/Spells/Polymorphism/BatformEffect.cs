using System;

using VampirismSys.Core;
using VampirismSys.Rules;

using XRL.World.Parts.Mutation;

namespace XRL.World.Effects
{

    [Serializable]
    public class BatformEffect : BasePolymorphEffect
    {
        int CurrentWingLevel;
        bool AlreadyHadWings;
        bool WasLessThanTen;

        public BatformEffect() : base()
        {
            Blueprint = GameObjectFactory.Factory.GetBlueprint("Bat");
            TargetFaction = Batform.FACTION;
            FactionFeeling = 100;

        }

        public override bool WantEvent(int ID, int cascade)
        {
            if (ID == BeforeRenderEvent.ID && !UI.Options.GetOptionBool(ModOptions.NIGHTBEAST)) //because nightbeast already does this for you
                return true;
            return base.WantEvent(ID, cascade);
        }
        public override bool HandleEvent(BeforeRenderEvent E)
        {
            AddLight(21, LightLevel.Dimvision);
            return base.HandleEvent(E);
        }

        public override void Transform()
        {
            base.Transform();
            Suppress(true);
            ChangeWings();
            CommandEvent.Send(Object, Wings.COMMAND_NAME);
            Suppress(false);
        }
        public override void Revert()
        {
            base.Revert();
            RevertWings();
        }
        void RevertWings()
        {
            if (!AlreadyHadWings)
                base.Object.RemoveMutation<Wings>();
            else if (WasLessThanTen)
            {
                var Wings = base.Object.GetPart<Wings>();
                Wings.BaseLevel = CurrentWingLevel;
                Wings.CapOverride = -1;
            }
        }

        void ChangeWings()
        {
            if (base.Object.TryGetMutation<Wings>(out var Wings))
            {
                HadWings(Wings);
            }
            else
            {
                AlreadyHadWings = false;
                var wings = base.Object.AddMutation<Wings>(10);
                wings.CapOverride = 10;
            }
        }

        void HadWings(Wings Wings)
        {
            AlreadyHadWings = true;
            if (Wings.Level < 10)
            {
                WasLessThanTen = true;
                CurrentWingLevel = Wings.BaseLevel;
                Wings.BaseLevel = 10;
                Wings.CapOverride = 10;
            }
        }
    }
}