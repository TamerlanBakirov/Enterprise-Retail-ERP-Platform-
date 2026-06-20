using GeorgiaERP.Domain.Common;

namespace GeorgiaERP.Domain.Products;

public class Category : BaseEntity
{
    public Guid? ParentId { get; private set; }
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? NameKa { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    // Navigation properties
    public Category? Parent { get; private set; }
    public ICollection<Category> Children { get; private set; } = new List<Category>();
    public ICollection<Product> Products { get; private set; } = new List<Product>();

    private Category() { }

    public static Category Create(string code, string name, string? nameKa = null, Guid? parentId = null, int sortOrder = 0)
    {
        return new Category
        {
            Code = code,
            Name = name,
            NameKa = nameKa,
            ParentId = parentId,
            SortOrder = sortOrder,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }
}
