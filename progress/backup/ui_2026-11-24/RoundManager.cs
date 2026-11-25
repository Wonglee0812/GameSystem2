using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace BrickPanic
{
    /// <summary>The endless round loop. Forecast + build (no timer, the player presses Start) -> disaster -> settle ->
    /// count survivors -> reward and next round on the same map, or game over when survivors &lt; goal.
    /// Standing bricks, their joints and their wear stay between rounds; rubble and dead people are cleared.</summary>
    public class RoundManager : MonoBehaviour
    {
        public enum State { Build, Disaster, Settle, Result, GameOver }

        [Header("References")]
        public BuildController build;
        public PhysicsActivator activator;
        public MapGenerator map;
        public Earthquake quake;
        public Tsunami tsunami;
        public Eruption eruption;

        [Header("Goal and reward")]
        [Tooltip("Survivors needed in round 1. Default 5.")]
        public int startGoal = 5;
        [Tooltip("Extra survivors needed every next round. Default 1.")]
        public int goalPerRound = 1;
        [Tooltip("Money for surviving a round (reaching the goal). Default 40.")]
        public float baseReward = 40f;
        [Tooltip("Extra money for every survivor above the goal. Default 8.")]
        public float bonusPerExtraSurvivor = 8f;

        [Header("After the disaster")]
        [Tooltip("Seconds to wait after the disaster ends, so falling bricks and people come to rest before counting. Default 3.")]
        public float settleSeconds = 3f;
        [Tooltip("A brick that moved more than this many metres during the round is rubble and is removed. Default 0.4.")]
        public float rubbleDistance = 0.4f;
        [Tooltip("A brick that turned more than this many degrees during the round is rubble and is removed. Default 12.")]
        public float rubbleAngle = 12f;

        [Header("Debug")]
        [Tooltip("Shows the temporary OnGUI HUD (the real UI comes later). Default on.")]
        public bool showDebugHud = true;

        public State Current { get; private set; } = State.Build;
        public int Round { get; private set; } = 1;
        public int Goal { get; private set; }
        public Disaster Next { get; private set; }
        public int NextSide { get; private set; }
        public int LastSurvivors { get; private set; }
        public int LastDead { get; private set; }
        public float LastReward { get; private set; }
        public int LastRubble { get; private set; }

        /// <summary>Forecast line, for example "Tsunami from the west - level 2".</summary>
        public string ForecastText => Next == null ? "" :
            Next.displayName + (Next == tsunami ? " from the " + Tsunami.SideNames[NextSide] : "") + " - level " + Round;

        readonly Dictionary<Block, Pose> startPose = new Dictionary<Block, Pose>();
        Disaster last;
        float settleTime;
        int aliveAtStart;
        GUIStyle box, big;

        void Start()
        {
            Goal = startGoal;
            PickNext();
            build.BeginBuildPhase();
        }

        void PickNext()
        {
            var all = new List<Disaster>();
            if (quake != null) all.Add(quake);
            if (tsunami != null) all.Add(tsunami);
            if (eruption != null) all.Add(eruption);
            if (all.Count > 1 && last != null) all.Remove(last);   // never the same disaster twice in a row
            Next = all[Random.Range(0, all.Count)];
            NextSide = tsunami != null ? tsunami.PickSide() : 0;
        }

        void Update()
        {
            var kb = Keyboard.current;
            switch (Current)
            {
                case State.Build:
                    if (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)) StartDisaster();
                    if (kb != null && kb.f5Key.wasPressedThisFrame) NewMap();
                    break;
                case State.Disaster:
                    if (!Next.Running) { Current = State.Settle; settleTime = 0f; }
                    break;
                case State.Settle:
                    settleTime += Time.deltaTime;
                    if (settleTime >= settleSeconds) Evaluate();
                    break;
                case State.Result:
                    if (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)) Continue();
                    break;
                case State.GameOver:
                    if (kb != null && kb.rKey.wasPressedThisFrame) Restart();
                    break;
            }
        }

        /// <summary>Ends the build phase and starts the forecast disaster.</summary>
        public void StartDisaster()
        {
            if (Current != State.Build || Next == null) return;
            build.EndBuildPhase();
            startPose.Clear();
            foreach (var b in Block.All) startPose[b] = new Pose(b.transform.position, b.transform.rotation);
            activator.Activate(new List<Block>(Block.All));
            aliveAtStart = Person.AliveCount;
            Next.SetLevel(Round);
            if (Next == tsunami) tsunami.Begin(NextSide); else Next.Begin();
            last = Next;
            Current = State.Disaster;
        }

        /// <summary>Counts survivors, clears dead people and rubble, freezes the standing bricks and pays the reward.</summary>
        void Evaluate()
        {
            LastSurvivors = Person.AliveCount;
            LastDead = Mathf.Max(0, aliveAtStart - LastSurvivors);

            foreach (var p in new List<Person>(Person.All)) if (!p.Alive) Destroy(p.gameObject);

            var standing = new List<Block>();
            LastRubble = 0;
            foreach (var b in new List<Block>(Block.All))
            {
                bool rubble = true;
                if (startPose.TryGetValue(b, out var pose))
                {
                    float moved = Vector3.Distance(b.transform.position, pose.position);
                    float turned = Quaternion.Angle(b.transform.rotation, pose.rotation);
                    bool still = b.Body == null || b.Body.linearVelocity.magnitude < 0.5f;
                    rubble = moved > rubbleDistance || turned > rubbleAngle || !still;
                }
                if (rubble)
                {
                    LastRubble++;
                    BrickFx.Smoke(b.transform.position, 0.5f, 1f);
                    Destroy(b.gameObject);
                }
                else standing.Add(b);
            }
            activator.Freeze(standing);
            startPose.Clear();

            if (LastSurvivors >= Goal)
            {
                LastReward = baseReward + bonusPerExtraSurvivor * (LastSurvivors - Goal);
                build.money += LastReward;
                Current = State.Result;
            }
            else
            {
                LastReward = 0f;
                Current = State.GameOver;
            }
        }

        /// <summary>From the result screen to the next build phase (goal +1, new forecast).</summary>
        public void Continue()
        {
            if (Current != State.Result) return;
            Round++;
            Goal += goalPerRound;
            PickNext();
            build.BeginBuildPhase();
            Current = State.Build;
        }

        /// <summary>Only before the first disaster: a new random island.</summary>
        public void NewMap()
        {
            if (Current != State.Build || Round != 1 || last != null) return;
            for (int i = Block.All.Count - 1; i >= 0; i--) Destroy(Block.All[i].gameObject);
            for (int i = Person.All.Count - 1; i >= 0; i--) Destroy(Person.All[i].gameObject);
            build.ResetBuild();
            map.Generate();
            PickNext();
        }

        /// <summary>New run: reloads the scene (new island, start money, round 1).</summary>
        public void Restart()
        {
            Person.Dead = 0;
            Person.Fainted = 0;
            Person.Causes.Clear();
            Block.JointsMade = Block.JointsBroken = Block.Lost = Block.Burned = 0;
            Block.BreakLog.Clear();
            DisasterState.Intensity = 0f;
            DisasterState.WearScale = 1f;
            SceneManager.LoadScene(gameObject.scene.name);   // the scene must be in the Build Profile scene list
        }

        // ---------------- temporary HUD (the real UI is the next step) ----------------

        void OnGUI()
        {
            if (!showDebugHud) return;
            if (box == null)
            {
                box = new GUIStyle(GUI.skin.box) { fontSize = 17, alignment = TextAnchor.UpperLeft, wordWrap = true };
                big = new GUIStyle(GUI.skin.box) { fontSize = 22, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            }
            int alive = Person.AliveCount;
            string top = $"ROUND {Round}   goal {Goal} survivors   people alive {alive}   money {build.money:0}   joint wear {activator.AverageWear * 100f:0}%\n" +
                         $"NEXT: {ForecastText}";
            switch (Current)
            {
                case State.Build:
                    string mode = build.CurrentMode == BuildController.Mode.People ? $"Person price {build.personPrice:0}" :
                        $"Material [1-4]: {build.Material.displayName}   Shape [Z/X]: {build.Shape.displayName}   price {build.Shape.Price(build.Material):0}";
                    GUI.Box(new Rect(10, 10, 920, 106), top + $"\nMODE [Tab]: {build.CurrentMode}   {mode}\n" +
                        "Left click place | right click sell | middle click move | R rotate | Ctrl+Z undo | fly: hold right mouse + WASD/QE" +
                        (Round == 1 && last == null ? " | F5 new map" : ""), box);
                    string warn = alive < Goal ? $"  (only {alive}/{Goal} people!)" : "";
                    if (GUI.Button(new Rect(Screen.width - 330, 10, 320, 60), "START  " + (Next != null ? Next.displayName.ToUpper() : "") + "  [Enter]" + warn, big)) StartDisaster();
                    break;
                case State.Disaster:
                case State.Settle:
                    GUI.Box(new Rect(10, 10, 920, 56), $"ROUND {Round}   {ForecastText}   {(Current == State.Settle ? "counting..." : "running")}\n" +
                        $"people alive {alive} / goal {Goal}   dead this round {Mathf.Max(0, aliveAtStart - alive)}", box);
                    break;
                case State.Result:
                    var r = new Rect(Screen.width * 0.5f - 260, Screen.height * 0.5f - 120, 520, 200);
                    GUI.Box(r, $"ROUND {Round} SURVIVED\n\nSurvivors {LastSurvivors} / goal {Goal}   (dead {LastDead})\n" +
                        $"Reward +{LastReward:0}  ({baseReward:0} + {bonusPerExtraSurvivor:0} x {LastSurvivors - Goal} extra)\nRubble cleared: {LastRubble} bricks", big);
                    if (GUI.Button(new Rect(r.x + 160, r.yMax + 10, 200, 50), "CONTINUE [Enter]", big)) Continue();
                    break;
                case State.GameOver:
                    var g = new Rect(Screen.width * 0.5f - 260, Screen.height * 0.5f - 120, 520, 200);
                    GUI.Box(g, $"GAME OVER\n\nSurvivors {LastSurvivors} / goal {Goal}\nRounds survived: {Round - 1}   people lost: {Person.Dead}", big);
                    if (GUI.Button(new Rect(g.x + 160, g.yMax + 10, 200, 50), "RESTART [R]", big)) Restart();
                    break;
            }
        }
    }
}
