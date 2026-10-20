using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class CuboItem : NetworkBehaviour
{
    [SerializeField] Renderer rend;

    public readonly NetworkVariable<byte> tipo = new((byte)TipoItem.Cubo);

    Rigidbody rb;
    NetworkRigidbody nrb;
    NetworkTransform nt;
    Vector3 origen;

    public bool EstaLlevado { get; private set; }

    public override void OnNetworkSpawn()
    {
        rb = GetComponent<Rigidbody>();
        nrb = GetComponent<NetworkRigidbody>();
        nt = GetComponent<NetworkTransform>();
        tipo.OnValueChanged += AlCambiarTipo;
        EstaLlevado = false;
        origen = transform.position;
        if (IsServer) ReiniciarFisica();
        Pintar();
    }

    public override void OnNetworkDespawn()
    {
        tipo.OnValueChanged -= AlCambiarTipo;
        if (IsServer) ReiniciarFisica();
        EstaLlevado = false;
    }

    protected override void OnOwnershipChanged(ulong anterior, ulong actual)
    {
        Pintar();
    }

    public override void OnGainedOwnership()
    {
        Debug.Log($"Cubo {NetworkObjectId}: nuevo dueño {OwnerClientId}");
    }

    public override void OnLostOwnership()
    {
        Debug.Log($"Cubo {NetworkObjectId}: perdió al dueño anterior");
    }

    void AlCambiarTipo(byte anterior, byte actual)
    {
        Pintar();
    }

    void Pintar()
    {
        if (rend == null) return;
        switch ((TipoItem)tipo.Value)
        {
            case TipoItem.Tronco:
                transform.localScale = new Vector3(0.35f, 0.35f, 1f);
                rend.material.color = new Color(0.45f, 0.28f, 0.12f);
                break;
            case TipoItem.Moneda:
                transform.localScale = new Vector3(0.5f, 0.1f, 0.5f);
                rend.material.color = Color.yellow;
                break;
            default:
                transform.localScale = Vector3.one * 0.6f;
                rend.material.color = Paleta.ColorDe(OwnerClientId);
                break;
        }
    }

    void ReiniciarFisica()
    {
        if (rb == null || rb.isKinematic) return;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    void Update()
    {
        if (!IsSpawned || !IsServer) return;
        if (transform.position.y > -10f) return;
        ReiniciarFisica();
        nt.Teleport(origen, Quaternion.identity, transform.localScale);
    }

    public void Agarrar(PlayerAvatar jugador)
    {
        if (!IsServer) return;
        if (!NetworkObject.TrySetParent(jugador.NetworkObject, false)) return;
        nrb.SetIsKinematic(true);
        transform.localPosition = new Vector3(0f, 1.6f, 0f);
        transform.localRotation = Quaternion.identity;
        EstaLlevado = true;
    }

    public void Soltar()
    {
        if (!IsServer || !EstaLlevado) return;
        NetworkObject.TryRemoveParent(true);
        nrb.SetIsKinematic(false);
        EstaLlevado = false;
    }

    public void Lanzar(Vector3 direccion)
    {
        if (!IsServer) return;
        Soltar();
        var impulso = (direccion.normalized + Vector3.up * 0.5f).normalized;
        rb.AddForce(impulso * 8f, ForceMode.Impulse);
    }

    public static CuboItem MasCercanoLibre(Vector3 desde, ulong cliente, float max = 3.5f)
    {
        CuboItem mejor = null;
        float menor = max;
        foreach (var c in FindObjectsByType<CuboItem>(FindObjectsSortMode.None))
        {
            if (!c.IsSpawned || c.EstaLlevado) continue;
            if (c.OwnerClientId != cliente && !c.IsOwnedByServer) continue;
            float d = Vector3.Distance(desde, c.transform.position);
            if (d < menor)
            {
                menor = d;
                mejor = c;
            }
        }
        return mejor;
    }

    public static CuboItem MasCercanoDe(ulong cliente, Vector3 desde, float max)
    {
        CuboItem mejor = null;
        float menor = max;
        foreach (var c in FindObjectsByType<CuboItem>(FindObjectsSortMode.None))
        {
            if (!c.IsSpawned || c.OwnerClientId != cliente) continue;
            float d = Vector3.Distance(desde, c.transform.position);
            if (d < menor)
            {
                menor = d;
                mejor = c;
            }
        }
        return mejor;
    }
}
