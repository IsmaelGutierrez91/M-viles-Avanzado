using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerAvatar : NetworkBehaviour
{
    [SerializeField] bool esGuardian;
    [SerializeField] float velocidad = 6f;
    [SerializeField] NetworkObject cofrePrefab;
    [SerializeField] NetworkObject cuboPrefab;
    [SerializeField] TMP_Text etiqueta;
    [SerializeField] Renderer cuerpo;

    public readonly NetworkVariable<FixedString32Bytes> nombre = new();

    PlayerHealth salud;
    PlayerInventory inventario;
    CuboItem llevado;
    Cofre cofreActual;

    public static PlayerAvatar De(ulong clienteId)
    {
        var nm = NetworkManager.Singleton;
        if (nm.ConnectedClients.TryGetValue(clienteId, out var cliente) && cliente.PlayerObject != null)
            return cliente.PlayerObject.GetComponent<PlayerAvatar>();
        return null;
    }

    public override void OnNetworkSpawn()
    {
        salud = GetComponent<PlayerHealth>();
        inventario = GetComponent<PlayerInventory>();
        nombre.OnValueChanged += AlCambiarNombre;
        if (IsServer)
            nombre.Value = new FixedString32Bytes(esGuardian ? $"Guardián {OwnerClientId}" : $"Explorador {OwnerClientId}");
        AlCambiarNombre(default, nombre.Value);
        Pintar(salud.IsDead);
    }

    public override void OnNetworkDespawn()
    {
        nombre.OnValueChanged -= AlCambiarNombre;
    }

    void AlCambiarNombre(FixedString32Bytes anterior, FixedString32Bytes actual)
    {
        etiqueta.text = actual.ToString();
    }

    public void Pintar(bool muerto)
    {
        cuerpo.material.color = muerto ? Color.gray : Paleta.ColorDe(OwnerClientId);
    }

    void Update()
    {
        if (!IsSpawned || !IsOwner || salud.IsDead) return;

        var d = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
        if (d.sqrMagnitude > 0.01f)
        {
            d.Normalize();
            transform.position += d * velocidad * Time.deltaTime;
            transform.rotation = Quaternion.LookRotation(d);
        }

        if (Input.GetKeyDown(KeyCode.Q)) AgarrarLanzarRpc(transform.forward);
        if (Input.GetKeyDown(KeyCode.T)) RegalarRpc();
        if (Input.GetKeyDown(KeyCode.B)) GuardarRpc();
        if (Input.GetKeyDown(KeyCode.V)) SoltarDeMochilaRpc(0);
        if (Input.GetKeyDown(KeyCode.X)) ConsumirRpc();
        if (Input.GetKeyDown(KeyCode.G) && esGuardian) PedirCofreRpc(transform.position + transform.forward * 3f);

        if (Input.GetKeyDown(KeyCode.R))
        {
            var recurso = MasCercano<RecursoReclamable>();
            if (recurso != null) recurso.ReclamarRpc();
        }
        if (Input.GetKeyDown(KeyCode.Z))
        {
            var arbol = MasCercano<Arbol>();
            if (arbol != null) arbol.TalarRpc();
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            var cofre = MasCercano<Cofre>();
            if (cofre != null)
            {
                cofreActual = cofre;
                cofre.AbrirRpc(true);
            }
        }
        if (Input.GetKeyUp(KeyCode.E) && cofreActual != null)
        {
            cofreActual.AbrirRpc(false);
            cofreActual = null;
        }
    }

    T MasCercano<T>(float max = 3.5f) where T : Component
    {
        T mejor = null;
        float menor = max;
        foreach (var c in FindObjectsByType<T>(FindObjectsSortMode.None))
        {
            float d = Vector3.Distance(transform.position, c.transform.position);
            if (d < menor)
            {
                menor = d;
                mejor = c;
            }
        }
        return mejor;
    }

    PlayerAvatar OtroMasCercano(float max)
    {
        PlayerAvatar mejor = null;
        float menor = max;
        foreach (var p in FindObjectsByType<PlayerAvatar>(FindObjectsSortMode.None))
        {
            if (p == this || p.salud.IsDead) continue;
            float d = Vector3.Distance(transform.position, p.transform.position);
            if (d < menor)
            {
                menor = d;
                mejor = p;
            }
        }
        return mejor;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    void AgarrarLanzarRpc(Vector3 direccion)
    {
        if (salud.IsDead) return;
        if (llevado != null && llevado.IsSpawned)
        {
            llevado.Lanzar(direccion);
            llevado = null;
            return;
        }
        var cubo = CuboItem.MasCercanoLibre(transform.position, OwnerClientId);
        if (cubo == null) return;
        cubo.Agarrar(this);
        llevado = cubo;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    void RegalarRpc()
    {
        if (salud.IsDead) return;
        var cubo = CuboItem.MasCercanoDe(OwnerClientId, transform.position, 4f);
        var destino = OtroMasCercano(4f);
        if (cubo == null || destino == null) return;
        if (cubo == llevado) llevado = null;
        cubo.Soltar();
        cubo.NetworkObject.ChangeOwnership(destino.OwnerClientId);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    void GuardarRpc()
    {
        if (salud.IsDead) return;
        var cubo = CuboItem.MasCercanoDe(OwnerClientId, transform.position, 4f);
        if (cubo == null) return;
        if (!inventario.Anadir((TipoItem)cubo.tipo.Value, 1)) return;
        if (cubo == llevado) llevado = null;
        cubo.Soltar();
        cubo.NetworkObject.Despawn(true);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    void SoltarDeMochilaRpc(int indice)
    {
        if (salud.IsDead) return;
        if (!inventario.TomarUno(indice, out var tipo)) return;
        var posicion = transform.position + transform.forward * 1.5f + Vector3.up * 0.5f;
        var objeto = cuboPrefab.InstantiateAndSpawn(NetworkManager, ownerClientId: OwnerClientId, position: posicion, rotation: Quaternion.identity);
        objeto.GetComponent<CuboItem>().tipo.Value = (byte)tipo;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    void ConsumirRpc()
    {
        var cubo = CuboItem.MasCercanoDe(OwnerClientId, transform.position, 4f);
        if (cubo == null) return;
        if (cubo == llevado) llevado = null;
        cubo.Soltar();
        cubo.NetworkObject.Despawn(true);
        salud.Curar(10);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    void PedirCofreRpc(Vector3 posicion)
    {
        if (!esGuardian || salud.IsDead) return;
        if (Vector3.Distance(posicion, transform.position) > 4f) return;
        if (FindObjectsByType<Cofre>(FindObjectsSortMode.None).Length >= 3) return;
        posicion.y = 0.5f;
        cofrePrefab.InstantiateAndSpawn(NetworkManager, position: posicion, rotation: Quaternion.identity);
    }
}
