using System.Data;

namespace BlazorApp1.Components.Pages;

public sealed partial class Home
{
    private DataTable? dataTable;

    protected override void OnInitialized()
    {
        dataTable = new DataTable();
        _ = dataTable.Columns.Add("ID", typeof(int));
        _ = dataTable.Columns.Add("Name", typeof(string));
        _ = dataTable.Rows.Add(1, "Alice");
        _ = dataTable.Rows.Add(2, "Bob");
        _ = dataTable.Rows.Add(3, "Charlie");
    }
}
