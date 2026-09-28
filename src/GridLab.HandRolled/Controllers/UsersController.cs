using GridLab.Grid;
using GridLab.Shared.Models;

namespace GridLab.HandRolled.Controllers;

/// <summary>Thousands of rows, modal editing, a Razor template for one cell, an enum rendered as a drop-down automatically.</summary>
public sealed class UsersController(IGridStore<User> store) : GridController<User>(store)
{
    protected override GridDefinition<User> Define() => GridDefinition.For<User>(g => g
        .EditMode(GridEditMode.Modal)
        .Column(u => u.Email, c => c.Template(u => new Microsoft.AspNetCore.Html.HtmlString($"<a href=\"mailto:{u.Email}\">{System.Net.WebUtility.HtmlEncode(u.Email)}</a>")))
        .Column(u => u.LastLogin, c => c.Format("dd MMM yyyy"))
        .DefaultSort(u => u.LastName)
        .PageSizes(10, 25, 50, 100)
        .RowClass(u => u.IsActive ? null : "user-inactive"));

    protected override void OnSaving(User item, bool isNew)
    {
        if (isNew) item.CreatedOn = DateTime.UtcNow;
    }
}
