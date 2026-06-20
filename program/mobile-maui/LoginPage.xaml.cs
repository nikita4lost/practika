namespace WarrantyReturns.Mobile;

public partial class LoginPage : ContentPage
{
    private const string SessionKey = "engineer_session_started";

    public LoginPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (await HasActiveSessionAsync())
        {
            Application.Current!.MainPage = new AppShell();
        }
    }

    private async void LoginButton_OnClicked(object sender, EventArgs e)
    {
        if (LoginEntry.Text?.Trim() != "engineer" || PasswordEntry.Text != "engineer123")
        {
            await DisplayAlert("Ошибка", "Введите логин engineer и пароль engineer123.", "OK");
            return;
        }

        await SecureStorage.SetAsync(SessionKey, DateTime.UtcNow.ToString("O"));
        Application.Current!.MainPage = new AppShell();
    }

    private static async Task<bool> HasActiveSessionAsync()
    {
        var value = await SecureStorage.GetAsync(SessionKey);
        if (!DateTime.TryParse(value, out var startedAt))
        {
            return false;
        }

        return DateTime.UtcNow - startedAt.ToUniversalTime() < TimeSpan.FromHours(24);
    }
}
