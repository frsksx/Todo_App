using System.Windows;

namespace WindowsTrayTasks.Views;

public partial class SimplePromptWindow : Window
{
    public string Result { get; private set; } = "";

    public SimplePromptWindow(string title, string label)
    {
        InitializeComponent();
        Title = title;
        LabelText.Text = label;
        Loaded += (_, _) => InputBox.Focus();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        Result = InputBox.Text;
        DialogResult = true;
        Close();
    }
}
