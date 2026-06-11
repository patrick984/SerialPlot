using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using SerialPlot.Models;

namespace SerialPlot.ViewModels;

public partial class ChannelViewModel(string name, int index) : ViewModelBase
{
    public string Name { get; } = name;
    public int Index { get; } = index;

    [ObservableProperty]
    private bool _canBeX;

    [ObservableProperty]
    private bool _canBeY;

    [ObservableProperty]
    private bool _isSelectedLeft;

    [ObservableProperty]
    private bool _isSelectedRight;

    [ObservableProperty]
    private IBrush? _leftTraceBrush;

    [ObservableProperty]
    private IBrush? _rightTraceBrush;

    public void Apply(ColumnState state)
    {
        CanBeX = state.CanBeX;
        CanBeY = state.CanBeY;
    }
}
