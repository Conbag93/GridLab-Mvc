using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using Microsoft.AspNetCore.Html;

namespace GridLab.Grid;

/// <summary>Runtime (non-generic) description of one column, consumed by the Razor partials.</summary>
public sealed class GridColumnSpec
{
    internal GridColumnSpec(PropertyInfo property)
    {
        Property = property;
        Name = property.Name;
        PropertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        IsNullable = !property.PropertyType.IsValueType || Nullable.GetUnderlyingType(property.PropertyType) is not null;

        var display = property.GetCustomAttribute<DisplayAttribute>();
        Title = display?.GetName() ?? Humanize(property.Name);
        Format = property.GetCustomAttribute<DisplayFormatAttribute>()?.DataFormatString is string f ? StripBraces(f) : null;
        Visible = property.GetCustomAttribute<ScaffoldColumnAttribute>()?.Scaffold ?? true;
        Editable = property.CanWrite && (property.GetCustomAttribute<EditableAttribute>()?.AllowEdit ?? true);
        Type = Detect(PropertyType, property.GetCustomAttribute<DataTypeAttribute>()?.DataType);
        Searchable = Type == GridColumnType.Text;
        if (PropertyType.IsEnum) Options = EnumOptions(PropertyType);
    }

    public PropertyInfo Property { get; }
    public string Name { get; }
    public Type PropertyType { get; }
    public bool IsNullable { get; }
    public string Title { get; set; }
    public GridColumnType Type { get; set; }
    public string? Format { get; set; }
    public bool Visible { get; set; }
    public bool Editable { get; set; }
    public bool Sortable { get; set; } = true;
    public bool Searchable { get; set; }
    public GridAlign Align { get; set; } = GridAlign.Auto;
    public string? CssClass { get; set; }
    public string? Width { get; set; }
    public IReadOnlyList<GridOption>? Options { get; set; }
    public string? Placeholder { get; set; }
    public Func<object, IHtmlContent>? Template { get; set; }
    public Func<object, IHtmlContent>? EditorTemplate { get; set; }

    public object? Get(object row) => Property.GetValue(row);
    public void Set(object row, object? value) => Property.SetValue(row, value);

    public bool IsNumeric => Type == GridColumnType.Number;

    /// <summary>Text shown in a display cell.</summary>
    public string DisplayText(object row)
    {
        var v = Get(row);
        if (v is null) return "";
        if (Options is not null)
        {
            var key = ValueString(v);
            return Options.FirstOrDefault(o => o.Value == key)?.Text ?? key;
        }
        return v switch
        {
            bool b => b ? "Yes" : "No",
            DateOnly d => d.ToString(Format ?? "yyyy-MM-dd", CultureInfo.CurrentCulture),
            DateTime dt => dt.ToString(Format ?? (Type == GridColumnType.Date ? "yyyy-MM-dd" : "yyyy-MM-dd HH:mm"), CultureInfo.CurrentCulture),
            DateTimeOffset dto => dto.ToString(Format ?? "yyyy-MM-dd HH:mm", CultureInfo.CurrentCulture),
            IFormattable f when Format is not null => f.ToString(Format, CultureInfo.CurrentCulture),
            decimal m => m.ToString("N2", CultureInfo.CurrentCulture),
            double or float => Convert.ToDouble(v).ToString("N2", CultureInfo.CurrentCulture),
            Enum e => EnumText(e),
            _ => v.ToString() ?? "",
        };
    }

    /// <summary>Value for an input's value attribute (invariant, HTML input formats).</summary>
    public string InputValue(object row)
    {
        var v = Get(row);
        return v switch
        {
            null => "",
            DateOnly d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            DateTime dt => dt.ToString(Type == GridColumnType.Date ? "yyyy-MM-dd" : "yyyy-MM-dd'T'HH:mm", CultureInfo.InvariantCulture),
            _ => ValueString(v),
        };
    }

    public static string ValueString(object? v) => v switch
    {
        null => "",
        Enum e => e.ToString(),
        bool b => b ? "true" : "false",
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => v.ToString() ?? "",
    };

    private static GridColumnType Detect(Type t, DataType? hint)
    {
        if (t == typeof(bool)) return GridColumnType.Boolean;
        if (t.IsEnum) return GridColumnType.Select;
        if (t == typeof(DateOnly)) return GridColumnType.Date;
        if (t == typeof(DateTime) || t == typeof(DateTimeOffset))
            return hint == DataType.Date ? GridColumnType.Date : GridColumnType.DateTime;
        if (t == typeof(int) || t == typeof(long) || t == typeof(short) || t == typeof(byte)
            || t == typeof(decimal) || t == typeof(double) || t == typeof(float)) return GridColumnType.Number;
        return GridColumnType.Text;
    }

    internal static IReadOnlyList<GridOption> EnumOptions(Type enumType) =>
        Enum.GetValues(enumType).Cast<Enum>().Select(e => new GridOption(e.ToString(), EnumText(e))).ToList();

    private static string EnumText(Enum e) =>
        e.GetType().GetField(e.ToString())?.GetCustomAttribute<DisplayAttribute>()?.GetName() ?? Humanize(e.ToString());

    internal static string Humanize(string name)
    {
        var sb = new System.Text.StringBuilder(name.Length + 4);
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (i > 0 && char.IsUpper(c) && (char.IsLower(name[i - 1]) || (i + 1 < name.Length && char.IsLower(name[i + 1]))))
                sb.Append(' ');
            sb.Append(i > 0 && char.IsUpper(c) && i + 1 < name.Length && char.IsLower(name[i + 1]) ? char.ToLowerInvariant(c) : c);
        }
        return sb.ToString();
    }

    private static string StripBraces(string f)
    {
        var start = f.IndexOf("{0:", StringComparison.Ordinal);
        if (start < 0) return f;
        var end = f.IndexOf('}', start);
        return end < 0 ? f : f.Substring(start + 3, end - start - 3);
    }
}
