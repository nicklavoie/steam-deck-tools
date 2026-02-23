using Microsoft.Gaming.XboxGameBar;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace NickGameBar.GameBarWidget;

sealed partial class App : Application
{
    private XboxGameBarWidget? _widget;

    public App()
    {
        InitializeComponent();
    }

    protected override async void OnActivated(IActivatedEventArgs args)
    {
        var frame = Window.Current.Content as Frame ?? new Frame();
        Window.Current.Content = frame;

        if (args is XboxGameBarWidgetActivatedEventArgs widgetArgs)
        {
            _widget = await XboxGameBarWidget.ActivateAsync(widgetArgs, Window.Current.CoreWindow, frame);
            frame.Navigate(typeof(MainPage), _widget);
        }
        else
        {
            frame.Navigate(typeof(MainPage), null);
        }

        Window.Current.Activate();
    }
}
