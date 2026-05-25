# Avalonia Design - Animations, Styling & Colors

## Transitions

Avalonia transitions animate property changes smoothly. CSS-inspired.

### Transition Types

| Type | Properties |
|------|------------|
| `DoubleTransition` | Width, Height, Opacity, numeric |
| `ThicknessTransition` | Margin, Padding, BorderThickness |
| `BrushTransition` | Background, Foreground, BorderBrush |
| `ColorTransition` | Direct Color properties |
| `TransformOperationsTransition` | RenderTransform |
| `BoxShadowsTransition` | BoxShadow |
| `CornerRadiusTransition` | CornerRadius |
| `PointTransition` | Point properties |
| `SizeTransition` | Size properties |

### Basic Syntax

```xml
<Border>
    <Border.Transitions>
        <Transitions>
            <DoubleTransition Property="Opacity" Duration="0:0:0.2"/>
            <BrushTransition Property="Background" Duration="0:0:0.15"/>
        </Transitions>
    </Border.Transitions>
</Border>
```

### Transitions in Styles (Recommended)

```xml
<Style Selector="Border.animated">
    <Setter Property="Transitions">
        <Transitions>
            <DoubleTransition Property="Width" Duration="0:0:0.25" Easing="CubicEaseOut"/>
            <BrushTransition Property="Background" Duration="0:0:0.15"/>
            <TransformOperationsTransition Property="RenderTransform" Duration="0:0:0.2"/>
        </Transitions>
    </Setter>
</Style>
```

---

## CSS-Like Transforms

```xml
<!-- Translate -->
<Setter Property="RenderTransform" Value="translateX(4px)"/>
<Setter Property="RenderTransform" Value="translateY(-2px)"/>

<!-- Scale -->
<Setter Property="RenderTransform" Value="scale(1.02)"/>
<Setter Property="RenderTransform" Value="scale(0.96)"/>

<!-- Rotate -->
<Setter Property="RenderTransform" Value="rotate(180deg)"/>

<!-- Combined -->
<Setter Property="RenderTransform" Value="translateX(4px) scale(1.02)"/>
```

**Important**: Set origin for rotation/scale:
```xml
<Setter Property="RenderTransformOrigin" Value="0.5,0.5"/> <!-- Center -->
<Setter Property="RenderTransformOrigin" Value="0,0.5"/>   <!-- Left center -->
```

---

## Easing Functions

- `Linear`
- `CubicEaseIn`, `CubicEaseOut`, `CubicEaseInOut`
- `QuadraticEaseIn`, `QuadraticEaseOut`, `QuadraticEaseInOut`
- `BackEaseIn`, `BackEaseOut`, `BackEaseInOut`
- `BounceEaseIn`, `BounceEaseOut`, `BounceEaseInOut`
- `ElasticEaseIn`, `ElasticEaseOut`, `ElasticEaseInOut`

```xml
<DoubleTransition Property="Width" Duration="0:0:0.25" Easing="CubicEaseOut"/>
```

---

## Gradients

### LinearGradientBrush

```xml
<!-- Vertical (top to bottom) -->
<LinearGradientBrush x:Key="HeaderGradient" StartPoint="0%,0%" EndPoint="0%,100%">
    <GradientStop Color="#242424" Offset="0.0"/>
    <GradientStop Color="#1A1A1A" Offset="1.0"/>
</LinearGradientBrush>

<!-- Horizontal (left to right) -->
<LinearGradientBrush x:Key="SidebarGradient" StartPoint="0%,0%" EndPoint="100%,0%">
    <GradientStop Color="#1F1F1F" Offset="0.0"/>
    <GradientStop Color="#1A1A1A" Offset="1.0"/>
</LinearGradientBrush>

<!-- Diagonal accent -->
<LinearGradientBrush x:Key="AccentGradient" StartPoint="0%,0%" EndPoint="100%,100%">
    <GradientStop Color="#3B82F6" Offset="0.0"/>
    <GradientStop Color="#8B5CF6" Offset="1.0"/>
</LinearGradientBrush>
```

---

## Interactive Button Pattern

```xml
<Style Selector="Button.interactive">
    <Setter Property="Background" Value="Transparent"/>
    <Setter Property="Cursor" Value="Hand"/>
    <Setter Property="RenderTransform" Value="scale(1)"/>
    <Setter Property="Transitions">
        <Transitions>
            <BrushTransition Property="Background" Duration="0:0:0.15"/>
            <TransformOperationsTransition Property="RenderTransform" Duration="0:0:0.1"/>
        </Transitions>
    </Setter>
</Style>

<Style Selector="Button.interactive:pointerover">
    <Setter Property="Background" Value="#2E2E2E"/>
</Style>

<Style Selector="Button.interactive:pressed">
    <Setter Property="RenderTransform" Value="scale(0.96)"/>
</Style>
```

---

## Hover Slide Effect

```xml
<Style Selector="Border.slide-hover">
    <Setter Property="RenderTransform" Value="translateX(0)"/>
    <Setter Property="Transitions">
        <Transitions>
            <TransformOperationsTransition Property="RenderTransform" Duration="0:0:0.15"/>
        </Transitions>
    </Setter>
</Style>

<Style Selector="Border.slide-hover:pointerover">
    <Setter Property="RenderTransform" Value="translateX(4px)"/>
</Style>
```

---

## Lift Card Effect

```xml
<Style Selector="Border.lift-card">
    <Setter Property="RenderTransform" Value="scale(1)"/>
    <Setter Property="BoxShadow" Value="0 2 8 0 #00000020"/>
    <Setter Property="Transitions">
        <Transitions>
            <TransformOperationsTransition Property="RenderTransform" Duration="0:0:0.2"/>
            <BoxShadowsTransition Property="BoxShadow" Duration="0:0:0.2"/>
        </Transitions>
    </Setter>
</Style>

<Style Selector="Border.lift-card:pointerover">
    <Setter Property="RenderTransform" Value="scale(1.02)"/>
    <Setter Property="BoxShadow" Value="0 4 16 0 #00000040"/>
</Style>
```

---

## Focus Animation Pattern

```xml
<Style Selector="Border.focusable">
    <Setter Property="BorderBrush" Value="Transparent"/>
    <Setter Property="BorderThickness" Value="1"/>
    <Setter Property="Transitions">
        <Transitions>
            <BrushTransition Property="BorderBrush" Duration="0:0:0.2"/>
        </Transitions>
    </Setter>
</Style>

<Style Selector="Border.focusable:focus-within">
    <Setter Property="BorderBrush" Value="{StaticResource AccentBrush}"/>
</Style>
```

---

## Conditional Classes

```xml
<!-- Toggle class based on binding -->
<Button Classes="btn" Classes.active="{Binding IsActive}"/>

<Style Selector="Button.btn.active">
    <Setter Property="Background" Value="{StaticResource AccentBrush}"/>
</Style>
```

---

## Dark Theme Color System

```xml
<!-- Background hierarchy (darkest to lightest) -->
<Color x:Key="WindowBackgroundColor">#181818</Color>
<Color x:Key="SidebarBackgroundColor">#1F1F1F</Color>
<Color x:Key="ToolbarBackgroundColor">#262626</Color>
<Color x:Key="InputBackgroundColor">#2E2E2E</Color>
<Color x:Key="SurfaceElevatedColor">#2A2A2A</Color>

<!-- Text hierarchy -->
<Color x:Key="TextPrimaryColor">#F5F5F5</Color>
<Color x:Key="TextSecondaryColor">#A3A3A3</Color>
<Color x:Key="TextMutedColor">#737373</Color>
<Color x:Key="TextDimmedColor">#525252</Color>

<!-- Accent -->
<Color x:Key="AccentColor">#3B82F6</Color>
<Color x:Key="AccentHoverColor">#60A5FA</Color>
<Color x:Key="AccentMutedColor">#1E3A5F</Color>

<!-- Borders -->
<Color x:Key="BorderColor">#333333</Color>
<Color x:Key="BorderSubtleColor">#2A2A2A</Color>

<!-- Semantic -->
<Color x:Key="SuccessColor">#22C55E</Color>
<Color x:Key="WarningColor">#F59E0B</Color>
<Color x:Key="ErrorColor">#EF4444</Color>

<!-- States -->
<Color x:Key="RowHoverColor">#262626</Color>
<Color x:Key="RowSelectedColor">#1E3A5F</Color>
<Color x:Key="RowAlternateColor">#1C1C1C</Color>
```

### Creating Brushes

```xml
<SolidColorBrush x:Key="WindowBackgroundBrush" Color="{StaticResource WindowBackgroundColor}"/>
<SolidColorBrush x:Key="AccentBrush" Color="{StaticResource AccentColor}"/>
```

---

## Spacing & Radius Resources

```xml
<Thickness x:Key="SpacingXs">4</Thickness>
<Thickness x:Key="SpacingSm">8</Thickness>
<Thickness x:Key="SpacingMd">12</Thickness>
<Thickness x:Key="SpacingLg">16</Thickness>
<Thickness x:Key="SpacingXl">24</Thickness>

<CornerRadius x:Key="RadiusSm">4</CornerRadius>
<CornerRadius x:Key="RadiusMd">6</CornerRadius>
<CornerRadius x:Key="RadiusLg">8</CornerRadius>
```

---

## Common Converters

### Bool to Double (Opacity)

```csharp
public class BoolToDoubleConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && b ? 1.0 : 0.0;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
```

**Registration**:
```xml
<Application.Resources>
    <conv:BoolToDoubleConverter x:Key="BoolToDoubleConverter"/>
</Application.Resources>
```

**Usage**:
```xml
<Border Opacity="{Binding IsVisible, Converter={StaticResource BoolToDoubleConverter}}"/>
```

---

## Common Pitfalls

### 1. Duration as Resource
```xml
<!-- WRONG -->
<Duration x:Key="Fast">0:0:0.1</Duration>

<!-- CORRECT - inline -->
<DoubleTransition Property="Opacity" Duration="0:0:0.1"/>
```

### 2. TextTransform Property
```xml
<!-- WRONG - doesn't exist -->
<Setter Property="TextTransform" Value="Uppercase"/>

<!-- WORKAROUND -->
<Setter Property="LetterSpacing" Value="1"/>
```

### 3. WPF-Style Transforms
```xml
<!-- WRONG - cannot transition -->
<RotateTransform Angle="45"/>

<!-- CORRECT -->
<Setter Property="RenderTransform" Value="rotate(45deg)"/>
```

### 4. PlacementMode Deprecated
```xml
<!-- DEPRECATED -->
<Popup PlacementMode="Center"/>

<!-- CORRECT -->
<Popup Placement="Center"/>
```

### 5. BoolConverters.ToDouble
```xml
<!-- WRONG - doesn't exist -->
{x:Static BoolConverters.ToDouble}

<!-- CORRECT - custom converter -->
{StaticResource BoolToDoubleConverter}
```
