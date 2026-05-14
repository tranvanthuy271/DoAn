using System;
using Unity.Netcode;

/// <summary>
/// Các loại debuff (hiệu ứng bất lợi) áp dụng từ skill lên player hoặc enemy.
/// </summary>
public enum SkillDebuffType
{
    None       = 0,
    Slow       = 1,   // Giảm tốc độ di chuyển
    Weaken     = 2,   // Giảm attack %
    Burn       = 3,   // Dot damage mỗi giây
    Freeze     = 4,   // Không di chuyển được (stun)
    DefenseDown = 5,  // Giảm defense %
}

/// <summary>
/// Một debuff entry đang active trên target.
/// Dùng trong NetworkList nên phải implement INetworkSerializable.
/// </summary>
public struct DebuffEntry : INetworkSerializable, IEquatable<DebuffEntry>
{
    /// <summary>Loại debuff.</summary>
    public SkillDebuffType Type;

    /// <summary>Giá trị hiệu ứng: % giảm tốc, % giảm attack, damage/tick, % giảm defense...</summary>
    public int Value;

    /// <summary>Icon ID để hiển thị (dùng chung với Resources/ItemIcons/{iconId}.png).</summary>
    public int IconId;

    /// <summary>Tên debuff hiển thị trên UI.</summary>
    public Unity.Collections.FixedString64Bytes Name;

    /// <summary>Thời điểm hết hạn tính từ NetworkManager.ServerTime.TimeAsFloat (server time).</summary>
    public float ExpireServerTime;

    /// <summary>Tổng thời gian duration (giây) — dùng để tính % outline fade.</summary>
    public float TotalDuration;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Type);
        serializer.SerializeValue(ref Value);
        serializer.SerializeValue(ref IconId);
        serializer.SerializeValue(ref Name);
        serializer.SerializeValue(ref ExpireServerTime);
        serializer.SerializeValue(ref TotalDuration);
    }

    public bool Equals(DebuffEntry other)
    {
        return Type == other.Type
            && Value == other.Value
            && IconId == other.IconId
            && Name.Equals(other.Name)
            && ExpireServerTime.Equals(other.ExpireServerTime)
            && TotalDuration.Equals(other.TotalDuration);
    }

    public override bool Equals(object obj)
    {
        return obj is DebuffEntry other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hashCode = (int)Type;
            hashCode = (hashCode * 397) ^ Value;
            hashCode = (hashCode * 397) ^ IconId;
            hashCode = (hashCode * 397) ^ Name.GetHashCode();
            hashCode = (hashCode * 397) ^ ExpireServerTime.GetHashCode();
            hashCode = (hashCode * 397) ^ TotalDuration.GetHashCode();
            return hashCode;
        }
    }

    public static bool operator ==(DebuffEntry left, DebuffEntry right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(DebuffEntry left, DebuffEntry right)
    {
        return !left.Equals(right);
    }
}
