using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PoolDeRed : MonoBehaviour
{
    [SerializeField] GameObject[] prefabs;

    readonly List<GameObject> registrados = new();

    void Start()
    {
        var manejador = NetworkManager.Singleton.PrefabHandler;
        foreach (var prefab in prefabs)
        {
            manejador.AddHandler(prefab, new HandlerPool(prefab));
            registrados.Add(prefab);
        }
    }

    void OnDestroy()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null) return;
        foreach (var prefab in registrados) nm.PrefabHandler.RemoveHandler(prefab);
    }
}

public class HandlerPool : INetworkPrefabInstanceHandler
{
    readonly GameObject prefab;
    readonly Stack<NetworkObject> libres = new();

    public HandlerPool(GameObject prefab)
    {
        this.prefab = prefab;
    }

    public NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation)
    {
        if (libres.Count > 0)
        {
            var reciclado = libres.Pop();
            reciclado.transform.SetPositionAndRotation(position, rotation);
            reciclado.gameObject.SetActive(true);
            return reciclado;
        }
        return Object.Instantiate(prefab, position, rotation).GetComponent<NetworkObject>();
    }

    public void Destroy(NetworkObject networkObject)
    {
        networkObject.transform.SetParent(null);
        networkObject.gameObject.SetActive(false);
        libres.Push(networkObject);
    }
}
