using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(RecursoReclamable))]
public class Arbol : NetworkBehaviour
{
    [SerializeField] int golpesParaTalar = 3;
    [SerializeField] int troncosPorArbol = 2;
    [SerializeField] int monedasPorArbol = 5;

    int golpes;
    float ultimoGolpe;

    public override void OnNetworkSpawn()
    {
        golpes = 0;
        ultimoGolpe = -10f;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void TalarRpc(RpcParams rpcParams = default)
    {
        if (Time.time - ultimoGolpe < 0.3f) return;
        var avatar = PlayerAvatar.De(rpcParams.Receive.SenderClientId);
        if (avatar == null) return;
        if (Vector3.Distance(avatar.transform.position, transform.position) > 4f) return;
        ultimoGolpe = Time.time;
        golpes++;
        Debug.Log($"Arbol {NetworkObjectId}: golpe {golpes} de {golpesParaTalar}");
        if (golpes < golpesParaTalar) return;
        var mochila = avatar.GetComponent<PlayerInventory>();
        mochila.Anadir(TipoItem.Tronco, troncosPorArbol);
        mochila.Anadir(TipoItem.Moneda, monedasPorArbol);
        BosqueManager.Instance.ProgramarReaparicion(transform.position);
        NetworkObject.Despawn(true);
    }
}
