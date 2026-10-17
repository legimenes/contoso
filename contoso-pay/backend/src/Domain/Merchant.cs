namespace Domain;

public sealed class Merchant(
    Guid id,
    string name,
    string document,
    DateTime createdAtUtc)
{
    public Guid Id { get; } = id;
    public string Name { get; } = name;
    public string Document { get; } = document;
    public DateTime CreatedAtUtc { get; } = createdAtUtc;

    public static Merchant New(
        string name,
        string document)
    { 
        return new(
            id: Guid.NewGuid(),
            name: name.Trim(),
            document: document.Trim(),
            createdAtUtc: DateTime.UtcNow);
    }
}