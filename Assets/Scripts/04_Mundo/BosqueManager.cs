using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class BosqueManager : MonoBehaviour
{
    public static BosqueManager Instance { get; private set; }

    [SerializeField] NetworkObject arbolPrefab;
    [SerializeField] Transform[] puntos;
    [SerializeField] float segundosReaparicion = 5f;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        NetworkManager.Singleton.OnServerStarted += SembrarBosque;
    }

    void OnDestroy()
    {
        if (NetworkManager.Singleton != null) NetworkManager.Singleton.OnServerStarted -= SembrarBosque;
    }

    void SembrarBosque()
    {
        foreach (var punto in puntos) Sembrar(punto.position);
    }

    void Sembrar(Vector3 posicion)
    {
        arbolPrefab.InstantiateAndSpawn(NetworkManager.Singleton, position: posicion, rotation: Quaternion.identity);
    }

    public void ProgramarReaparicion(Vector3 posicion)
    {
        StartCoroutine(EsperarYSembrar(posicion));
    }

    IEnumerator EsperarYSembrar(Vector3 posicion)
    {
        yield return new WaitForSeconds(segundosReaparicion);
        if (NetworkManager.Singleton.IsServer) Sembrar(posicion);
    }
}
