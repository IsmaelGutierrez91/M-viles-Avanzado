using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class RecursoReclamable : NetworkBehaviour
{
    [SerializeField] Renderer rend;
    [SerializeField] TMP_Text etiqueta;

    public readonly NetworkVariable<Color> color = new(Color.gray);
    public readonly NetworkVariable<FixedString32Bytes> nombreDueno = new();

    public override void OnNetworkSpawn()
    {
        color.OnValueChanged += AlCambiarColor;
        nombreDueno.OnValueChanged += AlCambiarNombre;
        if (IsServer)
        {
            color.Value = Color.gray;
            nombreDueno.Value = default;
        }
        AlCambiarColor(default, color.Value);
        AlCambiarNombre(default, nombreDueno.Value);
    }

    public override void OnNetworkDespawn()
    {
        color.OnValueChanged -= AlCambiarColor;
        nombreDueno.OnValueChanged -= AlCambiarNombre;
    }

    void AlCambiarColor(Color anterior, Color actual)
    {
        rend.material.color = actual;
    }

    void AlCambiarNombre(FixedString32Bytes anterior, FixedString32Bytes actual)
    {
        etiqueta.text = actual.ToString();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void ReclamarRpc(RpcParams rpcParams = default)
    {
        if (!HasAuthority) return;
        ulong remitente = rpcParams.Receive.SenderClientId;
        if (!IsOwnedByServer || !nombreDueno.Value.IsEmpty) return;
        var avatar = PlayerAvatar.De(remitente);
        if (avatar == null) return;
        if (Vector3.Distance(avatar.transform.position, transform.position) > 5f) return;
        NetworkObject.ChangeOwnership(remitente);
        color.Value = Paleta.ColorDe(remitente);
        nombreDueno.Value = avatar.nombre.Value;
    }
}
