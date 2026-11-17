using System.Collections.Generic;
using UnityEngine;

namespace DamRush
{
    public enum BeaverAge { Adult, Yearling, Kit }
    public enum WorkGroup { Idle, Wood, Earth, Build, Forage, Operate }
    public enum Item { None, Wood, Mud, Stone, Food, Branches, PressedWood, Gravel, Sand, Cement }
    public enum BeaverState { Thinking, Moving, Working, Resting, InLodge }

    /// <summary>
    /// One beaver. A small state machine: think, walk or swim along a hex path, work, carry, repeat.
    ///
    /// Each time it thinks, it checks in this order:
    /// 1. alarm  - after a tail slap the whole family hides in the lodge,
    /// 2. energy - a tired beaver sleeps in the lodge (work by day tires it out faster:
    ///             beavers are mainly active at night, Bartra Cabré et al., 2020),
    /// 3. hunger - a hungry beaver eats bark: wood from a storage, or a tree,
    /// 4. carrying - bring what it holds to the nearest storage with room,
    /// 5. instinct - idle workers go and plug the loudest leaking dam (based on Wilsson, 1971,
    ///               cited in Mental Floss, n.d.: running water makes beavers build),
    /// 6. its group job - cut wood, dig mud and stones, or build dams.
    /// Kits do not work in their first year, they stay close to the lodge.
    /// </summary>
    public partial class Beaver : MonoBehaviour
    {
        public string beaverName = "Beaver";
        public BeaverAge age;
        public WorkGroup group;
        public Item carrying;
        [Tooltip("How many items of 'carrying' it holds.")]
        public int carryCount;
        public BeaverState state = BeaverState.Thinking;
        public string task = "";

        [Header("Speeds (m/s)")]
        public float walkSpeed = 1.1f;
        public float swimSpeed = 2f;

        [Tooltip("Seconds lived in the current age stage (kits and yearlings grow up).")]
        public float ageSeconds;

        [Header("Needs (0-100)")]
        public float energy = 100f;
        public float food = 100f;
        [Tooltip("Drops while the stomach is empty; the beaver dies at 0.")]
        public float health = 100f;
        bool dead;

        public ColonyManager colony;
        public GameObject carryWood, carryMud, carryStone;

        List<Vector3> waypoints;
        int wpIndex;
        float timer;
        System.Action onArrive;
        System.Action onWorkDone;
        Transform body;
        bool hidingFromAlarm;

        public HexCell Current => colony.Grid.TryGet(transform.position, out var c) ? c : null;
        public bool CanWork => age != BeaverAge.Kit;
        /// <summary>Items per trip for every job (cutting, digging, building, piles): adults 2, yearlings 1.</summary>
        public int CarryCapacity => age == BeaverAge.Adult ? 2 : 1;
        public bool Tired => energy < colony.needs.tiredBelow;
        public bool Hungry => food < colony.needs.hungryBelow;
        /// <summary>Empty stomach: losing health. Food comes before sleep and before hiding.</summary>
        public bool Starving => food < 5f;

        void Start()
        {
            body = transform.GetChild(0);
            timer = Random.Range(0f, 1f);
            energy = Random.Range(70f, 100f);
            food = Random.Range(60f, 100f);
            UpdateCarryVisual();
        }

        public static float SizeOf(BeaverAge a) => a == BeaverAge.Adult ? 1f : a == BeaverAge.Yearling ? 0.82f : 0.6f;

        /// <summary>Kit -> yearling -> adult. The body grows and it moves faster.</summary>
        public void GrowUp()
        {
            age = age == BeaverAge.Kit ? BeaverAge.Yearling : BeaverAge.Adult;
            float sz = SizeOf(age);
            transform.GetChild(0).localScale = Vector3.one * sz;
            walkSpeed = 1.1f * Mathf.Lerp(0.8f, 1f, sz);
            swimSpeed = 2f * Mathf.Lerp(0.8f, 1f, sz);
        }

        /// <summary>Called by the colony when the player changes the group sizes.</summary>
        public void SetGroup(WorkGroup g)
        {
            if (group == g) return;
            group = g;
            if (state == BeaverState.InLodge) return;                          // decides when it comes out
            if (carrying != Item.None && state == BeaverState.Moving) return; // finish the delivery first
            Interrupt();
        }

        /// <summary>Drop the current plan (keeps what it carries) and think again.</summary>
        public void Interrupt()
        {
            if (state == BeaverState.InLodge) return;
            colony.Release(this);
            if (body != null) body.localRotation = Quaternion.identity;
            onArrive = null; onWorkDone = null;
            GoThink(0.1f);
        }

        void GoThink(float delay)
        {
            state = BeaverState.Thinking;
            timer = delay;
            waypoints = null;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            UpdateNeeds(dt);
            if (dead) return;
            switch (state)
            {
                case BeaverState.Thinking:
                case BeaverState.Resting:
                    timer -= dt;
                    if (timer <= 0f) Think();
                    break;
                case BeaverState.Moving:
                    Move(dt);
                    if (colony.IsSwimming(transform.position)) WaterRipples.Emit(transform.position, 0.8f, 2.2f);
                    break;
                case BeaverState.Working:
                    timer -= dt;
                    // gnawing / digging: small nod of the body
                    if (body != null) body.localRotation = Quaternion.Euler(Mathf.Sin(Time.time * 12f) * 8f, 0f, 0f);
                    if (timer <= 0f)
                    {
                        if (body != null) body.localRotation = Quaternion.identity;
                        var done = onWorkDone;
                        onWorkDone = null;
                        done?.Invoke();
                    }
                    break;
                case BeaverState.InLodge:
                    UpdateInLodge();
                    return; // hidden, no surface update
            }
            StickToSurface();
        }

        void UpdateNeeds(float dt)
        {
            var n = colony.needs;
            // kits are small and eat half as much
            food = Mathf.Max(0f, food - n.hungerPerSecond * (age == BeaverAge.Kit ? 0.5f : 1f) * dt);
            // an empty stomach costs health; a fed beaver heals again
            float day = colony.dayNight != null ? colony.dayNight.dayLength : 240f;
            if (food <= 0f) health -= 100f / Mathf.Max(0.1f, n.starveDays * day) * dt;
            else if (food > n.hungryBelow) health = Mathf.Min(100f, health + 100f / day * dt);
            if (health <= 0f && !dead) { dead = true; colony.Die(this, "hunger"); return; }
            if (state == BeaverState.InLodge && !hidingFromAlarm) return; // sleeping: handled in UpdateInLodge
            float drain = state == BeaverState.Working ? n.workEnergyPerSecond : n.awakeEnergyPerSecond;
            if (!colony.dayNight.IsNight) drain *= n.dayTiredness;
            energy = Mathf.Max(0f, energy - drain * dt);
        }

        // ------------------------------------------------------------------ decisions

        void Think()
        {
            if (colony.GameOver) return;
            // a starving beaver leaves the lodge to eat, even during an alarm
            if (colony.AlarmActive && !Starving) { Hide("Hiding: tail slap alarm", true); return; }
            if (colony.IsWinter) { WinterThink(); return; }
            if (Tired && !Starving) { Hide("Sleeping in the lodge", false); return; }
            if (Hungry && carrying != Item.None && colony.NearestStorageWithSpace(carrying, transform.position) == null)
            {
                // storages are full: put the load down here, eating comes first
                colony.DropPile(Current, carrying, Mathf.Max(1, carryCount));
                carrying = Item.None; carryCount = 0;
                UpdateCarryVisual();
            }
            if (Hungry && carrying == Item.None) { Eat(); return; }
            if (carrying != Item.None) { Deliver(); return; }
            if (CanWork && OperateIfCalled()) return; // a machine called this beaver

            // instinct: a working beaver without a job plugs the loudest leak
            if (CanWork && group == WorkGroup.Idle && colony.orders != null && colony.orders.PickJob(this, out Site leak, out Item fix, true))
            {
                Build(leak, fix, true);
                return;
            }
            // materials lying on the ground (from a broken dam or a removed order) are picked up first:
            // wood cutters take wood, diggers mud and stone, idle workers anything
            System.Predicate<Item> wants = group == WorkGroup.Forage ? (i => i == Item.Food) : group == WorkGroup.Wood ? (i => i == Item.Wood)
                : group == WorkGroup.Earth ? (i => i != Item.Wood) : group == WorkGroup.Idle ? (i => true) : (System.Predicate<Item>)null;
            if (CanWork && wants != null && colony.PickPile(this, wants, out MaterialPile pile))
            {
                task = $"Going to pick up {pile.item.ToString().ToLower()} from a pile";
                GoTo(pile.hex, pile.pos + (transform.position - pile.pos).normalized * 0.5f, () =>
                {
                    colony.Release(this);
                    int got = 0;
                    while (got < CarryCapacity && colony.TakeFrom(pile, pile.item)) got++;
                    if (got == 0) { GoThink(0.2f); return; }
                    carrying = pile.item;
                    carryCount = got;
                    UpdateCarryVisual();
                    Deliver();
                });
                return;
            }
            if (CanWork && group == WorkGroup.Forage) { Forage(); return; }
            if (CanWork && group == WorkGroup.Operate) { Operate(); return; }
            // marked trees first: felling clears the hex and gives a lot of wood at once
            if (CanWork && group == WorkGroup.Wood && colony.PickFellTree(this, out Vector3 fellPos, out HexCell fellHex, out Transform fellTree))
            {
                Fell(fellPos, fellHex, fellTree);
                return;
            }
            if (CanWork && group == WorkGroup.Wood && colony.FreeSpace(Item.Wood) <= 0) { Wander("Storage full of wood"); return; }
            if (CanWork && group == WorkGroup.Earth && !colony.HasDigOrders && colony.FreeSpace(Item.Mud) <= 0 && colony.FreeSpace(Item.Stone) <= 0) { Wander("Storage full of mud and stone"); return; }
            if (CanWork && group == WorkGroup.Wood && colony.PickTree(this, out Vector3 treePos, out HexCell treeHex))
            {
                task = "Going to cut a tree";
                GoTo(treeHex, treePos + (transform.position - treePos).normalized * 0.7f, () =>
                {
                    task = "Cutting a tree";
                    StartWork(colony.cutTime, Item.Wood);
                    colony.Release(this);
                });
                return;
            }
            if (CanWork && group == WorkGroup.Earth && colony.PickDigSite(this, out HexCell dig))
            {
                bool ordered = colony.orders != null && colony.orders.DigOrderOn(dig) != null;
                task = ordered ? "Going to dig (dig order)" : "Going to dig";
                GoTo(dig, dig.center + new Vector3(Random.Range(-0.6f, 0.6f), 0f, Random.Range(-0.6f, 0.6f)), () =>
                {
                    Item found = colony.NextEarthItem();
                    task = found == Item.Stone ? "Picking up a stone" : "Digging mud";
                    if (ordered) DigOrdered(dig, found);
                    else StartWork(colony.digTime, found);
                    colony.Release(this);
                });
                return;
            }
            if (CanWork && group == WorkGroup.Build && colony.orders != null && colony.orders.PickJob(this, out Site site, out Item need, false))
            {
                Build(site, need, false);
                return;
            }
            Wander();
        }

        /// <summary>Builder: fetch one item from a storage, carry it to the site, put it in place.</summary>
        void Build(Site site, Item need, bool instinct)
        {
            string why = instinct ? " (leak!)" : "";
            string what = site.Title.ToLower();
            var storage = colony.orders.JobStorage(this);
            string from = storage is MaterialPile ? "a pile" : "the storage";
            task = $"Fetching {need.ToString().ToLower()} from {from} for a {what}{why}";
            GoTo(storage.Hex, storage.Door(transform.position), () =>
            {
                int got = colony.orders.PickUp(this);
                if (got <= 0) { task = $"No material in {from}"; GoThink(1f); return; }
                carrying = need;
                carryCount = got;
                UpdateCarryVisual();
                task = $"Carrying {got} {need.ToString().ToLower()} to a {what}{why}";
                GoTo(site.hex, site.hex.center + new Vector3(Random.Range(-0.7f, 0.7f), 0f, Random.Range(-0.7f, 0.7f)), () =>
                {
                    if (!colony.orders.StillValid(this)) { GoThink(0.2f); return; } // order removed: bring it back
                    task = instinct ? "Plugging a leak" : $"Building a {what}";
                    // placing two items takes a bit longer than one, but far less than two trips
                    StartWork(colony.orders.buildTime * (1f + 0.5f * (carryCount - 1)), () =>
                    {
                        if (colony.IsSwimming(transform.position)) WaterRipples.Splash(transform.position);
                        carrying = Item.None;
                        carryCount = 0;
                        colony.orders.Complete(this); // gives the item back if the order was removed
                        UpdateCarryVisual();
                        GoThink(0.2f);
                    });
                });
            });
        }

        /// <summary>
        /// Eat from the nearest food storage (in winter only one with deep water next to it).
        /// Without stored food, gnaw bark from a tree: slow and only half a meal. No trees in winter.
        /// </summary>
        void Eat()
        {
            var store = colony.NearestFood(transform.position);
            if (store != null)
            {
                task = "Going to eat";
                GoTo(store.hex, store.Door(transform.position), () =>
                {
                    if (!colony.TakeFrom(store, Item.Food)) { GoThink(0.2f); return; }
                    colony.Stat("food eaten");
                    task = "Eating";
                    StartWork(colony.needs.eatTime, () => { food = Mathf.Min(100f, food + colony.needs.foodPerMeal); GoThink(0.2f); });
                });
                return;
            }
            if (!colony.IsWinter && colony.PickTree(this, out Vector3 treePos, out HexCell treeHex))
            {
                task = "No stored food: going to gnaw a tree";
                GoTo(treeHex, treePos + (transform.position - treePos).normalized * 0.7f, () =>
                {
                    colony.Release(this);
                    task = "Gnawing bark (half a meal)";
                    colony.Stat("meals from trees");
                    StartWork(colony.needs.eatTime * 2f, () => { food = Mathf.Min(100f, food + colony.needs.foodPerMeal * 0.5f); GoThink(0.2f); });
                });
                return;
            }
            if (colony.IsWinter) { Hide("Hungry: no food under the ice", false); return; }
            Wander("Hungry: no food in the storages");
        }

        /// <summary>Forager: water plants in spring and summer, branches from trees in autumn, then to a food storage.</summary>
        void Forage()
        {
            if (colony.FreeSpace(Item.Food) <= 0) { Wander("Food storages are full"); return; }
            // food farms first: the food is already grown, picking it up is quick
            var farm = colony.orders != null ? colony.orders.PickFarm(this) : null;
            if (farm != null)
            {
                task = "Going to the food farm";
                GoTo(farm.hex, farm.hex.center + new Vector3(Random.Range(-0.6f, 0.6f), 0f, Random.Range(-0.6f, 0.6f)), () =>
                {
                    task = "Picking food at the farm";
                    StartWork(colony.forageTime * 0.5f, () =>
                    {
                        int got = colony.orders.TakeFarmFood(this, farm, CarryCapacity + (colony.seasons != null ? colony.seasons.ForageBonus : 0));
                        if (got <= 0) { GoThink(0.2f); return; }
                        carrying = Item.Food; carryCount = got;
                        colony.Stat("food from farms", got);
                        UpdateCarryVisual();
                        Deliver();
                    });
                });
                return;
            }
            if (!colony.PickForageSpot(this, out Vector3 pos, out HexCell hex, out bool tree)) { Wander("No food to find here now"); return; }
            task = tree ? "Going to cut branches (food)" : "Going to pick water plants";
            Vector3 target = tree ? pos + (transform.position - pos).normalized * 0.7f : pos;
            GoTo(hex, target, () =>
            {
                task = tree ? "Cutting branches" : "Picking water plants";
                StartWork(colony.forageTime, () =>
                {
                    colony.Release(this);
                    carrying = Item.Food;
                    carryCount = CarryCapacity + (colony.seasons != null ? colony.seasons.ForageBonus : 0);
                    colony.Stat("food foraged", carryCount);
                    UpdateCarryVisual();
                    Deliver();
                });
            });
        }

        /// <summary>Wood cutter: fell a marked tree. Carry what fits home, the rest stays as a pile.</summary>
        void Fell(Vector3 treePos, HexCell treeHex, Transform tree)
        {
            task = "Going to fell a marked tree";
            GoTo(treeHex, treePos + (transform.position - treePos).normalized * 0.7f, () =>
            {
                task = "Felling a tree";
                StartWork(colony.cutTime * colony.fellTimeFactor, () =>
                {
                    colony.Release(this);
                    int wood = colony.FellTree(tree);
                    if (wood <= 0) { GoThink(0.2f); return; } // someone else felled it
                    int take = colony.FreeSpace(Item.Wood) > 0 ? Mathf.Min(CarryCapacity, wood) : 0;
                    colony.DropPile(treeHex, Item.Wood, wood - take);
                    colony.Stat("wood gathered", take);
                    if (take == 0) { GoThink(0.2f); return; }
                    carrying = Item.Wood; carryCount = take;
                    UpdateCarryVisual();
                    Deliver();
                });
            });
        }

        /// <summary>Digger on a dig order: the load counts for the order. With full storages the load goes on the bank as a pile.</summary>
        void DigOrdered(HexCell dig, Item found)
        {
            StartWork(colony.digTime, () =>
            {
                colony.orders.DugLoad(dig);
                if (colony.FreeSpace(found) <= 0)
                {
                    colony.DropPile(colony.Bank(dig), found, CarryCapacity);
                    GoThink(0.2f);
                    return;
                }
                carrying = found; carryCount = CarryCapacity;
                colony.Stat(found.ToString().ToLower() + " gathered", carryCount);
                UpdateCarryVisual();
                Deliver();
            });
        }

        /// <summary>Winter: no outside work. Bring home what it carries, eat when hungry, else stay in the lodge.</summary>
        void WinterThink()
        {
            if (carrying != Item.None) { Deliver(); return; }
            if (Hungry) { Eat(); return; }
            Hide("Waiting out the winter in the lodge", false);
        }

        /// <summary>Go into the lodge: to sleep, or to hide after a tail slap.</summary>
        void Hide(string why, bool alarm)
        {
            task = alarm ? "Swimming home: alarm!" : "Going home to sleep";
            GoTo(colony.NearestLodgeCell(Current), colony.LodgeDoor(transform.position), () =>
            {
                task = why; // it keeps what it carries and brings it to a storage later
                hidingFromAlarm = alarm;
                state = BeaverState.InLodge;
                transform.position = colony.LodgeCenter;
                body.gameObject.SetActive(false);
            });
        }

        void UpdateInLodge()
        {
            if (hidingFromAlarm)
            {
                if (!colony.AlarmActive || Starving) ComeOut();
                return;
            }
            energy = Mathf.Min(100f, energy + colony.needs.sleepEnergyPerSecond * Time.deltaTime);
            if (colony.IsWinter)
            {
                // winter: stay in the lodge; go out only to eat, and only when the entrance is not frozen in
                if (Hungry && !colony.LodgeFrozenIn && colony.NearestFood(transform.position) != null) ComeOut();
                return;
            }
            // wake up when rested, or when hungry and there is food
            if (energy >= colony.needs.wakeAt || (Hungry && energy > colony.needs.tiredBelow + 10f) || Starving) ComeOut();
        }

        void ComeOut()
        {
            hidingFromAlarm = false;
            body.gameObject.SetActive(true);
            transform.position = colony.LodgeDoor(colony.LodgeCenter + Random.insideUnitSphere * 3f);
            GoThink(Random.Range(0.2f, 1.2f));
        }

        void Wander(string why = null)
        {
            task = why ?? (group == WorkGroup.Build && CanWork ? "Waiting for an order or materials" : CanWork ? "Idle near the lodge" : "Playing near the lodge");
            var spot = colony.RandomSpotNearLodge();
            if (spot == null) { GoThink(1f); return; }
            GoTo(spot, spot.center + new Vector3(Random.Range(-0.8f, 0.8f), 0f, Random.Range(-0.8f, 0.8f)), () =>
            {
                state = BeaverState.Resting;
                timer = Random.Range(2f, 5f);
            });
        }

        void Deliver()
        {
            var store = colony.NearestStorageWithSpace(carrying, transform.position);
            if (store == null) { Wander($"All storages are full: holding {carrying.ToString().ToLower()}"); return; }
            if (carryCount < 1) carryCount = 1;
            task = "Carrying " + carrying.ToString().ToLower() + " to the storage";
            GoTo(store.hex, store.Door(transform.position), () =>
            {
                while (carryCount > 0 && colony.StoreIn(store, carrying)) carryCount--;
                if (carryCount <= 0)
                {
                    carrying = Item.None;
                    carryCount = 0;
                    UpdateCarryVisual();
                }
                GoThink(0.3f); // full on arrival: think again and try another storage
            });
        }

        /// <summary>Gather work: when done, carry the result to the lodge.</summary>
        void StartWork(float seconds, Item result)
        {
            StartWork(seconds, () =>
            {
                carrying = result;
                carryCount = CarryCapacity;
                colony.Stat(result.ToString().ToLower() + " gathered", carryCount);
                UpdateCarryVisual();
                Deliver();
            });
        }

        void StartWork(float seconds, System.Action done)
        {
            state = BeaverState.Working;
            // tired beavers work slower
            timer = seconds * (energy < 40f ? 1.5f : 1f);
            onWorkDone = done;
        }

        // ------------------------------------------------------------------ movement

        void GoTo(HexCell goal, Vector3 finalPoint, System.Action arrive)
        {
            var path = colony.Pathfinder.FindPath(Current, goal);
            if (path == null)
            {
                colony.Release(this);
                task = "Cannot reach the target";
                GoThink(1f);
                return;
            }
            waypoints = new List<Vector3>();
            // skip the hex we stand in and the goal hex: walk to the final point instead
            for (int i = 1; i < path.Count - 1; i++) waypoints.Add(path[i].center);
            waypoints.Add(finalPoint);
            wpIndex = 0;
            onArrive = arrive;
            state = BeaverState.Moving;
        }

        void Move(float dt)
        {
            if (waypoints == null || wpIndex >= waypoints.Count) { state = BeaverState.Thinking; return; }
            Vector3 target = waypoints[wpIndex];
            Vector3 pos = transform.position;
            Vector3 flat = new Vector3(target.x - pos.x, 0f, target.z - pos.z);
            float speed = colony.IsSwimming(pos) ? swimSpeed : walkSpeed;
            if (food <= 0f) speed *= 0.6f; // starving
            float step = speed * dt;
            if (flat.magnitude <= step)
            {
                transform.position = new Vector3(target.x, pos.y, target.z);
                wpIndex++;
                if (wpIndex >= waypoints.Count)
                {
                    waypoints = null;
                    var done = onArrive;
                    onArrive = null;
                    done?.Invoke();
                }
                return;
            }
            Vector3 dir = flat.normalized;
            transform.position = pos + dir * step;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), dt * 8f);
        }

        /// <summary>Walk on the ground, or float with the back just at the water surface when swimming.</summary>
        void StickToSurface()
        {
            Vector3 p = transform.position;
            float y = colony.SurfaceHeight(p.x, p.z);
            p.y = Mathf.Lerp(p.y, y, Time.deltaTime * 6f);
            transform.position = p;
        }

        void UpdateCarryVisual()
        {
            if (carryWood != null) carryWood.SetActive(carrying == Item.Wood);
            if (carryMud != null) carryMud.SetActive(carrying == Item.Mud);
            if (carryStone != null) carryStone.SetActive(carrying == Item.Stone);
        }
    }
}
