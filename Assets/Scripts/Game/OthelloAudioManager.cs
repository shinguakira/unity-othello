using UnityEngine;

public class OthelloAudioManager : MonoBehaviour
{
    public static OthelloAudioManager Instance { get; private set; }

    AudioClip _placeSFX;
    AudioClip _flipSFX;
    AudioClip _gameOverSFX;
    AudioClip _passSFX;

    const int SampleRate = 44100;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _placeSFX = GeneratePlaceClip();
        _flipSFX = GenerateFlipClip();
        _gameOverSFX = GenerateGameOverClip();
        _passSFX = GeneratePassClip();
    }

    void OnEnable()
    {
        EventBus.Subscribe<PiecePlacedEvent>(OnPiecePlaced);
        EventBus.Subscribe<PiecesFlippedEvent>(OnPiecesFlipped);
        EventBus.Subscribe<GameOverEvent>(OnGameOver);
        EventBus.Subscribe<PassTurnEvent>(OnPassTurn);
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<PiecePlacedEvent>(OnPiecePlaced);
        EventBus.Unsubscribe<PiecesFlippedEvent>(OnPiecesFlipped);
        EventBus.Unsubscribe<GameOverEvent>(OnGameOver);
        EventBus.Unsubscribe<PassTurnEvent>(OnPassTurn);
    }

    void OnPiecePlaced(PiecePlacedEvent e) => Play(_placeSFX);
    void OnPiecesFlipped(PiecesFlippedEvent e) => Play(_flipSFX);
    void OnGameOver(GameOverEvent e) => Play(_gameOverSFX);
    void OnPassTurn(PassTurnEvent e) => Play(_passSFX);

    void Play(AudioClip clip)
    {
        if (AudioManager.Instance != null && clip != null)
            AudioManager.Instance.PlaySFX(clip);
    }

    AudioClip GeneratePlaceClip()
    {
        int samples = SampleRate * 60 / 1000; // 60ms
        var data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / SampleRate;
            data[i] = 0.5f * Mathf.Sin(2f * Mathf.PI * 800f * t) * Mathf.Exp(-t * 30f);
        }
        return CreateClip("Place", data);
    }

    AudioClip GenerateFlipClip()
    {
        int samples = SampleRate * 80 / 1000; // 80ms
        var data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / SampleRate;
            float freq = Mathf.Lerp(400f, 900f, (float)i / samples);
            float env = t < 0.04f ? t / 0.04f : 1f - (t - 0.04f) / 0.04f;
            env = Mathf.Clamp01(env);
            data[i] = 0.4f * Mathf.Sin(2f * Mathf.PI * freq * t) * env;
        }
        return CreateClip("Flip", data);
    }

    AudioClip GenerateGameOverClip()
    {
        float[] freqs = { 523f, 392f, 261f };
        int noteLen = SampleRate * 120 / 1000; // 120ms each
        int total = noteLen * freqs.Length;
        var data = new float[total];

        for (int n = 0; n < freqs.Length; n++)
        {
            for (int i = 0; i < noteLen; i++)
            {
                float t = (float)i / SampleRate;
                float env = 1f - (float)i / noteLen;
                data[n * noteLen + i] = 0.35f * Mathf.Sin(2f * Mathf.PI * freqs[n] * t) * env;
            }
        }
        return CreateClip("GameOver", data);
    }

    AudioClip GeneratePassClip()
    {
        int samples = SampleRate * 100 / 1000; // 100ms
        var data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / SampleRate;
            data[i] = 0.3f * Mathf.Sin(2f * Mathf.PI * 300f * t) * Mathf.Exp(-t * 20f);
        }
        return CreateClip("Pass", data);
    }

    AudioClip CreateClip(string name, float[] data)
    {
        var clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
