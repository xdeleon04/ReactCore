using ReactCore.Backend.Models.Dto;

namespace ReactCore.Backend.Services;

public class InventoryConflictException : Exception
{
    public IReadOnlyList<InventoryConflictDto> Conflicts { get; }

    public InventoryConflictException(string message, IReadOnlyList<InventoryConflictDto> conflicts)
        : base(message)
    {
        Conflicts = conflicts;
    }
}

public class InvalidCartException : Exception
{
    public InvalidCartException(string message) : base(message) { }
}

public class EmptyCartException : Exception
{
    public EmptyCartException(string message) : base(message) { }
}
