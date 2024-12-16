using MassTransit;

namespace Core.Bases;

public interface IEntity
{
}

public interface IEntity<TId> : IEntity
{
    public TId Id { get; set; }
}

public abstract class BaseEntity : BaseEntity<Guid>, IGuidIdentify
{
    protected BaseEntity() => Id = NewId.Next().ToGuid();
}

public abstract class BaseEntity<TId> : IEntity<TId>
{
    public TId Id { get; set; } = default!;

    public virtual DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public interface IGuidIdentify
{
    public Guid Id { get; set; }
}