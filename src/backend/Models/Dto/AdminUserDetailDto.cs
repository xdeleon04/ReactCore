namespace ReactCore.Backend.Models.Dto;

public class AdminUserDetailDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLogin { get; set; }

    public List<AdminUserOrderHistoryDto> OrderHistory { get; set; } = new();
    public AdminUserCartActivityDto CartActivity { get; set; } = new();
}

public class AdminUserOrderHistoryDto
{
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class AdminUserCartActivityDto
{
    public int ItemCount { get; set; }
    public DateTime? LastUpdated { get; set; }
}
