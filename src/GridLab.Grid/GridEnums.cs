namespace GridLab.Grid;

public enum GridEditMode
{
    /// <summary>The row being edited turns into inputs in place (default).</summary>
    Inline,
    /// <summary>A form panel is rendered above the grid.</summary>
    Form,
    /// <summary>A form is rendered in a dialog over the grid.</summary>
    Modal,
}

public enum GridColumnType { Text, Number, Date, DateTime, Boolean, Select, Custom }

public enum GridAlign { Auto, Left, Center, Right }

/// <summary>One choice in a select column.</summary>
public sealed record GridOption(string Value, string Text);
