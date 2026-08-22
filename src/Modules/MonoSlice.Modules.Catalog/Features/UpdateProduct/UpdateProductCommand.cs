using System.ComponentModel.DataAnnotations;
using MonoSlice.Modules.Catalog.Features.CreateProduct;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Catalog.Features.UpdateProduct;

public sealed record UpdateProductCommand : ICommand<ApiResponse<ProductDto>>
{
    [Required]
    public Guid Id { get; init; }

    [Required]
    [MaxLength(200)]
    public string Name { get; init; } = string.Empty;

    [Range(0.01, 1_000_000)]
    public decimal Price { get; init; }

    [MaxLength(1000)]
    public string? Description { get; init; }
}
