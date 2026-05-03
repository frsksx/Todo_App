using System.Windows;

namespace WindowsTrayTasks.Views;

public partial class SettingsDialog : Window
{
    public bool ShowAgendaTab { get; set; }
    public bool SyncEnabled { get; set; }
    public string SupabaseUrl { get; set; } = "";
    public string SupabasePublishableKey { get; set; } = "";

    public SettingsDialog(bool showAgendaTab, bool syncEnabled = false, string? supabaseUrl = null, string? supabasePublishableKey = null)
    {
        ShowAgendaTab = showAgendaTab;
        SyncEnabled = syncEnabled;
        SupabaseUrl = supabaseUrl ?? "";
        SupabasePublishableKey = supabasePublishableKey ?? "";
        InitializeComponent();
        DataContext = this;
    }

    private void OK_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }
}
