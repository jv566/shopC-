namespace Shop.Application.Products.Queries;

public sealed record GetProductListQuery(bool IncludeOutOfStock = true, int Page = 1, int PageSize = 20);

