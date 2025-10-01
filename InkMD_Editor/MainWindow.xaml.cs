using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace InkMD_Editor
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            AppWindow.Resize(new Windows.Graphics.SizeInt32(1200 , 800));
            InitializeComponent();
            AppWindow.TitleBar.PreferredTheme = TitleBarTheme.UseDefaultAppMode;
            AppWindow.Title = "InkMD Editor";
        }

        private void Menu_Opening(object sender, object e)
        {
            CommandBarFlyout myFlyout = sender as CommandBarFlyout;
            if (myFlyout.Target == REBCustom)
            {
                AppBarButton myButton = new AppBarButton();
                myFlyout.PrimaryCommands.Add(myButton);
            }
        }

        private void REBCustom_Loaded(object sender, RoutedEventArgs e)
        {
            REBCustom.SelectionFlyout.Opening += Menu_Opening;
            REBCustom.ContextFlyout.Opening += Menu_Opening;
        }

        private void REBCustom_Unloaded(object sender, RoutedEventArgs e)
        {
            REBCustom.SelectionFlyout.Opening -= Menu_Opening;
            REBCustom.ContextFlyout.Opening -= Menu_Opening;
        }
    }
}
