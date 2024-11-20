using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public int width = 7;
    public int height = 7;
    public float timeLimitInSeconds = 60f;
    public GameObject[] pandaPrefabs;
    public float dropSpeed = 5f;
    public int scorePerMatch = 100;

    public GameObject[,] allPandas;
    public float remainingTime;
    public bool isGameActive = true;
    public int currentScore = 0;

    public GameObject selectedPanda = null;
    public Vector2Int selectedPosition;

    private Vector2 centerOffset;

    public float matchDisplayDuration = 0.8f; // 매칭된 판다들이 사라지기 전 대기 시간
    public Color matchHighlightColor = Color.red; // 매칭된 판다들의 하이라이트 색상

    void Start()
    {
        allPandas = new GameObject[width, height];
        remainingTime = timeLimitInSeconds;
        
        centerOffset = new Vector2(
            -(width - 1) / 2f,
            -(height - 1) / 2f
        );
        
        InitializeBoard();  // 이 함수는 유지하고 내부 로직만 변경
    }

    private void InitializeBoard()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                CreatePandaWithoutMatch(x, y);
            }
        }
    }

    void Update()
    {
        if (!isGameActive) return;

        remainingTime -= Time.deltaTime;
        if (remainingTime <= 0)
        {
            GameOver();
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            // 마우스 위치를 그리드 좌표로 변환
            Vector2 adjustedPos = mousePos - centerOffset;
            Vector2Int gridPosition = Vector2Int.RoundToInt(adjustedPos);

            // 그리드 좌표를 배열 인덱스로 변환
            int arrayX = gridPosition.x;
            int arrayY = gridPosition.y;

            if (IsValidPosition(arrayX, arrayY))
            {
                HandlePandaSelection(arrayX, arrayY);
            }
        }
    }

    private void HandlePandaSelection(int x, int y)
    {
        if (!IsValidPosition(x, y)) return;
        
        GameObject clickedPanda = allPandas[x, y];
        
        if (selectedPanda == null)
        {
            selectedPanda = clickedPanda;
            selectedPosition = new Vector2Int(x, y);
            
            // 새로 선택된 판다에 효과 적용
            selectedPanda.GetComponent<PandaSelectionEffect>().Select();
            
            // DEBUG: 선택된 판다 정보 출력
            PandaType pandaType = selectedPanda.GetComponent<PandaType>();
            Debug.Log($"Selected Panda at ({x}, {y}) - Type: {pandaType.type}");
        }
        else
        {
            Vector2Int newPosition = new Vector2Int(x, y);
            if (IsAdjacent(selectedPosition, newPosition))
            {
                // 스왑할 때는 선택 해제하지 않습니다.
                SwapPandas(selectedPosition, newPosition);
                // selectedPanda는 매치 처리 시에 null로 설정됩니다.
            }
            else
            {
                // 인접하지 않은 곳을 클릭한 경우에만 이전 선택 해제하고 새로 선택
                selectedPanda.GetComponent<PandaSelectionEffect>().Deselect();
                selectedPanda = clickedPanda;
                selectedPosition = new Vector2Int(x, y);
                selectedPanda.GetComponent<PandaSelectionEffect>().Select();
            }
        }
    }

    private bool IsValidPosition(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    private bool IsAdjacent(Vector2Int pos1, Vector2Int pos2)
    {
        return Mathf.Abs(pos1.x - pos2.x) + Mathf.Abs(pos1.y - pos2.y) == 1;
    }

    private void SwapPandas(Vector2Int pos1, Vector2Int pos2)
    {
        if (!IsValidPosition(pos1.x, pos1.y) || !IsValidPosition(pos2.x, pos2.y)) return;

        GameObject temp = allPandas[pos1.x, pos1.y];
        allPandas[pos1.x, pos1.y] = allPandas[pos2.x, pos2.y];
        allPandas[pos2.x, pos2.y] = temp;

        // 위치 업데이트 시 오프셋 적용
        Vector3 worldPos1 = GridToWorldPosition(pos1.x, pos1.y);
        Vector3 worldPos2 = GridToWorldPosition(pos2.x, pos2.y);
        
        allPandas[pos1.x, pos1.y].transform.position = worldPos1;
        allPandas[pos2.x, pos2.y].transform.position = worldPos2;

        // 두 위치 중 하나라도 매칭이 있는지 확인
        bool hasMatch1 = CheckMatch(pos1.x, pos1.y);
        bool hasMatch2 = CheckMatch(pos2.x, pos2.y);

        // 둘 다 매칭이 없을 때만 원위치 및 선택 해제
        if (!hasMatch1 && !hasMatch2)
        {
            StartCoroutine(SwapBackAfterDelay(pos1, pos2));
            selectedPanda.GetComponent<PandaSelectionEffect>().Deselect();
            selectedPanda = null;
        }
    }

    private IEnumerator SwapBackAfterDelay(Vector2Int pos1, Vector2Int pos2)
{
    yield return new WaitForSeconds(0.5f);
    
    // 원위치시킬 때는 매칭 체크하지 않음
    GameObject temp = allPandas[pos1.x, pos1.y];
    allPandas[pos1.x, pos1.y] = allPandas[pos2.x, pos2.y];
    allPandas[pos2.x, pos2.y] = temp;

    Vector3 worldPos1 = GridToWorldPosition(pos1.x, pos1.y);
    Vector3 worldPos2 = GridToWorldPosition(pos2.x, pos2.y);
    
    allPandas[pos1.x, pos1.y].transform.position = worldPos1;
    allPandas[pos2.x, pos2.y].transform.position = worldPos2;
}

    private void CreatePandaWithoutMatch(int x, int y)
    {
        if (!IsValidPosition(x, y)) return;

        List<int> availableTypes = new List<int>();
        for (int i = 0; i < pandaPrefabs.Length; i++)
        {
            availableTypes.Add(i);
        }

        // 현재 위치에서 사용할 수 없는 타입 제거
        if (x >= 2)
        {
            // 왼쪽 두 개의 팬더가 같은 타입이면 그 타입은 제외
            if (allPandas[x-1, y] != null && allPandas[x-2, y] != null)
            {
                int leftType = allPandas[x-1, y].GetComponent<PandaType>().type;
                if (leftType == allPandas[x-2, y].GetComponent<PandaType>().type)
                {
                    availableTypes.Remove(leftType);
                }
            }
        }

        if (y >= 2)
        {
            // 아래 두 개의 팬더가 같은 타입이면 그 타입은 제외
            if (allPandas[x, y-1] != null && allPandas[x, y-2] != null)
            {
                int bottomType = allPandas[x, y-1].GetComponent<PandaType>().type;
                if (bottomType == allPandas[x, y-2].GetComponent<PandaType>().type)
                {
                    availableTypes.Remove(bottomType);
                }
            }
        }

        // 남은 타입들 중에서 랜덤하게 선택
        int randomIndex = availableTypes[Random.Range(0, availableTypes.Count)];
        Vector3 position = GridToWorldPosition(x, y);
        GameObject newPanda = Instantiate(pandaPrefabs[randomIndex], position, Quaternion.identity);
        newPanda.transform.parent = transform;

        PandaType pandaType = newPanda.AddComponent<PandaType>();
        pandaType.type = randomIndex;

        allPandas[x, y] = newPanda;
    }
    
    private Vector3 GridToWorldPosition(int x, int y)
    {
        return new Vector3(x + centerOffset.x, y + centerOffset.y, 0);
    }

    private void CreatePanda(int x, int y)
    {
        if (!IsValidPosition(x, y)) return;

        int randomIndex = Random.Range(0, pandaPrefabs.Length);
        Vector3 position = GridToWorldPosition(x, y);
        GameObject newPanda = Instantiate(pandaPrefabs[randomIndex], position, Quaternion.identity);
        newPanda.transform.parent = transform;
    
        PandaType pandaType = newPanda.AddComponent<PandaType>();
        pandaType.type = randomIndex;
    
        allPandas[x, y] = newPanda;
    }

    private bool CheckMatch(int x, int y)
    {
        if (allPandas[x, y] == null) return false;  // null 체크 추가

        List<GameObject> horizontalMatches = FindHorizontalMatches(x, y);
        List<GameObject> verticalMatches = FindVerticalMatches(x, y);

        bool hasMatch = horizontalMatches.Count >= 3 || verticalMatches.Count >= 3;
    
        if (hasMatch)
        {
            List<GameObject> allMatches = new List<GameObject>();
            allMatches.AddRange(horizontalMatches);
            allMatches.AddRange(verticalMatches);
            HandleMatches(allMatches);
        }

        return hasMatch;
    }

    private List<GameObject> FindHorizontalMatches(int x, int y)
    {
        List<GameObject> matches = new List<GameObject>();
        if (allPandas[x, y] == null) return matches;

        int pandaType = allPandas[x, y].GetComponent<PandaType>().type;
        
        // 초기에는 가로 매칭만 확인
        List<GameObject> horizontalLine = new List<GameObject>();
        horizontalLine.Add(allPandas[x, y]);

        // 왼쪽 확인
        for (int i = x - 1; i >= 0; i--)
        {
            if (allPandas[i, y] != null && allPandas[i, y].GetComponent<PandaType>().type == pandaType)
                horizontalLine.Add(allPandas[i, y]);
            else
                break;
        }

        // 오른쪽 확인
        for (int i = x + 1; i < width; i++)
        {
            if (allPandas[i, y] != null && allPandas[i, y].GetComponent<PandaType>().type == pandaType)
                horizontalLine.Add(allPandas[i, y]);
            else
                break;
        }

        // 가로로 3개 이상 연결된 경우에만 처리
        if (horizontalLine.Count >= 3)
        {
            // HashSet을 사용하여 중복 제거
            HashSet<GameObject> allConnected = new HashSet<GameObject>();
            
            // 가로 라인의 각 판다에 대해 연결된 모든 판다를 찾음
            foreach (var panda in horizontalLine)
            {
                if (panda != null)
                {
                    Vector2Int pos = Vector2Int.RoundToInt(panda.transform.position - (Vector3)centerOffset);
                    FindConnectedPandas(pos.x, pos.y, pandaType, allConnected);
                }
            }
            
            return new List<GameObject>(allConnected);
        }

        return new List<GameObject>();
    }

    private List<GameObject> FindVerticalMatches(int x, int y)
    {
        List<GameObject> matches = new List<GameObject>();
        if (allPandas[x, y] == null) return matches;

        int pandaType = allPandas[x, y].GetComponent<PandaType>().type;
        
        // 초기에는 세로 매칭만 확인
        List<GameObject> verticalLine = new List<GameObject>();
        verticalLine.Add(allPandas[x, y]);

        // 아래쪽 확인
        for (int i = y - 1; i >= 0; i--)
        {
            if (allPandas[x, i] != null && allPandas[x, i].GetComponent<PandaType>().type == pandaType)
                verticalLine.Add(allPandas[x, i]);
            else
                break;
        }

        // 위쪽 확인
        for (int i = y + 1; i < height; i++)
        {
            if (allPandas[x, i] != null && allPandas[x, i].GetComponent<PandaType>().type == pandaType)
                verticalLine.Add(allPandas[x, i]);
            else
                break;
        }

        // 세로로 3개 이상 연결된 경우에만 처리
        if (verticalLine.Count >= 3)
        {
            // HashSet을 사용하여 중복 제거
            HashSet<GameObject> allConnected = new HashSet<GameObject>();
            
            // 세로 라인의 각 판다에 대해 연결된 모든 판다를 찾음
            foreach (var panda in verticalLine)
            {
                if (panda != null)
                {
                    Vector2Int pos = Vector2Int.RoundToInt(panda.transform.position - (Vector3)centerOffset);
                    FindConnectedPandas(pos.x, pos.y, pandaType, allConnected);
                }
            }
            
            return new List<GameObject>(allConnected);
        }

        return new List<GameObject>();
    }

    private void FindConnectedPandas(int x, int y, int targetType, HashSet<GameObject> matches)
    {
        // 유효하지 않은 위치나 이미 처리된 판다는 스킵
        if (!IsValidPosition(x, y) || 
            allPandas[x, y] == null || 
            allPandas[x, y].GetComponent<PandaType>().type != targetType || 
            matches.Contains(allPandas[x, y]))
            return;

        // 현재 판다를 매칭 목록에 추가
        matches.Add(allPandas[x, y]);

        // 상하좌우 모든 방향의 연결된 판다를 재귀적으로 확인
        FindConnectedPandas(x + 1, y, targetType, matches);  // 오른쪽
        FindConnectedPandas(x - 1, y, targetType, matches);  // 왼쪽
        FindConnectedPandas(x, y + 1, targetType, matches);  // 위쪽
        FindConnectedPandas(x, y - 1, targetType, matches);  // 아래쪽
    }

    private void HandleMatches(List<GameObject> matches)
    {
        HashSet<GameObject> uniqueMatches = new HashSet<GameObject>(matches);
        StartCoroutine(HandleMatchesWithEffect(uniqueMatches));
    }

    private IEnumerator HandleMatchesWithEffect(HashSet<GameObject> matches)
    {
        // 매칭된 판다들 하이라이트 효과
        foreach (GameObject match in matches)
        {
            if (match != null && match != selectedPanda)  // 선택된 판다가 아닌 경우에만
            {
                PandaSelectionEffect selectionEffect = match.GetComponent<PandaSelectionEffect>();
                if (selectionEffect != null)
                {
                    selectionEffect.Select();
                }
                SpriteRenderer renderer = match.GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    // LeanTween.color(match, matchHighlightColor, 0.2f);
                }
            }
            else if (match == selectedPanda)  // 선택된 판다는 크기 유지하고 색상만 변경
            {
                SpriteRenderer renderer = match.GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    // LeanTween.color(match, matchHighlightColor, 0.2f);
                }
            }
        }

        // 매칭 확인을 위한 대기 시간
        yield return new WaitForSeconds(matchDisplayDuration);

        // 사라지는 이펙트와 함께 제거
        foreach (GameObject match in matches)
        {
            if (match != null)
            {
                Vector2Int position = Vector2Int.RoundToInt(match.transform.position - (Vector3)centerOffset);
                if (IsValidPosition(position.x, position.y))
                {
                    // 선택 상태 처리를 삭제 이펙트 전에 해제
                    if (match == selectedPanda)
                    {
                        selectedPanda = null;  // 선택 상태만 해제하고 크기는 유지
                    }
                    
                    // 현재 크기에서 시작해서 0으로 줄어들도록 수정
                    Vector3 currentScale = match.transform.localScale;
                    LeanTween.scale(match, Vector3.zero, 0.3f).setEase(LeanTweenType.easeInBack);
                    LeanTween.alpha(match, 0f, 0.3f).setOnComplete(() => {
                        allPandas[position.x, position.y] = null;
                        Destroy(match);
                        currentScore += scorePerMatch;
                    });
                }
            }
        }

        // 이펙트가 완료될 때까지 대기
        yield return new WaitForSeconds(0.3f);
        
        StartCoroutine(DropPandas());
    }

    private IEnumerator DropPandas()
    {
        yield return new WaitForSeconds(0.2f);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (allPandas[x, y] == null)
                {
                    // 현재 위치 위의 팡들을 아래로 이동
                    for (int yAbove = y + 1; yAbove < height; yAbove++)
                    {
                        if (allPandas[x, yAbove] != null)
                        {
                            allPandas[x, y] = allPandas[x, yAbove];
                            allPandas[x, yAbove] = null;
                            
                            // 월드 좌표로 변환하여 이동
                            Vector3 newPosition = GridToWorldPosition(x, y);
                            allPandas[x, y].transform.position = newPosition;
                            break;
                        }
                    }

                    // 빈 공간에 새 팡 생성
                    if (allPandas[x, y] == null)
                    {
                        CreatePanda(x, y);
                    }
                }
            }
        }

        yield return new WaitForSeconds(0.5f);
        CheckAllMatches();
    }

    private void CheckAllMatches()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                CheckMatch(x, y);
            }
        }
    }

    private void GameOver()
    {
        isGameActive = false;
        Debug.Log($"Game Over! Final Score: {currentScore}");
    }
}