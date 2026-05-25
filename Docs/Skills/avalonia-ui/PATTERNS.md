# Avalonia UI Patterns

Reusable patterns for common UI components.

---

## Animated Sidebar

Collapsible sidebar with smooth animation.

### ViewModel

```csharp
public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isSidebarVisible = true;

    [ObservableProperty]
    private double _sidebarWidth = 220;

    private const double SidebarExpandedWidth = 220;
    private const double SidebarCollapsedWidth = 0;

    partial void OnIsSidebarVisibleChanged(bool value)
    {
        SidebarWidth = value ? SidebarExpandedWidth : SidebarCollapsedWidth;
    }

    [RelayCommand]
    private void ToggleSidebar() => IsSidebarVisible = !IsSidebarVisible;
}
```

### Style

```xml
<Style Selector="Border.sidebar-animated">
    <Setter Property="ClipToBounds" Value="True"/>
    <Setter Property="Transitions">
        <Transitions>
            <DoubleTransition Property="Width" Duration="0:0:0.25" Easing="CubicEaseOut"/>
            <DoubleTransition Property="Opacity" Duration="0:0:0.2"/>
        </Transitions>
    </Setter>
</Style>

<Style Selector="Button.sidebar-toggle">
    <Setter Property="RenderTransformOrigin" Value="0.5,0.5"/>
    <Setter Property="RenderTransform" Value="rotate(0deg)"/>
    <Setter Property="Transitions">
        <Transitions>
            <TransformOperationsTransition Property="RenderTransform" Duration="0:0:0.2"/>
        </Transitions>
    </Setter>
</Style>

<Style Selector="Button.sidebar-toggle.collapsed">
    <Setter Property="RenderTransform" Value="rotate(180deg)"/>
</Style>
```

### XAML

```xml
<Grid ColumnDefinitions="Auto,*">
    <!-- Sidebar -->
    <Border Grid.Column="0"
            Classes="sidebar-animated"
            Width="{Binding SidebarWidth}"
            Background="#1F1F1F">
        <StackPanel Margin="12">
            <!-- Sidebar content -->
            <TextBlock Text="Navigation" FontWeight="SemiBold" Margin="0,0,0,12"/>
            <ListBox ItemsSource="{Binding NavItems}"/>
        </StackPanel>
    </Border>

    <!-- Main content -->
    <Grid Grid.Column="1">
        <DockPanel>
            <!-- Toggle button in toolbar -->
            <Border DockPanel.Dock="Top" Background="#262626" Padding="8">
                <Button Classes="sidebar-toggle"
                        Classes.collapsed="{Binding !IsSidebarVisible}"
                        Command="{Binding ToggleSidebarCommand}"
                        Content="&#x25C0;"/>
            </Border>
            <ContentControl Content="{Binding CurrentView}"/>
        </DockPanel>
    </Grid>
</Grid>
```

---

## Navigation Tree

Hierarchical navigation with expandable sections.

### Model

```csharp
public record NavItem(
    string Name,
    string Icon,
    ObservableCollection<NavItem>? Children = null)
{
    public bool HasChildren => Children?.Count > 0;

    [ObservableProperty]
    private bool _isExpanded;
}
```

### ViewModel

```csharp
public partial class MainWindowViewModel : ObservableObject
{
    public ObservableCollection<NavItem> NavItems { get; } = new()
    {
        new("Favorites", "star", new()
        {
            new("Desktop", "folder"),
            new("Downloads", "folder"),
            new("Documents", "folder"),
        }),
        new("Storage", "drive", new()
        {
            new("C: Drive", "drive"),
            new("D: Drive", "drive"),
        }),
    };

    [ObservableProperty]
    private NavItem? _selectedNavItem;

    partial void OnSelectedNavItemChanged(NavItem? value)
    {
        if (value != null)
            NavigateTo(value);
    }
}
```

### Style

```xml
<Style Selector="TreeViewItem">
    <Setter Property="IsExpanded" Value="{Binding IsExpanded, Mode=TwoWay}"/>
</Style>

<Style Selector="TreeViewItem /template/ Border#PART_LayoutRoot">
    <Setter Property="CornerRadius" Value="4"/>
</Style>

<Style Selector="TreeViewItem:selected /template/ Border#PART_LayoutRoot">
    <Setter Property="Background" Value="#1E3A5F"/>
</Style>
```

### XAML

```xml
<TreeView ItemsSource="{Binding NavItems}"
          SelectedItem="{Binding SelectedNavItem}">
    <TreeView.ItemTemplate>
        <TreeDataTemplate ItemsSource="{Binding Children}">
            <StackPanel Orientation="Horizontal" Spacing="8">
                <TextBlock Text="{Binding Icon}" FontFamily="Segoe MDL2 Assets"/>
                <TextBlock Text="{Binding Name}"/>
            </StackPanel>
        </TreeDataTemplate>
    </TreeView.ItemTemplate>
</TreeView>
```

---

## DataGrid with Styling

Full-featured data grid with alternating rows, selection, and sorting.

### Setup (App.axaml)

```xml
<Application.Styles>
    <FluentTheme/>
    <StyleInclude Source="avares://Avalonia.Controls.DataGrid/Themes/Fluent.axaml"/>
</Application.Styles>
```

### Model

```csharp
public record FileItem(
    string Name,
    string Type,
    DateTime DateModified,
    int? Folders,
    int? Files,
    string? Size,
    bool IsFolder);
```

### ViewModel

```csharp
public partial class MainWindowViewModel : ObservableObject
{
    public ObservableCollection<FileItem> Files { get; } = new();

    [ObservableProperty]
    private FileItem? _selectedFile;

    [ObservableProperty]
    private int _selectedCount;

    public MainWindowViewModel()
    {
        // Sample data
        Files.Add(new("Documents", "Folder", DateTime.Now, 5, 23, null, true));
        Files.Add(new("report.pdf", "PDF", DateTime.Now, null, null, "2.3 MB", false));
    }
}
```

### Style

```xml
<!-- Alternating rows -->
<Style Selector="DataGridRow:nth-child(even)">
    <Setter Property="Background" Value="#1C1C1C"/>
</Style>

<Style Selector="DataGridRow:nth-child(odd)">
    <Setter Property="Background" Value="#181818"/>
</Style>

<!-- Hover -->
<Style Selector="DataGridRow:pointerover /template/ Rectangle#BackgroundRectangle">
    <Setter Property="Fill" Value="#262626"/>
</Style>

<!-- Selected -->
<Style Selector="DataGridRow:selected /template/ Rectangle#BackgroundRectangle">
    <Setter Property="Fill" Value="#1E3A5F"/>
</Style>

<!-- Header -->
<Style Selector="DataGridColumnHeader">
    <Setter Property="Background" Value="#2D2D30"/>
    <Setter Property="Foreground" Value="#A3A3A3"/>
    <Setter Property="FontSize" Value="12"/>
    <Setter Property="Padding" Value="12,8"/>
</Style>

<!-- Cells -->
<Style Selector="DataGridCell">
    <Setter Property="Padding" Value="12,8"/>
    <Setter Property="FontSize" Value="13"/>
</Style>
```

### XAML

```xml
<DataGrid ItemsSource="{Binding Files}"
          SelectedItem="{Binding SelectedFile}"
          AutoGenerateColumns="False"
          CanUserReorderColumns="True"
          CanUserResizeColumns="True"
          CanUserSortColumns="True"
          GridLinesVisibility="None"
          SelectionMode="Extended">
    <DataGrid.Columns>
        <DataGridTemplateColumn Header="Name" Width="2*" CanUserSort="True" SortMemberPath="Name">
            <DataGridTemplateColumn.CellTemplate>
                <DataTemplate>
                    <StackPanel Orientation="Horizontal" Spacing="8">
                        <TextBlock Text="{Binding IsFolder, Converter={StaticResource FolderIconConverter}}"/>
                        <TextBlock Text="{Binding Name}"/>
                    </StackPanel>
                </DataTemplate>
            </DataGridTemplateColumn.CellTemplate>
        </DataGridTemplateColumn>
        <DataGridTextColumn Header="Type" Binding="{Binding Type}" Width="1.5*"/>
        <DataGridTextColumn Header="Date Modified"
                           Binding="{Binding DateModified, StringFormat='{}{0:M/d/yyyy h:mm tt}'}"
                           Width="1.5*"/>
        <DataGridTextColumn Header="Folders" Binding="{Binding Folders}" Width="0.7*"/>
        <DataGridTextColumn Header="Files" Binding="{Binding Files}" Width="0.7*"/>
        <DataGridTextColumn Header="Size" Binding="{Binding Size}" Width="1*"/>
    </DataGrid.Columns>
</DataGrid>
```

### FolderIconConverter

```csharp
public class FolderIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool isFolder && isFolder ? "folder" : "file";

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
```

---

## Breadcrumb Address Bar

Clickable path navigation with hover effects.

### ViewModel

```csharp
public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _currentPath = @"C:\Users\Me\Downloads";

    public ObservableCollection<PathSegment> PathSegments { get; } = new();

    partial void OnCurrentPathChanged(string value)
    {
        PathSegments.Clear();
        var parts = value.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
        var fullPath = "";
        foreach (var part in parts)
        {
            fullPath = Path.Combine(fullPath, part);
            if (string.IsNullOrEmpty(fullPath)) fullPath = part + Path.DirectorySeparatorChar;
            PathSegments.Add(new PathSegment(part, fullPath));
        }
    }

    [RelayCommand]
    private void NavigateToPath(string path)
    {
        CurrentPath = path;
        // Load directory contents...
    }
}

public record PathSegment(string Name, string FullPath);
```

### Style

```xml
<Style Selector="Button.breadcrumb">
    <Setter Property="Background" Value="Transparent"/>
    <Setter Property="Padding" Value="6,4"/>
    <Setter Property="CornerRadius" Value="4"/>
    <Setter Property="Cursor" Value="Hand"/>
    <Setter Property="Transitions">
        <Transitions>
            <BrushTransition Property="Background" Duration="0:0:0.15"/>
        </Transitions>
    </Setter>
</Style>

<Style Selector="Button.breadcrumb:pointerover">
    <Setter Property="Background" Value="#2E2E2E"/>
</Style>

<Style Selector="TextBlock.separator">
    <Setter Property="Foreground" Value="#525252"/>
    <Setter Property="Margin" Value="2,0"/>
    <Setter Property="VerticalAlignment" Value="Center"/>
</Style>
```

### XAML

```xml
<Border Background="#3C3C3C" CornerRadius="4" Padding="4">
    <ItemsControl ItemsSource="{Binding PathSegments}">
        <ItemsControl.ItemsPanel>
            <ItemsPanelTemplate>
                <StackPanel Orientation="Horizontal"/>
            </ItemsPanelTemplate>
        </ItemsControl.ItemsPanel>
        <ItemsControl.ItemTemplate>
            <DataTemplate>
                <StackPanel Orientation="Horizontal">
                    <Button Classes="breadcrumb"
                            Command="{Binding $parent[ItemsControl].((vm:MainWindowViewModel)DataContext).NavigateToPathCommand}"
                            CommandParameter="{Binding FullPath}"
                            Content="{Binding Name}"/>
                    <TextBlock Classes="separator" Text="&#x25B8;"/>
                </StackPanel>
            </DataTemplate>
        </ItemsControl.ItemTemplate>
    </ItemsControl>
</Border>
```

---

## Tab Bar

Closeable tabs with new tab button.

### ViewModel

```csharp
public partial class TabViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "New Tab";

    [ObservableProperty]
    private string _path = "";

    [ObservableProperty]
    private bool _isActive;
}

public partial class MainWindowViewModel : ObservableObject
{
    public ObservableCollection<TabViewModel> Tabs { get; } = new();

    [ObservableProperty]
    private TabViewModel? _activeTab;

    public MainWindowViewModel()
    {
        var tab = new TabViewModel { Title = "Downloads", Path = @"C:\Users\Me\Downloads", IsActive = true };
        Tabs.Add(tab);
        ActiveTab = tab;
    }

    [RelayCommand]
    private void NewTab()
    {
        var tab = new TabViewModel { Title = "New Tab" };
        Tabs.Add(tab);
        SelectTab(tab);
    }

    [RelayCommand]
    private void CloseTab(TabViewModel tab)
    {
        var index = Tabs.IndexOf(tab);
        Tabs.Remove(tab);
        if (Tabs.Count > 0)
            SelectTab(Tabs[Math.Max(0, index - 1)]);
    }

    [RelayCommand]
    private void SelectTab(TabViewModel tab)
    {
        foreach (var t in Tabs) t.IsActive = false;
        tab.IsActive = true;
        ActiveTab = tab;
    }
}
```

### Style

```xml
<Style Selector="Button.tab">
    <Setter Property="Background" Value="Transparent"/>
    <Setter Property="Padding" Value="12,8"/>
    <Setter Property="CornerRadius" Value="4,4,0,0"/>
</Style>

<Style Selector="Button.tab.active">
    <Setter Property="Background" Value="#1E1E1E"/>
</Style>

<Style Selector="Button.tab:pointerover">
    <Setter Property="Background" Value="#2A2A2A"/>
</Style>

<Style Selector="Button.tab-close">
    <Setter Property="Width" Value="16"/>
    <Setter Property="Height" Value="16"/>
    <Setter Property="Padding" Value="0"/>
    <Setter Property="Background" Value="Transparent"/>
    <Setter Property="Opacity" Value="0.5"/>
</Style>

<Style Selector="Button.tab-close:pointerover">
    <Setter Property="Opacity" Value="1"/>
    <Setter Property="Background" Value="#3E3E42"/>
</Style>
```

### XAML

```xml
<Border Background="#2D2D30" Padding="4,4,4,0">
    <StackPanel Orientation="Horizontal">
        <ItemsControl ItemsSource="{Binding Tabs}">
            <ItemsControl.ItemsPanel>
                <ItemsPanelTemplate>
                    <StackPanel Orientation="Horizontal" Spacing="2"/>
                </ItemsPanelTemplate>
            </ItemsControl.ItemsPanel>
            <ItemsControl.ItemTemplate>
                <DataTemplate>
                    <Button Classes="tab" Classes.active="{Binding IsActive}"
                            Command="{Binding $parent[ItemsControl].((vm:MainWindowViewModel)DataContext).SelectTabCommand}"
                            CommandParameter="{Binding}">
                        <StackPanel Orientation="Horizontal" Spacing="8">
                            <TextBlock Text="{Binding Title}"/>
                            <Button Classes="tab-close"
                                    Command="{Binding $parent[ItemsControl].((vm:MainWindowViewModel)DataContext).CloseTabCommand}"
                                    CommandParameter="{Binding}"
                                    Content="x"/>
                        </StackPanel>
                    </Button>
                </DataTemplate>
            </ItemsControl.ItemTemplate>
        </ItemsControl>
        <Button Command="{Binding NewTabCommand}" Content="+" Padding="8,4"/>
    </StackPanel>
</Border>
```

---

## Status Bar

Information bar with selection count and view options.

### Style

```xml
<Style Selector="Border.statusbar">
    <Setter Property="Background">
        <LinearGradientBrush StartPoint="0%,0%" EndPoint="100%,0%">
            <GradientStop Color="#2563EB" Offset="0"/>
            <GradientStop Color="#3B82F6" Offset="0.5"/>
            <GradientStop Color="#1D4ED8" Offset="1"/>
        </LinearGradientBrush>
    </Setter>
    <Setter Property="Padding" Value="12,4"/>
</Style>

<Style Selector="TextBlock.statusbar-text">
    <Setter Property="FontSize" Value="12"/>
    <Setter Property="Foreground" Value="White"/>
    <Setter Property="VerticalAlignment" Value="Center"/>
</Style>
```

### XAML

```xml
<Border Classes="statusbar">
    <Grid ColumnDefinitions="*,Auto">
        <StackPanel Grid.Column="0" Orientation="Horizontal" Spacing="16">
            <TextBlock Classes="statusbar-text"
                       Text="{Binding SelectedCount, StringFormat='{}{0} selected'}"/>
            <TextBlock Classes="statusbar-text"
                       Text="{Binding TotalFiles, StringFormat='{}{0} items'}"/>
        </StackPanel>
        <StackPanel Grid.Column="1" Orientation="Horizontal" Spacing="8">
            <Button Content="List" Classes="view-btn"/>
            <Button Content="Grid" Classes="view-btn"/>
        </StackPanel>
    </Grid>
</Border>
```

---

## Command Palette

Quick command search overlay.

### ViewModel

```csharp
public partial class CommandPaletteViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isOpen;

    [ObservableProperty]
    private string _searchText = string.Empty;

    public ObservableCollection<CommandItem> Commands { get; } = new();
    public ObservableCollection<CommandItem> FilteredCommands { get; } = new();

    partial void OnSearchTextChanged(string value)
    {
        FilteredCommands.Clear();
        var filtered = string.IsNullOrWhiteSpace(value)
            ? Commands
            : Commands.Where(c => c.Name.Contains(value, StringComparison.OrdinalIgnoreCase));
        foreach (var cmd in filtered.Take(10))
            FilteredCommands.Add(cmd);
    }

    [RelayCommand]
    private void Open()
    {
        IsOpen = true;
        SearchText = string.Empty;
        OnSearchTextChanged(string.Empty);
    }

    [RelayCommand]
    private void Close() => IsOpen = false;

    [RelayCommand]
    private void Execute(CommandItem command)
    {
        command.Action?.Invoke();
        Close();
    }
}

public record CommandItem(string Name, string Shortcut, Action? Action);
```

### Style

```xml
<Style Selector="Border.command-palette">
    <Setter Property="Background" Value="#2D2D30"/>
    <Setter Property="BorderBrush" Value="#3E3E42"/>
    <Setter Property="BorderThickness" Value="1"/>
    <Setter Property="CornerRadius" Value="8"/>
    <Setter Property="Width" Value="500"/>
    <Setter Property="BoxShadow" Value="0 8 32 0 #00000080"/>
</Style>

<Style Selector="ListBoxItem.command-item:pointerover">
    <Setter Property="Background" Value="#094771"/>
</Style>

<Style Selector="ListBoxItem.command-item:selected">
    <Setter Property="Background" Value="#094771"/>
</Style>
```

### XAML

```xml
<!-- Overlay (add to main window) -->
<Panel>
    <!-- Main content here -->

    <!-- Command palette overlay -->
    <Border IsVisible="{Binding CommandPalette.IsOpen}"
            Background="#80000000">
        <Border Classes="command-palette"
                VerticalAlignment="Top"
                Margin="0,100,0,0">
            <DockPanel>
                <TextBox DockPanel.Dock="Top"
                         Text="{Binding CommandPalette.SearchText}"
                         Watermark="Type a command..."
                         Margin="12"/>
                <ListBox ItemsSource="{Binding CommandPalette.FilteredCommands}"
                         MaxHeight="300">
                    <ListBox.ItemTemplate>
                        <DataTemplate>
                            <Grid ColumnDefinitions="*,Auto">
                                <TextBlock Grid.Column="0" Text="{Binding Name}"/>
                                <TextBlock Grid.Column="1" Text="{Binding Shortcut}"
                                           Foreground="#737373" FontSize="12"/>
                            </Grid>
                        </DataTemplate>
                    </ListBox.ItemTemplate>
                </ListBox>
            </DockPanel>
        </Border>
    </Border>
</Panel>
```

### Keyboard Shortcut (code-behind)

```csharp
protected override void OnKeyDown(KeyEventArgs e)
{
    if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.K)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.CommandPalette.OpenCommand.Execute(null);
        e.Handled = true;
    }
    base.OnKeyDown(e);
}
```
