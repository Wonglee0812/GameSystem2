using UnityEngine;

namespace DamRush
{
    /// <summary>
    /// Machine work (Phase C). A machine with an order calls one beaver from the family
    /// (Industry.AssignOperators). The beaver fetches inputs for up to three batches, runs them
    /// one after the other (slower or faster with the water wheel), carries the products home
    /// and goes on until the order is done. Then it goes back to its own work group.
    /// </summary>
    public partial class Beaver
    {
        /// <summary>The machine this beaver was called to run, or null.</summary>
        [System.NonSerialized] public BuildingSite machineJob;

        /// <summary>Called from Think: true when the beaver works on its machine now.</summary>
        bool OperateIfCalled()
        {
            if (machineJob == null) return false;
            var ind = colony.industry;
            if (ind == null || !ind.StillOperating(this, machineJob)) { machineJob = null; return false; }
            OperateNext(machineJob);
            return true;
        }

        /// <summary>Carry products home, fetch missing inputs, or run the loaded batches.</summary>
        void OperateNext(BuildingSite m)
        {
            var ind = colony.industry;
            if (!ind.StillOperating(this, m)) { GoThink(0.2f); return; }
            int want = ind.WantBatches(m), loaded = ind.BatchesLoaded(m);

            if (loaded < want)
            {
                Item need = ind.MissingInput(m, want, out int count);
                if (need != Item.None)
                {
                    var supply = colony.NearestSupplyWith(need, transform.position, m.hex.center);
                    if (supply != null) { Fetch(m, need, count, supply); return; }
                }
            }
            if (loaded > 0 && want > 0) { RunBatch(m); return; }
            if (m.outBuffer > 0) { CarryOut(m); return; }
            // nothing more to do here: back to the work group
            ind.Release(this);
            GoThink(0.3f);
        }

        void Fetch(BuildingSite m, Item need, int count, ISupply supply)
        {
            var ind = colony.industry;
            task = $"Fetching {ItemInfo.Lower(need)} for the {m.Title.ToLower()}";
            GoTo(supply.Hex, supply.Door(transform.position), () =>
            {
                int got = 0;
                while (got < count && colony.TakeFrom(supply, need)) got++;
                if (got == 0) { GoThink(0.3f); return; }
                carrying = need; carryCount = got;
                UpdateCarryVisual();
                task = $"Loading {got} {ItemInfo.Lower(need)} into the {m.Title.ToLower()}";
                GoTo(m.hex, m.Door(transform.position), () =>
                {
                    if (!ind.StillOperating(this, m)) { GoThink(0.2f); return; } // keeps the items and stores them
                    m.AddBuffer(need, carryCount);
                    carrying = Item.None; carryCount = 0;
                    UpdateCarryVisual();
                    OperateNext(m);
                });
            });
        }

        void RunBatch(BuildingSite m)
        {
            var ind = colony.industry;
            if (m.power <= 0f)
            {
                // the wheel stopped: the inputs stay loaded, the beaver goes back to other work
                if (m.outBuffer > 0) { CarryOut(m); return; }
                ind.Release(this);
                GoThink(0.5f);
                return;
            }
            task = $"Running the {m.Title.ToLower()} ({m.power:0.0}x)";
            StartWork(ind.BatchSeconds(m), () =>
            {
                if (!ind.StillOperating(this, m)) { GoThink(0.2f); return; }
                ind.FinishBatch(m);
                OperateNext(m);
            });
        }

        void CarryOut(BuildingSite m)
        {
            carrying = m.machine.recipe.output.item;
            carryCount = m.outBuffer;
            m.outBuffer = 0;
            UpdateCarryVisual();
            Deliver(); // the next Think goes on with the machine while the order is open
        }

        void Operate()
        {
            // the old "Operators" work group is no longer used: machines call beavers themselves
            Wander();
        }
    }
}
