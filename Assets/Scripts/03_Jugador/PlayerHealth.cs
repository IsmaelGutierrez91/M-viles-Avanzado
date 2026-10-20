using System.Collections;
using Unity.Netcode;
using UnityEngine.UI;
using Unity.Netcode.Components;
using UnityEngine;

public class PlayerHealth : NetworkBehaviour
{
    public const int Max = 100;

    [SerializeField] Slider barra;
    readonly NetworkVariable<int> health = new(Max);
    PlayerAvatar avatar;

    public int Vida => health.Value;
    public bool IsDead => health.Value <= 0;

    public override void OnNetworkSpawn()
    {
        avatar = GetComponent<PlayerAvatar>();
        health.OnValueChanged += AlCambiarVida;
        Actualizar();
    }

    public override void OnNetworkDespawn()
    {
        health.OnValueChanged -= AlCambiarVida;
    }

    void AlCambiarVida(int anterior, int actual)
    {
        Actualizar();
        if (IsServer && actual <= 0) StartCoroutine(Reaparecer());
    }

    void Actualizar()
    {
        float f = Mathf.Clamp01(health.Value / (float)Max);
        if (barra != null) barra.value = f;
        avatar.Pintar(IsDead);
    }

    public void RecibirDanio(int danio)
    {
        if (!IsServer || IsDead) return;
        health.Value = Mathf.Max(0, health.Value - danio);
    }

    public void Curar(int cantidad)
    {
        if (!IsServer || IsDead) return;
        health.Value = Mathf.Min(Max, health.Value + cantidad);
    }

    IEnumerator Reaparecer()
    {
        yield return new WaitForSeconds(3f);
        if (!IsSpawned) yield break;
        health.Value = Max;
        ReaparecerRpc(new Vector3(Random.Range(-4f, 4f), 1f, Random.Range(-4f, 4f)));
    }

    [Rpc(SendTo.Owner)]
    void ReaparecerRpc(Vector3 posicion)
    {
        GetComponent<NetworkTransform>().Teleport(posicion, Quaternion.identity, transform.localScale);
    }
}
