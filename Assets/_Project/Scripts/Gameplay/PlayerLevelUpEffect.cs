using UnityEngine;

public sealed class PlayerLevelUpEffect : MonoBehaviour
{
    [SerializeField] private AutoBattleUnit player;
    [SerializeField] private GameObject levelUpEffectPrefab;
    [SerializeField] private Vector3 offset;
    [SerializeField] private bool parentToPlayer = true;
    [SerializeField] private float destroyDelay = 2f;

    private void Awake()
    {
        if (player == null)
        {
            player = GetComponent<AutoBattleUnit>();
        }
    }

    private void OnEnable()
    {
        if (player != null)
        {
            player.LevelChanged += PlayLevelUpEffect;
        }
    }

    private void OnDisable()
    {
        if (player != null)
        {
            player.LevelChanged -= PlayLevelUpEffect;
        }
    }

    private void PlayLevelUpEffect(int level)
    {
        if (levelUpEffectPrefab == null)
        {
            return;
        }

        Transform effectParent = parentToPlayer ? transform : null;
        var effect = Instantiate(levelUpEffectPrefab, transform.position + offset, Quaternion.identity, effectParent);

        var particles = effect.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particles.Length; i++)
        {
            particles[i].Clear(true);
            particles[i].Play(true);
        }

        if (destroyDelay > 0f)
        {
            Destroy(effect, destroyDelay);
        }
    }
}
