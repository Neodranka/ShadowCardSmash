using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ShadowCardSmash.Core.Data;
using ShadowCardSmash.Core.Effects;
using ShadowCardSmash.Core.Rules;
using ShadowCardSmash.Managers;
using ShadowCardSmash.UI.Battle;

namespace ShadowCardSmash.Tests
{
    /// <summary>
    /// 本地热座游戏管理器 - 两个玩家在同一台设备上轮流操作
    /// </summary>
    public class HotSeatGameManager : MonoBehaviour
    {
        [Header("References")]
        public BattleUIController battleUI;
        public GameController gameController;

        [Header("Player Switch UI")]
        public GameObject playerSwitchPrompt;
        public TextMeshProUGUI switchMessageText;
        public TextMeshProUGUI nextPlayerText;
        public Button confirmSwitchButton;

        [Header("Game Over UI")]
        public GameObject gameOverPanel;
        public TextMeshProUGUI gameOverText;
        public Button restartButton;
        public Button quitButton;

        [Header("Test Settings")]
        public bool useTestDeck = true;
        public int randomSeed = -1; // -1 = 随机

        // 内部状态
        private TestCardDatabase _cardDatabase;
        private int _currentViewingPlayer = 0;
        private bool _isWaitingForSwitch = false;

        void Start()
        {
            Debug.Log("=== HotSeatGameManager Start() 被调用 ===");

            // 初始化UI
            InitializeUI();

            // 开始游戏
            InitializeTestGame();
        }

        void InitializeUI()
        {
            // 隐藏切换提示
            if (playerSwitchPrompt != null)
            {
                playerSwitchPrompt.SetActive(false);
            }

            // 隐藏游戏结束面板
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }

            // 绑定按钮事件
            if (confirmSwitchButton != null)
            {
                confirmSwitchButton.onClick.AddListener(OnConfirmSwitch);
            }

            if (restartButton != null)
            {
                restartButton.onClick.AddListener(RestartGame);
            }

            if (quitButton != null)
            {
                quitButton.onClick.AddListener(QuitGame);
            }
        }

        void InitializeTestGame()
        {
            Debug.Log("=== HotSeatGameManager: InitializeTestGame 开始 ===");

            try
            {
                // 1. 创建测试卡牌数据库
                Debug.Log("Step 1: 创建测试卡牌数据库");
                _cardDatabase = new TestCardDatabase();
                Debug.Log($"  卡牌数据库创建成功，卡牌数量: {_cardDatabase.GetAllCards()?.Count ?? 0}");

                // 2. 创建游戏控制器（如果没有引用）
                Debug.Log("Step 2: 创建游戏控制器");
                if (gameController == null)
                {
                    var go = new GameObject("GameController_Runtime");
                    gameController = go.AddComponent<GameController>();
                    Debug.Log("  GameController 已动态创建");
                }
                else
                {
                    Debug.Log("  GameController 已存在引用");
                }

                // 3. 初始化游戏控制器
                Debug.Log("Step 3: 初始化游戏控制器");
                gameController.Initialize(_cardDatabase);

                // 4. 创建测试牌库
                Debug.Log("Step 4: 创建测试牌库");
                var deck0 = CreateTestDeckData("玩家1卡组", HeroClass.ClassA);
                var deck1 = CreateTestDeckData("玩家2卡组", HeroClass.ClassB);
                Debug.Log($"  牌库创建成功: P0={deck0.cards.Count}张, P1={deck1.cards.Count}张");

                // 5. 使用随机种子
                int seed = randomSeed >= 0 ? randomSeed : UnityEngine.Random.Range(0, int.MaxValue);
                Debug.Log($"Step 5: 使用随机种子 {seed}");

                // 6. 初始化本地游戏
                Debug.Log("Step 6: 初始化本地游戏");
                gameController.InitializeLocalGame(deck0, deck1, seed);

                // 7. 订阅游戏事件
                Debug.Log("Step 7: 订阅游戏事件");
                gameController.OnTurnChanged += OnTurnChanged;
                gameController.OnGameOver += OnGameOver;

                // 8. 初始化UI
                Debug.Log($"Step 8: 初始化UI (battleUI is null: {battleUI == null})");
                if (battleUI != null)
                {
                    // 设置 BattleUIController 的 gameController 引用
                    battleUI.gameController = gameController;
                    battleUI.Initialize(_cardDatabase, _currentViewingPlayer);
                    Debug.Log("  BattleUIController 初始化完成");
                }
                else
                {
                    Debug.LogError("  battleUI 为空！请在 Inspector 中设置引用");
                }

                // 9. 开始游戏（会自动触发 UI 刷新）
                Debug.Log("Step 9: 开始游戏");
                gameController.StartGame();

                Debug.Log($"=== HotSeatGameManager: 游戏初始化完成 ===");
                Debug.Log($"  种子={seed}，先手玩家={gameController.CurrentState?.currentPlayerId}");
                Debug.Log($"  P0 手牌: {gameController.CurrentState?.players[0]?.hand?.Count}");
                Debug.Log($"  P1 手牌: {gameController.CurrentState?.players[1]?.hand?.Count}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"=== HotSeatGameManager: 初始化失败 ===");
                Debug.LogError($"错误信息: {e.Message}");
                Debug.LogError($"堆栈跟踪: {e.StackTrace}");
            }
        }

        DeckData CreateTestDeckData(string name, HeroClass heroClass)
        {
            var deck = DeckData.Create(name, heroClass);

            // 使用测试数据库的卡牌创建牌库
            var cardIds = _cardDatabase.CreateTestDeck();

            foreach (var cardId in cardIds)
            {
                // 检查是否已有该卡牌
                bool found = false;
                foreach (var entry in deck.cards)
                {
                    if (entry.cardId == cardId)
                    {
                        entry.count++;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    deck.cards.Add(new DeckEntry(cardId, 1));
                }
            }

            return deck;
        }

        #region Turn Management

        void OnTurnChanged(int playerId)
        {
            Debug.Log($"HotSeatGameManager: 回合切换到玩家{playerId}");

            // 如果回合切换到另一个玩家，显示切换提示
            if (playerId != _currentViewingPlayer)
            {
                ShowPlayerSwitchPrompt(playerId);
            }
            else
            {
                // 刷新UI
                battleUI?.RefreshAllUI();
            }
        }

        void ShowPlayerSwitchPrompt(int nextPlayerId)
        {
            _isWaitingForSwitch = true;

            if (playerSwitchPrompt != null)
            {
                playerSwitchPrompt.SetActive(true);
            }

            if (switchMessageText != null)
            {
                switchMessageText.text = "回合结束！\n请将设备交给对方玩家";
            }

            if (nextPlayerText != null)
            {
                nextPlayerText.text = $"轮到：玩家 {nextPlayerId + 1}";
            }
        }

        void OnConfirmSwitch()
        {
            if (!_isWaitingForSwitch) return;

            _isWaitingForSwitch = false;

            // 隐藏提示
            if (playerSwitchPrompt != null)
            {
                playerSwitchPrompt.SetActive(false);
            }

            // 切换视角
            SwitchPlayerView();
        }

        /// <summary>
        /// 切换玩家视角
        /// </summary>
        public void SwitchPlayerView()
        {
            _currentViewingPlayer = 1 - _currentViewingPlayer;

            Debug.Log($"HotSeatGameManager: 切换到玩家{_currentViewingPlayer}视角");

            // 更新游戏控制器的本地玩家
            gameController.SetControlledPlayer(_currentViewingPlayer);

            // 重新初始化UI
            if (battleUI != null)
            {
                battleUI.Initialize(_cardDatabase, _currentViewingPlayer);
                battleUI.RefreshAllUI();
            }
        }

        #endregion

        #region Game Over

        void OnGameOver(int winnerId, string reason)
        {
            Debug.Log($"HotSeatGameManager: 游戏结束，胜者=玩家{winnerId + 1}，原因={reason}");

            ShowGameOverUI(winnerId, reason);
        }

        void ShowGameOverUI(int winnerId, string reason)
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
            }

            if (gameOverText != null)
            {
                string resultText = $"游戏结束！\n\n";
                resultText += $"胜者：玩家 {winnerId + 1}\n";
                resultText += $"原因：{reason}";
                gameOverText.text = resultText;
            }
        }

        void RestartGame()
        {
            Debug.Log("HotSeatGameManager: 重新开始游戏");

            // 隐藏游戏结束面板
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }

            // 重置状态
            _currentViewingPlayer = 0;
            _isWaitingForSwitch = false;

            // 取消订阅旧事件
            if (gameController != null)
            {
                gameController.OnTurnChanged -= OnTurnChanged;
                gameController.OnGameOver -= OnGameOver;
            }

            // 重新初始化游戏
            InitializeTestGame();
        }

        void QuitGame()
        {
            Debug.Log("HotSeatGameManager: 退出游戏");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        #endregion

        #region Debug

        void Update()
        {
            // 调试快捷键
            if (Input.GetKeyDown(KeyCode.F1))
            {
                // 打印当前游戏状态
                PrintGameState();
            }

            if (Input.GetKeyDown(KeyCode.F2))
            {
                // 强制切换视角（调试用）
                SwitchPlayerView();
            }

            if (Input.GetKeyDown(KeyCode.F3))
            {
                // 强制结束回合（调试用）
                gameController?.TryEndTurn();
            }
        }

        void PrintGameState()
        {
            if (gameController?.CurrentState == null)
            {
                Debug.Log("游戏状态为空");
                return;
            }

            var state = gameController.CurrentState;
            var p0 = state.GetPlayer(0);
            var p1 = state.GetPlayer(1);

            Debug.Log($"=== 游戏状态 ===");
            Debug.Log($"回合: {state.turnNumber}, 当前玩家: {state.currentPlayerId}");
            Debug.Log($"玩家0: HP={p0.health}, PP={p0.mana}/{p0.maxMana}, 手牌={p0.hand.Count}, 牌库={p0.deck.Count}");
            Debug.Log($"玩家1: HP={p1.health}, PP={p1.mana}/{p1.maxMana}, 手牌={p1.hand.Count}, 牌库={p1.deck.Count}");
            Debug.Log($"当前视角: 玩家{_currentViewingPlayer}");
        }

        #endregion

        void OnDestroy()
        {
            // 清理事件订阅
            if (gameController != null)
            {
                gameController.OnTurnChanged -= OnTurnChanged;
                gameController.OnGameOver -= OnGameOver;
            }
        }
    }
}
