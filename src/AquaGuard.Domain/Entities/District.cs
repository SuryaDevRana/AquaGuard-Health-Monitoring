using AquaGuard.Domain.Common;

namespace AquaGuard.Domain.Entities;

public class District : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public ICollection<Village> Villages { get; set; } = new List<Village>();
}
