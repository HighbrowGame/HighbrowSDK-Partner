using System.Collections;
using UnityEngine;

namespace Highbrow.UI
{
    public enum DirectionType
    {
        Up,
        Down,
        Left,
        Right
    }

    public enum EaseType
    {
        Linear,
        EaseIn,
        EaseOut,
        EaseInOut
    }

    [RequireComponent(typeof(RectTransform))]
    public class HbrwUISlider : MonoBehaviour
    {
        [Header("Direction Settings")]
        public DirectionType Direction = DirectionType.Down;

        [Header("Ease Action Settings")]
        public EaseType EaseType = EaseType.Linear;
        public float EaseRate = 1f;

        [Header("이동 거리 (px)")]
        public float Distance = 100f;
        [Header("초당 이동 거리 (px/s)")]
        public float Speed = 2f;
        [Header("이동 시작 지연 시간 (sec)")]
        public float Delay = 0f;

        private RectTransform _rectTransform;
        private Vector2 _originalPos;
        private Coroutine _moveCoroutine;

        void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _originalPos = _rectTransform.anchoredPosition;
        }

        /// <summary>
        /// 오브젝트를 설정된 방향으로 이동시킵니다.
        /// </summary>
        public void Move()
        {
            if (_moveCoroutine != null) StopCoroutine(_moveCoroutine);
            _moveCoroutine = StartCoroutine(MoveRoutine(_originalPos + GetDirectionVector() * Distance));
        }

        /// <summary>
        /// 오브젝트를 원래 위치로 되돌립니다.
        /// </summary>
        public void ResetPosition(bool immediatly = false)
        {
            if (_moveCoroutine != null) StopCoroutine(_moveCoroutine);
            if (immediatly)
            {
                if (_rectTransform == null)
                    _rectTransform = GetComponent<RectTransform>();
                _rectTransform.anchoredPosition = _originalPos;
            }
            else
            {
                _moveCoroutine = StartCoroutine(MoveRoutine(_originalPos));
            }
        }

        private IEnumerator MoveRoutine(Vector2 targetPosition)
        {
            yield return new WaitForSeconds(Delay);

            Vector2 startPosition = _rectTransform.anchoredPosition;
            float elapsedTime = 0f;
            float duration = Speed > 0f ? (Distance / Speed) : 0.5f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / duration);
                float easedT = ApplyEase(t);

                _rectTransform.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, easedT);
                yield return null;
            }

            _rectTransform.anchoredPosition = targetPosition;
            _moveCoroutine = null;
        }

        private Vector2 GetDirectionVector()
        {
            return Direction switch
            {
                DirectionType.Up => Vector2.up,
                DirectionType.Down => Vector2.down,
                DirectionType.Left => Vector2.left,
                DirectionType.Right => Vector2.right,
                _ => Vector2.zero,
            };
        }

        private float ApplyEase(float t)
        {
            return EaseType switch
            {
                EaseType.EaseIn => Mathf.Pow(t, EaseRate),
                EaseType.EaseOut => 1f - Mathf.Pow(1f - t, EaseRate),
                EaseType.EaseInOut => t < 0.5f ? 2f * Mathf.Pow(t, EaseRate) : 1f - Mathf.Pow(-2f * t + 2f, EaseRate) / 2f,
                _ => t,
            };
        }
    }
}
