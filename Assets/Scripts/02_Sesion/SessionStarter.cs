using Unity.Netcode;
using UnityEngine;

public class SessionStarter : MonoBehaviour
{
    [SerializeField] NetworkObject guardianPrefab;
    [SerializeField] NetworkObject exploradorPrefab;

    void Start()
    {
        var nm = NetworkManager.Singleton;
        nm.NetworkConfig.ConnectionApproval = true;
        nm.ConnectionApprovalCallback = Aprobar;
        nm.OnConnectionEvent += AlConectar;
    }

    void OnDestroy()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null) return;
        nm.ConnectionApprovalCallback = null;
        nm.OnConnectionEvent -= AlConectar;
    }

    void Aprobar(NetworkManager.ConnectionApprovalRequest peticion, NetworkManager.ConnectionApprovalResponse respuesta)
    {
        bool primero = NetworkManager.Singleton.ConnectedClientsIds.Count == 0;
        var prefab = primero ? guardianPrefab : exploradorPrefab;
        respuesta.Approved = true;
        respuesta.CreatePlayerObject = true;
        respuesta.PlayerPrefabHash = prefab.PrefabIdHash;
        respuesta.Position = new Vector3(Random.Range(-4f, 4f), 1f, Random.Range(-4f, 4f));
        respuesta.Rotation = Quaternion.identity;
        respuesta.Pending = false;
    }

    void AlConectar(NetworkManager nm, ConnectionEventData datos)
    {
        Debug.Log($"[{(nm.IsServer ? "Servidor" : "Cliente")}] {datos.EventType} -> {datos.ClientId}");
    }

    void Update()
    {
        var nm = NetworkManager.Singleton;
        if (nm.IsListening) return;
        if (Input.GetKeyDown(KeyCode.H)) nm.StartHost();
        else if (Input.GetKeyDown(KeyCode.C)) nm.StartClient();
    }

    void OnGUI()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null) return;
        string texto = nm.IsListening
            ? "WASD mover | F golpear | R reclamar | Z talar | E abrir cofre | Q agarrar o lanzar | T regalar | B guardar | V soltar mochila | X consumir | G cofre (Guardián)"
            : "H = Host   C = Cliente";
        GUI.Label(new Rect(10, Screen.height - 30, Screen.width - 20, 24), texto);
    }
}
