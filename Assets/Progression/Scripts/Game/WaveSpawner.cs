using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PokerTD
{
    /// <summary>
    /// Spawns one wave: a list of enemy types picked by their share and first ante,
    /// one every spawnInterval seconds. Tells the game when all of them are gone.
    /// </summary>
    public class WaveSpawner : MonoBehaviour
    {
        public static WaveSpawner Instance { get; private set; }
        public TDConfig config;
        public Material enemyMaterial;

        bool spawning;

        void Awake() => Instance = this;

        public void Begin(int ante, int waveInAnte) => StartCoroutine(Run(ante, waveInAnte));

        IEnumerator Run(int ante, int waveInAnte)
        {
            spawning = true;
            float health = TDGame.Instance.WaveHealth(ante, waveInAnte);
            var list = Compose(ante);
            foreach (var type in list)
            {
                Enemy.Spawn(type, health * type.healthMultiplier, enemyMaterial);
                yield return new WaitForSeconds(config.spawnInterval);
            }
            spawning = false;
        }

        List<TDConfig.EnemyType> Compose(int ante)
        {
            int count = config.enemiesPerWave + config.extraEnemiesPerAnte * (ante - 1);
            var list = new List<TDConfig.EnemyType>();
            var normal = config.enemies[0];
            for (int i = 0; i < count; i++)
            {
                var pick = normal;
                foreach (var e in config.enemies)
                    if (e != normal && ante >= e.fromAnte && Random.value < e.share) { pick = e; break; }
                list.Add(pick);
            }
            return list;
        }

        void Update()
        {
            if (!spawning && TDGame.Instance.Current == TDGame.Phase.Wave && Enemy.All.Count == 0) TDGame.Instance.WaveCleared();
        }
    }
}
