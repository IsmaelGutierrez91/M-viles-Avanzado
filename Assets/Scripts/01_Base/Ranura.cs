using System;
using Unity.Netcode;

public enum TipoItem : byte { Ninguno = 0, Cubo = 1, Tronco = 2, Moneda = 3 }

public struct Ranura : INetworkSerializable, IEquatable<Ranura>
{
    public byte tipo;
    public int cantidad;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref tipo);
        serializer.SerializeValue(ref cantidad);
    }

    public bool Equals(Ranura otra) => tipo == otra.tipo && cantidad == otra.cantidad;
    public override bool Equals(object obj) => obj is Ranura r && Equals(r);
    public override int GetHashCode() => HashCode.Combine(tipo, cantidad);
}
