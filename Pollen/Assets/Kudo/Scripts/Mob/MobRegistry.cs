using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mob
{
    /// <summary>
    /// Chase モードに入った Mob を登録順に管理するシングルトン。
    ///
    /// ---- 追従ルール ----
    ///   登録番号 0 → Player を追従
    ///   登録番号 N → 番号 N-1 の Mob を追従
    ///
    /// ---- ライフサイクル ----
    ///   Chase 開始時 : ChaseBehavior.OnEnter() → Register()
    ///   Mob 破棄時   : MobController.OnDestroy() → Unregister()
    ///   Unregister 時は後続の番号を詰め直す（追従先が自動的に繰り上がる）
    /// </summary>
    public class MobRegistry : MonoBehaviour
    {
        // ---- Singleton ----
        public static MobRegistry Instance { get; private set; }

        private readonly List<MobController> _chaseList = new();

        // ---- Player 参照（ChaseBehavior が参照する） ----
        public Transform PlayerTransform { get; private set; }

        // ================================================================
        //  Unity
        // ================================================================

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Player を自動取得
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                PlayerTransform = player.transform;
            else
                Debug.LogWarning("[MobRegistry] Player タグのオブジェクトが見つかりません。");
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ================================================================
        //  Public API
        // ================================================================

        /// <summary>
        /// Chase 開始時に Mob を末尾へ登録する。
        /// 登録番号（リストインデックス）を返す。
        /// </summary>
        public int Register(MobController mob)
        {
            if (!_chaseList.Contains(mob))
                _chaseList.Add(mob);

            int index = _chaseList.IndexOf(mob);
            Debug.Log($"[MobRegistry] 登録: {mob.name}  番号={index}  合計={_chaseList.Count}");
            return index;
        }

        /// <summary>
        /// Mob 破棄時にリストから除外する。
        /// 後続エントリは番号が 1 つ繰り上がり、追従先が自動更新される。
        /// </summary>
        public void Unregister(MobController mob)
        {
            _chaseList.Remove(mob);
            Debug.Log($"[MobRegistry] 解除: {mob.name}  残={_chaseList.Count}");
        }

        /// <summary>
        /// 指定番号の 1 つ前の追従ターゲット Transform を返す。
        ///   番号 0 または前が存在しない → PlayerTransform
        ///   番号 N               → _chaseList[N-1].transform
        /// </summary>
        public Transform GetFollowTarget(int myIndex)
        {
            if (myIndex <= 0 || _chaseList.Count == 0)
                return PlayerTransform;

            int prevIndex = myIndex - 1;
            if (prevIndex < _chaseList.Count)
                return _chaseList[prevIndex].transform;

            // リストが縮んで自分のインデックスが範囲外になった場合は末尾の前を返す
            return PlayerTransform;
        }

        /// <summary>登録済み Chase Mob の現在のインデックスを返す（Unregister 後の再取得用）。</summary>
        public int GetIndex(MobController mob) => _chaseList.IndexOf(mob);

        /// <summary>現在の登録数。</summary>
        public int Count => _chaseList.Count;

        public Action<MobController> OnRegistered { get; internal set; }
        public Action<MobController> OnUnregistered { get; internal set; }
    }
}
