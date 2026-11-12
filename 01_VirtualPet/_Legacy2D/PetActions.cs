using UnityEngine;

namespace WindPet
{
    /// <summary>
    /// Everything the player can do. UI buttons call these public methods.
    /// Each action has a benefit and a cost, so the player has to choose.
    /// </summary>
    public class PetActions : MonoBehaviour
    {
        public PetStats stats;
        public PetStateMachine fsm;
        public Wallet wallet;

        PetConfig Config => stats.config;

        /// <summary>Fan: free food, but the cold air cools the pet down.</summary>
        public void UseFan()
        {
            if (!fsm.IsAwake) { fsm.Say("zzz..."); return; }
            stats.ChangeHunger(-Config.fanFeed);
            stats.ChangeTemperature(-Config.fanCooling);
            fsm.OnFed();
            fsm.Say(stats.Temperature < Config.comfortMin ? "Yum... but chilly." : "Yum! Fresh air!");
        }

        /// <summary>Hair dryer: food and warmth, but it costs electricity.</summary>
        public void UseHairDryer()
        {
            if (!fsm.IsAwake) { fsm.Say("zzz..."); return; }
            if (!wallet.TrySpend(Config.dryerCost))
            {
                fsm.Say("No money for electricity...");
                return;
            }
            stats.ChangeHunger(-Config.dryerFeed);
            stats.ChangeTemperature(Config.dryerHeating);
            fsm.OnFed();
            fsm.Say(stats.Temperature > Config.comfortMax ? "Hot hot hot!" : "Warm and cosy!");
        }

        /// <summary>Pinwheel: fun and trust, but it tires the pet out.</summary>
        public void PlayPinwheel()
        {
            if (!fsm.TryStartPlay()) return;
            stats.ChangeFun(Config.playFun);
            stats.ChangeEnergy(-Config.playEnergyCost);
            stats.ChangeBond(Config.playBond);
            fsm.Say("Wheee!");
        }

        /// <summary>Turbine: starts or stops work. Income is handled by the Turbine.</summary>
        public void ToggleTurbine()
        {
            if (fsm.State == PetState.Working) fsm.StopWork();
            else fsm.TryStartWork();
        }
    }
}
