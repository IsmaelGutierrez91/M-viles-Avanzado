using System.Text;
using Unity.Netcode;
using UnityEngine;

public class PlayerInventory : NetworkBehaviour
{
    public const int MaxCasillas = 8;
    public const int MaxPila = 64;

    NetworkList<Ranura> inventario;
    string texto = "";

    void Awake()
    {
        inventario = new NetworkList<Ranura>(null, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Server);
    }

    public override void OnNetworkSpawn()
    {
        inventario.OnListChanged += AlCambiar;
        Refrescar();
    }

    public override void OnNetworkDespawn()
    {
        inventario.OnListChanged -= AlCambiar;
    }

    void AlCambiar(NetworkListEvent<Ranura> evento)
    {
        switch (evento.Type)
        {
            case NetworkListEvent<Ranura>.EventType.Add:
                Debug.Log($"[Mochila {OwnerClientId}] Add casilla {evento.Index}: {(TipoItem)evento.Value.tipo} x{evento.Value.cantidad}");
                break;
            case NetworkListEvent<Ranura>.EventType.RemoveAt:
                Debug.Log($"[Mochila {OwnerClientId}] RemoveAt casilla {evento.Index}: {(TipoItem)evento.PreviousValue.tipo}");
                break;
            case NetworkListEvent<Ranura>.EventType.Value:
                Debug.Log($"[Mochila {OwnerClientId}] Value casilla {evento.Index}: x{evento.PreviousValue.cantidad} -> x{evento.Value.cantidad}");
                break;
        }
        Refrescar();
    }

    void Refrescar()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Mochila");
        for (int i = 0; i < inventario.Count; i++)
            sb.AppendLine($"{i}: {(TipoItem)inventario[i].tipo} x{inventario[i].cantidad}");
        texto = sb.ToString();
    }

    void OnGUI()
    {
        if (!IsSpawned || !IsOwner) return;
        GUI.Label(new Rect(10, 10, 260, 220), texto);
    }

    public bool Anadir(TipoItem tipo, int cantidad)
    {
        if (!IsServer) return false;
        for (int i = 0; i < inventario.Count && cantidad > 0; i++)
        {
            var ranura = inventario[i];
            if (ranura.tipo != (byte)tipo || ranura.cantidad >= MaxPila) continue;
            int mover = Mathf.Min(MaxPila - ranura.cantidad, cantidad);
            ranura.cantidad += mover;
            inventario[i] = ranura;
            cantidad -= mover;
        }
        while (cantidad > 0 && inventario.Count < MaxCasillas)
        {
            int mover = Mathf.Min(MaxPila, cantidad);
            inventario.Add(new Ranura { tipo = (byte)tipo, cantidad = mover });
            cantidad -= mover;
        }
        return cantidad == 0;
    }

    public bool TomarUno(int indice, out TipoItem tipo)
    {
        tipo = TipoItem.Ninguno;
        if (!IsServer || indice < 0 || indice >= inventario.Count) return false;
        var ranura = inventario[indice];
        tipo = (TipoItem)ranura.tipo;
        ranura.cantidad--;
        if (ranura.cantidad <= 0) inventario.RemoveAt(indice);
        else inventario[indice] = ranura;
        return true;
    }
}
