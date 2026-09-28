using GridLab.Grid;
using GridLab.Grid.Paging;
using GridLab.Shared.Data;
using GridLab.Shared.Models;

namespace GridLab.HandRolled.Controllers;

/// <summary>Form-panel editing, a static drop-down for country, an enum drop-down for kind, a custom search that also matches the user id.</summary>
public sealed class AddressesController(IGridStore<Address> store) : GridController<Address>(store)
{
    protected override GridDefinition<Address> Define() => GridDefinition.For<Address>(g => g
        .Title("Addresses")
        .EditMode(GridEditMode.Form)
        .Column(a => a.UserId, c => c.Title("User #").Width("6rem"))
        .Column(a => a.Country, c => c.Dropdown(SeedData.Countries, x => x.Code, x => x.Name))
        .Column(a => a.PostCode, c => c.Searchable())
        .DefaultSort(a => a.City));

    // "Search will be passed to the controller": exact-match a user id in addition to the grid's own text search.
    protected override IQueryable<Address> Filter(IQueryable<Address> source, GridQuery query) =>
        int.TryParse(query.Search, out var userId) ? source.Where(a => a.UserId == userId) : source;

    protected override PagedResult<Address> Load(GridQuery query)
    {
        if (!int.TryParse(query.Search, out _)) return base.Load(query);

        // Filter() already applied the numeric search; skip the text search by giving the pipeline a query without it.
        var source = Filter(((IQueryableGridStore<Address>)Store).Query(), query);
        return source.ApplySort(Definition.Spec, new GridQuery { Sort = query.Sort, Desc = query.Desc }).ApplyPaging(Definition.Spec, query);
    }
}
