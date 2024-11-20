using UnityEngine;
using TMPro;

public class GameUIController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI scoreText;
    
    [Header("Format Settings")]
    [SerializeField] private string timeFormat = "Time: {0:00}";
    [SerializeField] private string scoreFormat = "Score: {0}";
    
    private GameManager gameManager;
    
    private void Start()
    {
        // 같은 게임오브젝트에서 GameManager 참조 가져오기
        gameManager = GetComponent<GameManager>();
        
        if (gameManager == null)
        {
            Debug.LogError("GameManager not found on the same GameObject!");
            enabled = false;
            return;
        }
        
        // UI 컴포넌트 유효성 검사
        if (timeText == null || scoreText == null)
        {
            Debug.LogError("Some UI references are missing!");
            enabled = false;
            return;
        }
        
        // 초기값 설정
        UpdateTimeDisplay(gameManager.remainingTime);
        UpdateScoreDisplay(gameManager.currentScore);
    }
    
    private void Update()
    {
        if (!gameManager.isGameActive) return;
        
        UpdateTimeDisplay(gameManager.remainingTime);
        UpdateScoreDisplay(gameManager.currentScore);
    }
    
    private void UpdateTimeDisplay(float timeValue)
    {
        if (timeText != null)
        {
            timeText.text = string.Format(timeFormat, Mathf.Max(0, timeValue));
        }
    }
    
    private void UpdateScoreDisplay(int scoreValue)
    {
        if (scoreText != null)
        {
            scoreText.text = string.Format(scoreFormat, scoreValue);
        }
    }
}