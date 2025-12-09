using UnityEngine;
using UnityEditor;
using ShadowCardSmash.UI.Battle;
using ShadowCardSmash.Tests;

namespace ShadowCardSmash.Editor
{
    /// <summary>
    /// 战斗场景验证器 - 检查并修复场景中的引用
    /// </summary>
    public class BattleSceneValidator : EditorWindow
    {
        [MenuItem("ShadowCardSmash/验证战斗场景")]
        public static void ValidateScene()
        {
            Debug.Log("=== 开始验证战斗场景 ===");

            bool hasErrors = false;

            // 1. 检查 HotSeatGameManager
            var hotSeat = FindObjectOfType<HotSeatGameManager>();
            if (hotSeat == null)
            {
                Debug.LogError("❌ 找不到 HotSeatGameManager！");
                hasErrors = true;
            }
            else
            {
                Debug.Log("✓ HotSeatGameManager 存在");

                if (hotSeat.battleUI == null)
                {
                    Debug.LogError("  ❌ battleUI 未设置！");
                    hasErrors = true;
                }
                else
                {
                    Debug.Log("  ✓ battleUI 已设置");
                }

                if (hotSeat.mulliganUI == null)
                {
                    Debug.LogWarning("  ⚠ mulliganUI 未设置（将跳过换牌阶段）");
                }
                else
                {
                    Debug.Log("  ✓ mulliganUI 已设置");
                    ValidateMulliganUI(hotSeat.mulliganUI);
                }
            }

            // 2. 检查 BattleUIController
            var battleUI = FindObjectOfType<BattleUIController>();
            if (battleUI == null)
            {
                Debug.LogError("❌ 找不到 BattleUIController！");
                hasErrors = true;
            }
            else
            {
                Debug.Log("✓ BattleUIController 存在");
                ValidateBattleUI(battleUI);
            }

            // 3. 检查 HandAreaController
            var handAreas = FindObjectsOfType<HandAreaController>();
            Debug.Log($"✓ 找到 {handAreas.Length} 个 HandAreaController");
            foreach (var hand in handAreas)
            {
                ValidateHandArea(hand);
            }

            if (hasErrors)
            {
                Debug.LogError("=== 场景验证失败，请修复上述错误 ===");
            }
            else
            {
                Debug.Log("=== 场景验证通过 ===");
            }
        }

        static void ValidateBattleUI(BattleUIController battleUI)
        {
            if (battleUI.myHandArea == null)
            {
                Debug.LogError("  ❌ myHandArea 未设置！");
            }
            else
            {
                Debug.Log("  ✓ myHandArea 已设置");
            }

            if (battleUI.opponentHandArea == null)
            {
                Debug.LogError("  ❌ opponentHandArea 未设置！");
            }
            else
            {
                Debug.Log("  ✓ opponentHandArea 已设置");
            }

            if (battleUI.myTiles == null || battleUI.myTiles.Length == 0)
            {
                Debug.LogWarning("  ⚠ myTiles 未设置或为空");
            }
            else
            {
                Debug.Log($"  ✓ myTiles 已设置 ({battleUI.myTiles.Length} 个)");
            }

            if (battleUI.opponentTiles == null || battleUI.opponentTiles.Length == 0)
            {
                Debug.LogWarning("  ⚠ opponentTiles 未设置或为空");
            }
            else
            {
                Debug.Log($"  ✓ opponentTiles 已设置 ({battleUI.opponentTiles.Length} 个)");
            }

            if (battleUI.endTurnButton == null)
            {
                Debug.LogWarning("  ⚠ endTurnButton 未设置");
            }
            else
            {
                Debug.Log("  ✓ endTurnButton 已设置");
            }
        }

        static void ValidateHandArea(HandAreaController hand)
        {
            string name = hand.isOpponentHand ? "对手手牌区" : "我方手牌区";
            Debug.Log($"  检查 {name} ({hand.gameObject.name}):");

            if (hand.handContainer == null)
            {
                Debug.LogError($"    ❌ handContainer 未设置！");
            }
            else
            {
                Debug.Log($"    ✓ handContainer 已设置");
            }

            if (hand.cardPrefab == null)
            {
                Debug.LogWarning($"    ⚠ cardPrefab 未设置（将使用备用卡牌）");
            }
            else
            {
                Debug.Log($"    ✓ cardPrefab 已设置");
            }
        }

        static void ValidateMulliganUI(MulliganUI mulligan)
        {
            if (mulligan.mulliganPanel == null)
            {
                Debug.LogError("  ❌ mulliganPanel 未设置！");
            }
            else
            {
                Debug.Log("  ✓ mulliganPanel 已设置");
            }

            if (mulligan.cardContainer == null)
            {
                Debug.LogWarning("  ⚠ cardContainer 未设置（将使用 mulliganPanel）");
            }
            else
            {
                Debug.Log("  ✓ cardContainer 已设置");
            }

            if (mulligan.confirmButton == null)
            {
                Debug.LogWarning("  ⚠ confirmButton 未设置");
            }
            else
            {
                Debug.Log("  ✓ confirmButton 已设置");
            }
        }

        [MenuItem("ShadowCardSmash/自动修复场景引用")]
        public static void AutoFixReferences()
        {
            Debug.Log("=== 尝试自动修复场景引用 ===");

            // 查找所有相关组件
            var hotSeat = FindObjectOfType<HotSeatGameManager>();
            var battleUI = FindObjectOfType<BattleUIController>();
            var mulliganUI = FindObjectOfType<MulliganUI>();
            var handAreas = FindObjectsOfType<HandAreaController>();

            bool changed = false;

            // 修复 HotSeatGameManager
            if (hotSeat != null)
            {
                if (hotSeat.battleUI == null && battleUI != null)
                {
                    hotSeat.battleUI = battleUI;
                    Debug.Log("✓ 自动设置 HotSeatGameManager.battleUI");
                    changed = true;
                }

                if (hotSeat.mulliganUI == null && mulliganUI != null)
                {
                    hotSeat.mulliganUI = mulliganUI;
                    Debug.Log("✓ 自动设置 HotSeatGameManager.mulliganUI");
                    changed = true;
                }
            }

            // 修复 BattleUIController
            if (battleUI != null)
            {
                foreach (var hand in handAreas)
                {
                    if (!hand.isOpponentHand && battleUI.myHandArea == null)
                    {
                        battleUI.myHandArea = hand;
                        Debug.Log("✓ 自动设置 BattleUIController.myHandArea");
                        changed = true;
                    }
                    else if (hand.isOpponentHand && battleUI.opponentHandArea == null)
                    {
                        battleUI.opponentHandArea = hand;
                        Debug.Log("✓ 自动设置 BattleUIController.opponentHandArea");
                        changed = true;
                    }
                }
            }

            // 修复 HandAreaController 的 handContainer
            foreach (var hand in handAreas)
            {
                if (hand.handContainer == null)
                {
                    // 尝试找到名为 "CardContainer" 或 "Container" 的子对象
                    var container = hand.transform.Find("CardContainer") ??
                                   hand.transform.Find("Container") ??
                                   hand.transform.Find("Cards");

                    if (container != null)
                    {
                        hand.handContainer = container;
                        Debug.Log($"✓ 自动设置 {hand.gameObject.name}.handContainer");
                        changed = true;
                    }
                    else
                    {
                        // 使用自身作为容器
                        hand.handContainer = hand.transform;
                        Debug.Log($"✓ 使用自身作为 {hand.gameObject.name}.handContainer");
                        changed = true;
                    }
                }
            }

            // 修复 MulliganUI
            if (mulliganUI != null)
            {
                if (mulliganUI.mulliganPanel == null)
                {
                    mulliganUI.mulliganPanel = mulliganUI.gameObject;
                    Debug.Log("✓ 自动设置 MulliganUI.mulliganPanel 为自身");
                    changed = true;
                }

                if (mulliganUI.cardContainer == null)
                {
                    var container = mulliganUI.transform.Find("CardContainer") ??
                                   mulliganUI.transform.Find("Cards");
                    if (container != null)
                    {
                        mulliganUI.cardContainer = container;
                        Debug.Log("✓ 自动设置 MulliganUI.cardContainer");
                        changed = true;
                    }
                }
            }

            if (changed)
            {
                // 标记场景为已修改
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                    UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
                Debug.Log("=== 自动修复完成，请保存场景 ===");
            }
            else
            {
                Debug.Log("=== 没有需要修复的引用 ===");
            }

            // 再次验证
            ValidateScene();
        }
    }
}
