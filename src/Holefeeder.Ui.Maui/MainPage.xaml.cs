namespace Holefeeder.Ui.Maui;

public partial class MainPage
{
    public MainPage()
    {
        InitializeComponent();
    }

#pragma warning disable CA1822
    public void NavigateToPurchase()
    {
        var mainComponent = Main.Instance;
        mainComponent?.NavigateToPurchase();
    }
#pragma warning restore CA1822
}
