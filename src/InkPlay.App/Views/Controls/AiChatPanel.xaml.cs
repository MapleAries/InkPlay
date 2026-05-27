using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace InkPlay.App.Views.Controls;

public sealed partial class AiChatPanel : UserControl
{
    public static readonly DependencyProperty OnSendMessageProperty =
        DependencyProperty.Register(nameof(OnSendMessage), typeof(string), typeof(AiChatPanel), new(null));

    public string? OnSendMessage
    {
        get => GetValue(OnSendMessageProperty) as string;
        set => SetValue(OnSendMessageProperty, value);
    }

    public event EventHandler<string>? MessageSent;
    public event EventHandler<string>? QuickAction;

    public AiChatPanel()
    {
        InitializeComponent();
    }

    private void Send_Click(object sender, RoutedEventArgs e)
    {
        SendMessage();
    }

    private void InputBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            SendMessage();
        }
    }

    private void SendMessage()
    {
        var text = InputBox.Text;
        if (!string.IsNullOrWhiteSpace(text))
        {
            MessageSent?.Invoke(this, text);
            InputBox.Text = string.Empty;
        }
    }

    private void Continue_Click(object sender, RoutedEventArgs e)
    {
        QuickAction?.Invoke(this, "continue");
    }

    private void Rewrite_Click(object sender, RoutedEventArgs e)
    {
        QuickAction?.Invoke(this, "rewrite");
    }

    private void Polish_Click(object sender, RoutedEventArgs e)
    {
        QuickAction?.Invoke(this, "polish");
    }

    private void Expand_Click(object sender, RoutedEventArgs e)
    {
        QuickAction?.Invoke(this, "expand");
    }
}
