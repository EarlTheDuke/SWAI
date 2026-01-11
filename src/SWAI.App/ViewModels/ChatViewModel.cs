using CommunityToolkit.Mvvm.ComponentModel;

namespace SWAI.App.ViewModels;

/// <summary>
/// View model for the chat component
/// </summary>
public partial class ChatViewModel : ObservableObject
{
    [ObservableProperty]
    private string _inputText = string.Empty;

    [ObservableProperty]
    private bool _isTyping;
}
