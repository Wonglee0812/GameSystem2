using System;
using System.Collections;
using UnityEngine;

namespace PokerTD
{
    /// <summary>
    /// Between the two turns the battle board sinks out of view and the shop floor
    /// comes up in its place (and the other way round), like the station in Unrailed 2.
    /// Graybox: both are simply moved up and down with an ease-in-out curve.
    /// </summary>
    public class BoardTransition : MonoBehaviour
    {
        public static BoardTransition Instance { get; private set; }
        public Transform board;
        public Transform shop;
        public float depth = 9f;
        public TDConfig config;

        public bool Busy { get; private set; }

        void Awake()
        {
            Instance = this;
            shop.position = new Vector3(shop.position.x, -depth, shop.position.z);
            shop.gameObject.SetActive(false);
        }

        public void Sink(Action done = null) => StartCoroutine(Move(true, done));
        public void Rise(Action done = null) => StartCoroutine(Move(false, done));

        IEnumerator Move(bool toShop, Action done)
        {
            Busy = true;
            shop.gameObject.SetActive(true);
            board.gameObject.SetActive(true);
            float t = 0f, time = Mathf.Max(0.05f, config.boardMoveSeconds);
            while (t < 1f)
            {
                t = Mathf.Min(1f, t + Time.deltaTime / time);
                float k = Mathf.SmoothStep(0f, 1f, t);
                float boardY = toShop ? Mathf.Lerp(0f, -depth, k) : Mathf.Lerp(-depth, 0f, k);
                Set(board, boardY);
                Set(shop, -depth - boardY);   // the shop goes the other way
                yield return null;
            }
            (toShop ? board : shop).gameObject.SetActive(false);
            Busy = false;
            done?.Invoke();
        }

        static void Set(Transform t, float y) => t.position = new Vector3(t.position.x, y, t.position.z);
    }
}
