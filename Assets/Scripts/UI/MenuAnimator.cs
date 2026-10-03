using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class MenuAnimator : MonoBehaviour
    {
        [SerializeField] private RectTransform crosshair;
        [SerializeField] private RectTransform title;
        [SerializeField] private RectTransform subtitle;
        [SerializeField] private Graphic accentLine;
        [SerializeField] private float crosshairSpeed = 12f;
        [SerializeField] private float titleAmplitude = 6f;

        private Vector3 _titleBase;
        private Vector2 _subtitleBase;
        private float _elapsed;

        private void Start()
        {
            if (title != null) _titleBase = title.localPosition;
            if (subtitle != null) _subtitleBase = subtitle.anchoredPosition;
        }

        private void Update()
        {
            _elapsed += Time.unscaledDeltaTime;

            if (crosshair != null)
                crosshair.localRotation = Quaternion.Euler(0f, 0f, _elapsed * crosshairSpeed);

            if (title != null)
                title.localPosition = _titleBase + new Vector3(0f, Mathf.Sin(_elapsed * 1.4f) * titleAmplitude, 0f);

            if (subtitle != null)
                subtitle.anchoredPosition = _subtitleBase + new Vector2(0f, Mathf.Cos(_elapsed * 1.1f) * 4f);

            if (accentLine != null)
            {
                Color c = accentLine.color;
                c.a = 0.7f + Mathf.Sin(_elapsed * 1.8f) * 0.3f;
                accentLine.color = c;
            }
        }
    }
}