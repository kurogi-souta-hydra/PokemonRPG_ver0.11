using UnityEngine; // Unityの基本機能
using UnityEngine.SceneManagement; // シーン管理機能
using System.Collections; // コルーチン、時間を少し待つ

public class PlayerController : MonoBehaviour
{
    [Header("移動")]
    [SerializeField] float moveSpeed = 8f;

    [Header("ジャンプ")]
    [SerializeField] float jumpForce = 12f;
    
    [Header("ホバリング")]
    [SerializeField] float hoverGravity = 0.3f;
    [SerializeField] float normalGravity = 1.2f;

    [Header("接地判定")]
    [SerializeField] Transform groundCheck;
    [SerializeField] float groundRadius = 0.4f;
    [SerializeField] LayerMask groundLayer;
    
    [Header("すり抜け足場")]
    [SerializeField] LayerMask PlatformLayer;
    
    // 「下キーを素早く2回押して床をすり抜ける」操作用
    private float lastDownKeyTime = -1f;
    [SerializeField] private float doubleTapTime = 0.3f;

    Rigidbody2D rb;
    Animator animator;
    bool isGrounded;
    float move;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }
    private void Start()
	{
	    if (PlayerPrefs.GetInt("GoNextArea", 0) == 1) // 新しいエリアへワープ
	    {
	        transform.position = new Vector3(22f, 182f);

	        PlayerPrefs.SetInt("GoNextArea", 0);
	        PlayerPrefs.Save();
	    }
	    else if (PlayerPrefs.HasKey("PlayerX")) // 元いた場所に戻る
	    {
	        float x = PlayerPrefs.GetFloat("PlayerX");
	        float y = PlayerPrefs.GetFloat("PlayerY");

	        transform.position = new Vector3(x, y, 0);
	    }
	}
    private void Update()
    {
        // 接地判定
        isGrounded = Physics2D.OverlapCircle(
            groundCheck.position,
            groundRadius,
            groundLayer
        );
        
        move = 0f;
        
        // 左移動
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) // GetKey押してる間ずっと
        {
            move = -1f;
            animator.SetFloat("moveX", -1);
            animator.SetFloat("moveY", 0);
        }
        // 右移動
        else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            move = 1f;
            animator.SetFloat("moveX", 1);
            animator.SetFloat("moveY", 0);
        }

        // 歩きアニメーション
        animator.SetBool("isMoving", move != 0);
        
        // ジャンプ
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.Space)) // GetKeyDown押したとき
        {
            rb.velocity = new Vector2(
                rb.velocity.x,
                jumpForce
            );
        }
        
        // 下キーを素早く2回押して床をすり抜ける
		if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
	    {
	        if (Time.time - lastDownKeyTime <= doubleTapTime)
	        {
	            DescendPlatform();   // 2回目なので降りる
	            
	        }

	        lastDownKeyTime = Time.time;
	    }

        // 空中にいる場合ホバリング
        if (!isGrounded && (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.Space)))
        {
            rb.gravityScale = hoverGravity;
            
        }
        // 地面にいる、またはキーを離した
        else
        {
            rb.gravityScale = normalGravity;
        }
        
        // デバッグ用
        if (Input.GetKeyDown(KeyCode.P))
		{
		    PlayerPrefs.DeleteAll();
		    Debug.Log("セーブデータをリセットしました");
		    SceneManager.LoadScene(
		        SceneManager.GetActiveScene().name
		    );
		}
		Debug.Log(rb.velocity.y);
    }
    void FixedUpdate()
	{
	    rb.velocity = new Vector2(
	        move * moveSpeed,
	        rb.velocity.y
	    );
	}

    // 敵と接触
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            Debug.Log("モンスターに遭遇");
            
            // 敵からプレイヤーへの方向
			Vector2 dir = (transform.position - other.transform.position).normalized;

			// 敵から2m離れた位置
			Vector2 respawnPos = (Vector2)transform.position + dir * 2f;

			// その位置を保存
			PlayerPrefs.SetFloat("PlayerX", respawnPos.x);
			PlayerPrefs.SetFloat("PlayerY", respawnPos.y);
			PlayerPrefs.Save();
			
            Enemy enemy = other.GetComponent<Enemy>();

            if(enemy != null)
            {
                BattleSystem.EnemyBase = enemy.pokemonBase;
                BattleSystem.EnemyLevel = enemy.level;
                BattleSystem.EnemyID = enemy.enemyID; // 倒した敵を保存するためのID

                SceneManager.LoadScene("Battle Scene");
            }
        }
    }

    /// <summary>
	/// すり抜け床から降りる
	/// </summary>
	private void DescendPlatform()
	{
	    Debug.Log("DescendPlatform");

	    float radius = 0.5f;

	    Collider2D[] platforms = Physics2D.OverlapCircleAll(
	        groundCheck.position,
	        radius,
	        PlatformLayer
	    );

	    Debug.Log("見つかった足場：" + platforms.Length);

	    foreach (Collider2D platform in platforms)
	    {
	        Debug.Log("Collider : " + platform.name);

	        Platform platformScript = platform.GetComponent<Platform>();

	        Debug.Log("Platform : " + platformScript);

	        if (platformScript != null)
	        {
	            platformScript.DisablePlatform();
	        }
	    }
	}
    
    // 接地判定の可視化
    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;

        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(
            groundCheck.position,
            groundRadius
        );
    }
}