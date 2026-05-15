namespace Santase.UI
{
    using Santase.UI.Pages;

    public partial class AppShell : Shell
    {
        public AppShell()
        {
            this.InitializeComponent();

            Routing.RegisterRoute("GamePage", typeof(GamePage));
        }
    }
}
