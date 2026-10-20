using Unity.Netcode;
using UnityEngine;

public class Cofre : NetworkBehaviour
{
    public const ulong Libre = ulong.MaxValue;

    [SerializeField] NetworkObject cuboPrefab;
    [SerializeField] float segundosApertura = 2f;
    [SerializeField] Renderer rend;

    public readonly NetworkVariable<ulong> abriendo = new(Libre);

    float progreso;

    public override void OnNetworkSpawn()
    {
        abriendo.OnValueChanged += AlCambiar;
        Pintar(abriendo.Value);
    }

    public override void OnNetworkDespawn()
    {
        abriendo.OnValueChanged -= AlCambiar;
    }

    void AlCambiar(ulong anterior, ulong actual)
    {
        Pintar(actual);
    }

    void Pintar(ulong quien)
    {
        rend.material.color = quien == Libre ? new Color(0.8f, 0.6f, 0.2f) : Color.red;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void AbrirRpc(bool mantiene, RpcParams rpcParams = default)
    {
        ulong remitente = rpcParams.Receive.SenderClientId;
        if (mantiene)
        {
            if (abriendo.Value != Libre) return;
            var avatar = PlayerAvatar.De(remitente);
            if (avatar == null) return;
            if (Vector3.Distance(avatar.transform.position, transform.position) > 4f) return;
            abriendo.Value = remitente;
            progreso = 0f;
        }
        else if (abriendo.Value == remitente)
        {
            Cancelar();
        }
    }

    void Update()
    {
        if (!IsSpawned || !IsServer || abriendo.Value == Libre) return;
        var avatar = PlayerAvatar.De(abriendo.Value);
        if (avatar == null || Vector3.Distance(avatar.transform.position, transform.position) > 4.5f)
        {
            Cancelar();
            return;
        }
        progreso += Time.deltaTime;
        if (progreso < segundosApertura) return;
        ulong duenio = abriendo.Value;
        for (int i = 0; i < 3; i++)
        {
            var posicion = transform.position + new Vector3(Mathf.Cos(i * 2.1f), 1f, Mathf.Sin(i * 2.1f)) * 1.5f;
            cuboPrefab.InstantiateAndSpawn(NetworkManager, ownerClientId: duenio, position: posicion, rotation: Quaternion.identity);
        }
        Cancelar();
    }

    void Cancelar()
    {
        progreso = 0f;
        abriendo.Value = Libre;
    }
}
