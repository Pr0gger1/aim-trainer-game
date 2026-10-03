using UnityEngine;

namespace UI
{
    [RequireComponent(typeof(AudioSource))]
    public class MenuMusicController : MonoBehaviour
    {
        [SerializeField] private float fadeInDuration = 2f;
        [SerializeField] private float targetVolume = 0.55f;

        private AudioSource _source;
        private float _timer;

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _source.loop = true;
            _source.playOnAwake = false;
            _source.volume = 0f;
            _source.Play();
        }

        public void StartFadeIn()
        {
            _timer = 0f;
        }

        private void Update()
        {
            if (_timer >= fadeInDuration) return;
            _timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(_timer / fadeInDuration);
            _source.volume = Mathf.Lerp(0f, targetVolume, Mathf.SmoothStep(0f, 1f, t));
        }
    }
}