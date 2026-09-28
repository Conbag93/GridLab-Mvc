using GridLab.Grid;
using GridLab.Shared.Data;
using GridLab.Shared.Models;

namespace GridLab.HandRolled.Controllers;

/// <summary>Inline editing, a drop-down for the category, a read-only audit column stamped on save.</summary>
public sealed class ProductsController(IGridStore<Product> store, IRepository<Category> categories) : GridController<Product>(store)
{
    protected override GridDefinition<Product> Define() => GridDefinition.For<Product>(g => g
        .Title("Products")
        .Column(p => p.CategoryId, c => c.Title("Category").Dropdown(categories.GetAll(), x => x.Id, x => x.Name))
        .Column(p => p.UnitPrice, c => c.Format("N2").Width("8rem"))
        .Column(p => p.UnitsInStock, c => c.Width("7rem"))
        .Column(p => p.LastUpdated, c => c.ReadOnly())
        .DefaultSort(p => p.Name)
        .PageSizes(5, 10, 20));

    protected override void OnSaving(Product item, bool isNew) => item.LastUpdated = DateTime.UtcNow;
}
