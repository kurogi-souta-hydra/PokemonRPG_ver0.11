using UnityEngine; // Unityの基本機能
using UnityEngine.SceneManagement; // シーン管理機能
using System.Collections; // コルーチン、時間を少し待つ
using System.Collections.Generic; // 型指定できるコレクション


public enum BattleState
{
    Start,
    PlayerAction, // 行動選択
    PlayerMove,   // 技選択
    EnemyMove,
    Busy,
}


public class BattleSystem : MonoBehaviour
{
    public static PokemonBase EnemyBase;
    public static int EnemyLevel;
    public static string EnemyID;
    // 4体撃破後ワープフラグ
	const string NextAreaFlag = "GoNextArea";

    [SerializeField] BattleUnit playerUnit;
    [SerializeField] BattleUnit enemyUnit;
    [SerializeField] BattleHud playerHud;
    [SerializeField] BattleHud enemyHud;
    [SerializeField] BattleDialogBox dialogBox;
    [SerializeField] BattleBackground battleBackground;
    [SerializeField] FadeManager fadeManager;

    BattleState state;

    int currentAction = 0; // 0:Fight, 1:Run
    int currentMove = 0;   // 0:左上, 1:右上 2:左下, 3:右下 の技
    
    // 効果音を使用します
    public AudioClip sound1;
    AudioSource audioSource;


    private void Start()
    {
        StartCoroutine(SetupBattle());
        // Componentを取得
        audioSource = GetComponent<AudioSource>();
    }


    IEnumerator SetupBattle()
    {
        if(EnemyBase == null)
		{
		    Debug.LogError("EnemyBaseが設定されていません");
		    SceneManager.LoadScene("Map Scene");
		    yield break;
		}
        Debug.Log(EnemyBase.name);
        Debug.Log(EnemyLevel);

        state = BattleState.Start;


        // モンスターの生成と描画
        playerUnit.Setup();
        enemyUnit.Setup(EnemyBase, EnemyLevel);
        
        battleBackground.SetBackground(EnemyID);


        // HUDの描画
        playerHud.SetData(playerUnit.Pokemon);
        enemyHud.SetData(enemyUnit.Pokemon);

        dialogBox.SetMoveNames(playerUnit.Pokemon.Moves);


        yield return(dialogBox.TypeDialog($"やせいの{enemyUnit.Pokemon.Base.Name}があらわれた"));

        PlayerAction();
    }


    void PlayerAction()
    {
        state = BattleState.PlayerAction;

        dialogBox.EnableActionSelector(true);

        StartCoroutine(dialogBox.TypeDialog("どうする？"));
    }


    void PlayerMove()
    {
        state = BattleState.PlayerMove;

        dialogBox.EnableDialogText(false);
        dialogBox.EnableActionSelector(false);
        dialogBox.EnableMoveSelector(true);
    }

    // PlayerMoveの実行
	IEnumerator PerformPlayerMove()
	{
	    state = BattleState.Busy;

	    Move move = playerUnit.Pokemon.Moves[currentMove];
	    
        // PPがないなら技を使えない
        if (move.PP <= 0)
        {
            yield return dialogBox.TypeDialog("PPがたりない！");
            yield return EnemyMove();
            yield break;
        }
        // PPを1減らす
        move.PP--;
        
	    yield return dialogBox.TypeDialog($"{playerUnit.Pokemon.Base.Name}は{move.Base.Name}をつかった");


	    playerUnit.PlayerAttackAnimation();
	    
        audioSource.PlayOneShot(sound1);   // 攻撃音
        
	    yield return new WaitForSeconds(0.7f);


	    enemyUnit.PlayerHitAnimation();
	    
        audioSource.PlayOneShot(sound1);   // 被弾音
        
	    DamageDetails damageDetails = null;

	    // 攻撃技ならダメージを与える
	    if (move.Base.Power > 0)
	    {
	        damageDetails = enemyUnit.Pokemon.TakeDamage(move, playerUnit.Pokemon);

	        yield return enemyHud.UpdateHP();

	        yield return ShowDamageDetails(damageDetails);

	        // 敵モンスターを倒したら
	        if (damageDetails.Fainted)
	        {
	            yield return dialogBox.TypeDialog($"{enemyUnit.Pokemon.Base.Name}は戦闘不能");

	            enemyUnit.PlayerFaintAnimation();

	            yield return new WaitForSeconds(1f);

	            BattleResult.isVictory = true;

	            Debug.Log("保存するID：" + EnemyID);

	            // 敵撃破処理
	            if (PlayerPrefs.GetInt(EnemyID, 0) == 0)
	            {
	                PlayerPrefs.SetInt(EnemyID, 1);

	                int count = PlayerPrefs.GetInt("DefeatedCount", 0);
	                count++;

	                PlayerPrefs.SetInt("DefeatedCount", count);

	                Debug.Log("倒した数：" + count);
	            }

	            PlayerPrefs.Save();

	            int defeatedCount = PlayerPrefs.GetInt("DefeatedCount", 0);

	            if (defeatedCount >= 5)
	            {
	                Debug.Log("5体撃破！最初の場所へ");
	                PlayerPrefs.DeleteAll();
	                Debug.Log("セーブデータをリセットしました");
	                yield return fadeManager.FadeOutStart();
	                SceneManager.LoadScene("Map Scene");
	            }
	            else if (defeatedCount >= 4)
	            {
	                Debug.Log("4体撃破！次の場所へ");
	                yield return fadeManager.FadeOutStart();
	                PlayerPrefs.SetInt(NextAreaFlag, 1);
	                PlayerPrefs.Save();
	            }

	            yield return fadeManager.FadeOutStart();
	            SceneManager.LoadScene("Map Scene");

	            yield break;
	        }
	    }

	    // 回復技なら回復する
	    if (move.Base.Heal > 0)
	    {
	        playerUnit.Pokemon.Heal(move.Base.Heal);

	        yield return playerHud.UpdateHP();
	    }

	    yield return EnemyMove();
	}



    // EnemyMoveの実行
    IEnumerator EnemyMove()
	{
	    state = BattleState.EnemyMove;

	    // 技を決定:ランダム
	    Move move = enemyUnit.Pokemon.GetRandomMove();

        // PPがないなら技を使えない
        if (move.PP <= 0)
        {
            yield return dialogBox.TypeDialog("PPがたりない！");
            yield return EnemyMove();
            yield break;
        }
        // PPを1減らす
        move.PP--;
        
	    yield return dialogBox.TypeDialog($"{enemyUnit.Pokemon.Base.Name}は{move.Base.Name}をつかった");


	    enemyUnit.PlayerAttackAnimation();
	    
        audioSource.PlayOneShot(sound1);   // 攻撃音
        
	    yield return new WaitForSeconds(0.7f);


	    playerUnit.PlayerHitAnimation();
	    
        audioSource.PlayOneShot(sound1);   // 被弾音
        
	    DamageDetails damageDetails = null;

	    // 攻撃技ならダメージを与える
	    if (move.Base.Power > 0)
	    {
	        damageDetails = playerUnit.Pokemon.TakeDamage(move, enemyUnit.Pokemon);

	        yield return playerHud.UpdateHP();

	        yield return ShowDamageDetails(damageDetails);

	        // プレイヤーが倒されたら
	        if (damageDetails.Fainted)
	        {
	            yield return dialogBox.TypeDialog($"{playerUnit.Pokemon.Base.Name}は戦闘不能");

	            playerUnit.PlayerFaintAnimation();

	            yield return new WaitForSeconds(1f);

	            BattleResult.isVictory = false;

	            int defeatedCount = PlayerPrefs.GetInt("DefeatedCount", 0);

	            if (defeatedCount >= 4)
	            {
	                Debug.Log("4体撃破！次の場所へ");

	                PlayerPrefs.SetInt(NextAreaFlag, 1);
	                PlayerPrefs.Save();
	            }

	            yield return fadeManager.FadeOutStart();
	            SceneManager.LoadScene("Map Scene");

	            yield break;
	        }
	    }

	    // 回復技なら回復する
	    if (move.Base.Heal > 0)
	    {
	        enemyUnit.Pokemon.Heal(move.Base.Heal);

	        yield return enemyHud.UpdateHP();
	    }

	    PlayerAction();
	}



    IEnumerator ShowDamageDetails(DamageDetails damageDetails)
    {
        if(damageDetails.Critical > 1f)
        {
            yield return dialogBox.TypeDialog($"急所にあたった");
        }


        if (damageDetails.TypeEffectiveness > 1f)
        {
            yield return dialogBox.TypeDialog($"効果はバツグンだ");
        }
        else if (damageDetails.TypeEffectiveness < 1f)
        {
            yield return dialogBox.TypeDialog($"効果はいまひとつ");
        }
    }


    IEnumerator RunAway()
    {
        state = BattleState.Busy;


        if (Random.Range(0, 100) < 50)
        {
            yield return dialogBox.TypeDialog("うまく にげきれた!");
            
			yield return fadeManager.FadeOutStart();
			yield return new WaitForSeconds(2f); // 2秒待機
            SceneManager.LoadScene("Map Scene");
        }
        else
        {
            yield return dialogBox.TypeDialog("にげられなかった!");

            yield return EnemyMove();
        }
    }


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) // Escapeが押されたら
	    {
	        HandleBack();
	    }
        if (state == BattleState.PlayerAction)
        {
            HandleActionSelection();
        }
        else if (state == BattleState.PlayerMove)
        {
            HandleMoveSelection();
        }
    }
    void HandleBack()
	{
	    if (state == BattleState.PlayerMove)
	    {
	        // 技の選択位置を初期化
        	currentMove = 0;
        	
	        // 技選択を終了
	        dialogBox.EnableMoveSelector(false);

	        // 行動選択を表示
	        dialogBox.EnableActionSelector(true);

	        // メッセージ表示を戻す
	        dialogBox.EnableDialogText(true);

	        state = BattleState.PlayerAction;

	        StartCoroutine(dialogBox.TypeDialog("どうする？"));
	    }
	}


    // PlayerActionでの行動を処理する
    void HandleActionSelection()
    {
        // 下を入力するとRun
        // 上を入力するとFight


        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            if (currentAction < 1)
            {
                currentAction++;
            }
        }


        if(Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            if (currentAction > 0)
            {
                currentAction--;
            }
        }


        dialogBox.UpdateActionSelection(currentAction);


        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            if (currentAction == 0)
            {
                PlayerMove();
            }
            else if (currentAction == 1)
            {
                StartCoroutine(RunAway());
            }
        }
    }
    // 0:左上
    // 1:右上
    // 2:左下
    // 3:右下
    void HandleMoveSelection()
    {
        if(Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (currentMove < playerUnit.Pokemon.Moves.Count - 1)
            {
                currentMove++;
            }
        }


        else if(Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (currentMove > 0)
            {
                currentMove--;
            }
        }


        else if(Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            if (currentMove < playerUnit.Pokemon.Moves.Count - 2)
            {
                currentMove += 2;
            }
        }


        else if(Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            if (currentMove > 1)
            {
                currentMove -= 2;
            }
        }


        dialogBox.UpdateMoveSelection(
            currentMove,
            playerUnit.Pokemon.Moves[currentMove]
        );


        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            // 技選択のUIを非表示
            dialogBox.EnableMoveSelector(false);

            // メッセージ復活
            dialogBox.EnableDialogText(true);

            // 技決定
            StartCoroutine(PerformPlayerMove());
        }
    }
}