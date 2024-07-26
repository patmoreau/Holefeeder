namespace Holefeeder.Ui.Maui;

public partial class App
{
    public App()
    {
        InitializeComponent();

        MainPage = new MainPage();
    }

    public void PerformActionForShortcutItem(string shortcutItemType)
    {
        if (shortcutItemType == "com.drifterapps.Holefeeder.purchase")
        {
            NavigateToPurchase();
        }
    }

    private void NavigateToPurchase()
    {
        var mainPage = MainPage as MainPage;
        mainPage?.NavigateToPurchase();
    }
}
