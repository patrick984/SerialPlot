# Avalonia Core - XAML Syntax & Bindings

## AXAML File Structure

### Window/UserControl Header

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="using:MyApp.ViewModels"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        mc:Ignorable="d" d:DesignWidth="800" d:DesignHeight="450"
        x:Class="MyApp.Views.MainWindow"
        x:DataType="vm:MainViewModel"
        Title="My Application">
```

### App.axaml Structure

```xml
<Application xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:local="using:MyApp"
             x:Class="MyApp.App"
             RequestedThemeVariant="Default">
    <Application.DataTemplates>
        <local:ViewLocator/>
    </Application.DataTemplates>
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceInclude Source="avares://MyApp/Styles/App.axaml"/>
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
    <Application.Styles>
        <FluentTheme/>
    </Application.Styles>
</Application>
```

---

## Style Selectors (CSS-like)

### Basic Selectors

| Selector | Description |
|----------|-------------|
| `Button` | All Button controls |
| `Button.primary` | Buttons with class "primary" |
| `Button.primary.large` | Buttons with BOTH classes |
| `Button:pointerover` | Button on hover |
| `Button:pressed` | Button when pressed |
| `Button:disabled` | Disabled buttons |
| `Button:focus` | Focused buttons |
| `Button#myButton` | Button with Name="myButton" |
| `:is(Button)` | Button and derived types |

### Hierarchy Selectors

| Selector | Description |
|----------|-------------|
| `StackPanel > Button` | Direct child buttons |
| `StackPanel Button` | Any descendant button |
| `Button /template/ ContentPresenter` | Inside Button's template |

### Property Match

```xml
<Style Selector="Button[IsDefault=true]">
<Style Selector="TextBlock[(Grid.Row)=0]">
```

### Nth-Child

```xml
<Style Selector="ListBoxItem:nth-child(odd)">
<Style Selector="ListBoxItem:nth-child(2n+1)">
<Style Selector="ListBoxItem:nth-last-child(1)">
```

### Not Selector

```xml
<Style Selector="TextBlock:not(.h1)">
```

### Multiple Selectors (OR)

```xml
<Style Selector="TextBlock, Button">
```

---

## Writing Styles

### Basic Style

```xml
<Window.Styles>
    <Style Selector="TextBlock.h1">
        <Setter Property="FontSize" Value="24"/>
        <Setter Property="FontWeight" Value="Bold"/>
    </Style>
</Window.Styles>
```

### Nested Styles (use `^` for parent)

```xml
<Style Selector="Button.primary">
    <Setter Property="Background" Value="Blue"/>
    <Setter Property="Foreground" Value="White"/>

    <Style Selector="^:pointerover">
        <Setter Property="Background" Value="DarkBlue"/>
    </Style>

    <Style Selector="^:pressed">
        <Setter Property="Background" Value="Navy"/>
    </Style>
</Style>
```

### Complex Values in Setters

```xml
<Setter Property="Background">
    <LinearGradientBrush StartPoint="0%,0%" EndPoint="100%,100%">
        <GradientStop Color="Blue" Offset="0"/>
        <GradientStop Color="Purple" Offset="1"/>
    </LinearGradientBrush>
</Setter>
```

---

## Pseudo-Classes Reference

| Pseudo-class | Description |
|--------------|-------------|
| `:pointerover` | Mouse over control |
| `:pressed` | Being pressed |
| `:focus` | Has keyboard focus |
| `:focus-within` | Control or child has focus |
| `:focus-visible` | Focus with visible indicator |
| `:disabled` | IsEnabled=false |
| `:checked` | CheckBox/RadioButton checked |
| `:unchecked` | Not checked |
| `:indeterminate` | CheckBox indeterminate |
| `:selected` | ListBoxItem selected |
| `:vertical` | Vertical orientation |
| `:horizontal` | Horizontal orientation |
| `:empty` | TextBox has no text |
| `:open` | ComboBox/Popup open |

---

## ControlThemes (for templated controls)

```xml
<Application.Resources>
    <ControlTheme x:Key="RoundButton" TargetType="Button">
        <Setter Property="Background" Value="Blue"/>
        <Setter Property="Template">
            <ControlTemplate>
                <Border Background="{TemplateBinding Background}"
                        CornerRadius="20"
                        Padding="{TemplateBinding Padding}">
                    <ContentPresenter Content="{TemplateBinding Content}"
                                      HorizontalContentAlignment="Center"/>
                </Border>
            </ControlTemplate>
        </Setter>

        <Style Selector="^:pointerover">
            <Setter Property="Background" Value="DarkBlue"/>
        </Style>
    </ControlTheme>
</Application.Resources>

<!-- Usage -->
<Button Theme="{StaticResource RoundButton}" Content="Click"/>
```

---

## Data Binding

### Basic Binding

```xml
<TextBox Text="{Binding UserName}"/>
<TextBlock Text="{Binding Status, Mode=OneWay}"/>
```

### Compiled Bindings (Recommended)

Enable with `x:DataType` on root element:

```xml
<UserControl x:DataType="vm:MyViewModel">
    <TextBlock Text="{Binding Name}"/>  <!-- Compile-time checked -->
</UserControl>
```

### Binding Modes

- `OneWay` - Source to target only
- `TwoWay` - Bidirectional (default for inputs)
- `OneTime` - Initial value only
- `OneWayToSource` - Target to source only

### Converters

```xml
<TextBlock IsVisible="{Binding HasItems, Converter={x:Static BoolConverters.Not}}"/>
<TextBlock Text="{Binding Value, StringFormat='Value: {0:F2}'}"/>
```

### FallbackValue

```xml
<Image Source="{Binding ImagePath, FallbackValue={StaticResource DefaultImage}}"/>
```

### Command Binding

```xml
<Button Command="{Binding SaveCommand}" CommandParameter="{Binding SelectedItem}"/>
```

### Binding to Parent DataContext (from template)

```xml
<Button Command="{Binding $parent[ListBox].((vm:MainViewModel)DataContext).DeleteCommand}"
        CommandParameter="{Binding}"/>
```

---

## Data Templates

### Inline DataTemplate

```xml
<ListBox ItemsSource="{Binding Items}">
    <ListBox.ItemTemplate>
        <DataTemplate>
            <StackPanel Orientation="Horizontal">
                <TextBlock Text="{Binding Name}"/>
                <TextBlock Text="{Binding Description}" Margin="10,0,0,0"/>
            </StackPanel>
        </DataTemplate>
    </ListBox.ItemTemplate>
</ListBox>
```

### DataTemplate with DataType

```xml
<Application.DataTemplates>
    <DataTemplate DataType="{x:Type vm:PersonViewModel}">
        <Border Background="LightBlue" Padding="10">
            <TextBlock Text="{Binding FullName}"/>
        </Border>
    </DataTemplate>
</Application.DataTemplates>
```

---

## Resources

### Defining Resources

```xml
<Window.Resources>
    <SolidColorBrush x:Key="PrimaryBrush" Color="#0078D4"/>
    <sys:Double x:Key="HeaderFontSize">24</sys:Double>
</Window.Resources>
```

### Using Resources

```xml
<Button Background="{StaticResource PrimaryBrush}"/>
<TextBlock FontSize="{DynamicResource HeaderFontSize}"/>
```

---

## Layout Panels

```xml
<!-- StackPanel -->
<StackPanel Orientation="Vertical" Spacing="10">
    <TextBlock Text="Item 1"/>
    <TextBlock Text="Item 2"/>
</StackPanel>

<!-- Grid -->
<Grid RowDefinitions="Auto,*,Auto" ColumnDefinitions="*,2*">
    <TextBlock Grid.Row="0" Grid.Column="0"/>
    <ContentControl Grid.Row="1" Grid.ColumnSpan="2"/>
</Grid>

<!-- DockPanel -->
<DockPanel>
    <Menu DockPanel.Dock="Top"/>
    <StatusBar DockPanel.Dock="Bottom"/>
    <ContentControl/>  <!-- Fills remaining -->
</DockPanel>

<!-- WrapPanel -->
<WrapPanel>
    <Button Content="1"/>
    <Button Content="2"/>
</WrapPanel>

<!-- Canvas -->
<Canvas>
    <Button Canvas.Left="100" Canvas.Top="50" Content="Positioned"/>
</Canvas>
```

---

## Window Properties

```xml
<Window TransparencyLevelHint="AcrylicBlur"
        Background="Transparent"
        ExtendClientAreaToDecorationsHint="True"
        WindowStartupLocation="CenterScreen"
        SystemDecorations="None"
        CanResize="True"
        MinWidth="400"
        MinHeight="300">
```

---

## Asset References

```xml
<!-- From current assembly -->
<Image Source="/Assets/logo.png"/>
<Image Source="avares://MyApp/Assets/logo.png"/>

<!-- From another assembly -->
<Image Source="avares://MyApp.Resources/Images/icon.png"/>

<!-- Resource dictionary -->
<ResourceInclude Source="avares://MyApp/Styles/Buttons.axaml"/>
```

---

## Common Gotchas

1. **Style classes order doesn't matter** (unlike CSS specificity)
2. **Use `:is(Control)` to match derived types**
3. **Setters create shared instances** - use `<Template>` for unique instances
4. **Compiled bindings require `x:DataType`** on root element
5. **Use `^` in nested selectors** to reference parent
6. **ControlThemes vs Styles**: ControlTheme for templates, Styles for properties
7. **No Triggers**: Use pseudo-classes instead
8. **File extension is `.axaml`** not `.xaml`
