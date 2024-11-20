using UnityEngine;

// 판다 선택 효과를 관리하는 컴포넌트
public class PandaSelectionEffect : MonoBehaviour
{
    private Vector3 originalScale;
    private float selectedScale = 1.2f;
    private float animationDuration = 0.2f;
    private bool isSelected = false;
    
    private void Start()
    {
        originalScale = transform.localScale;
    }
    
    public void Select()
    {
        if (!isSelected)
        {
            isSelected = true;
            // 크기 확대 애니메이션
            LeanTween.scale(gameObject, originalScale * selectedScale, animationDuration)
                .setEase(LeanTweenType.easeOutBack);
        }
    }
    
    public void Deselect()
    {
        if (isSelected)
        {
            isSelected = false;
            // 원래 크기로 복귀 애니메이션
            LeanTween.scale(gameObject, originalScale, animationDuration)
                .setEase(LeanTweenType.easeInBack);
        }
    }
}
