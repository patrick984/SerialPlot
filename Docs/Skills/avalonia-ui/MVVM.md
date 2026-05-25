# Avalonia MVVM with CommunityToolkit.Mvvm

## Setup

### NuGet Packages

```xml
<PackageReference Include="Avalonia" Version="11.*"/>
<PackageReference Include="Avalonia.Desktop" Version="11.*"/>
<PackageReference Include="CommunityToolkit.Mvvm" Version="8.*"/>
```

### ViewLocator.cs

```csharp
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MyApp;

public class ViewLocator : IDataTemplate
{
    public Control? Build(object? data)
    {
        if (data is null) return null;
        var name = data.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
        var type = Type.GetType(name);

        if (type != null)
        {
            var control = (Control)Activator.CreateInstance(type)!;
            control.DataContext = data;
            return control;
        }
        return new TextBlock { Text = "Not Found: " + name };
    }

    public bool Match(object? data) => data is ObservableObject;
}
```

---

## Observable Properties

Use `[ObservableProperty]` on private fields. Source generator creates public property.

```csharp
public partial class PersonViewModel : ObservableObject
{
    [ObservableProperty]
    private string _firstName = string.Empty;

    [ObservableProperty]
    private string _lastName = string.Empty;

    [ObservableProperty]
    private int _age;

    [ObservableProperty]
    private bool _isActive;
}
```

**Generated** (you don't write this):
```csharp
public string FirstName
{
    get => _firstName;
    set => SetProperty(ref _firstName, value);
}
```

**XAML**:
```xml
<TextBox Text="{Binding FirstName}"/>
```

---

## Property Change Callbacks

```csharp
public partial class PersonViewModel : ObservableObject
{
    [ObservableProperty]
    private string _firstName = string.Empty;

    // Called BEFORE change
    partial void OnFirstNameChanging(string value)
    {
        Console.WriteLine($"Changing to: {value}");
    }

    // Called AFTER change
    partial void OnFirstNameChanged(string value)
    {
        OnPropertyChanged(nameof(FullName));
    }

    public string FullName => $"{FirstName} {LastName}";
}
```

---

## Dependent Properties

```csharp
public partial class OrderViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Total))]
    [NotifyPropertyChangedFor(nameof(CanCheckout))]
    private decimal _subtotal;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Total))]
    private decimal _tax;

    // Computed properties
    public decimal Total => Subtotal + Tax;
    public bool CanCheckout => Subtotal > 0;
}
```

---

## RelayCommand - Basic

```csharp
public partial class CounterViewModel : ObservableObject
{
    [ObservableProperty]
    private int _count;

    [RelayCommand]
    private void Increment() => Count++;

    [RelayCommand]
    private void Decrement() => Count--;

    [RelayCommand]
    private void Reset() => Count = 0;
}
```

**XAML**:
```xml
<Button Command="{Binding IncrementCommand}" Content="+"/>
<Button Command="{Binding DecrementCommand}" Content="-"/>
<Button Command="{Binding ResetCommand}" Content="Reset"/>
```

---

## Async Commands

```csharp
public partial class DataViewModel : ObservableObject
{
    [ObservableProperty]
    private string _data = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            Data = await _dataService.FetchDataAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken token)
    {
        // CancellationToken automatically provided
        await _dataService.SaveAsync(Data, token);
    }
}
```

---

## Commands with CanExecute

```csharp
public partial class LoginViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    private string _username = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    private string _password = string.Empty;

    [RelayCommand(CanExecute = nameof(CanLogin))]
    private async Task LoginAsync()
    {
        await _authService.LoginAsync(Username, Password);
    }

    private bool CanLogin()
    {
        return !string.IsNullOrWhiteSpace(Username)
            && !string.IsNullOrWhiteSpace(Password);
    }
}
```

---

## Commands with Parameters

```csharp
public partial class ListViewModel : ObservableObject
{
    public ObservableCollection<ItemViewModel> Items { get; } = new();

    [RelayCommand]
    private void SelectItem(ItemViewModel item)
    {
        SelectedItem = item;
    }

    [RelayCommand]
    private void DeleteItem(ItemViewModel item)
    {
        Items.Remove(item);
    }

    [ObservableProperty]
    private ItemViewModel? _selectedItem;
}
```

**XAML** (binding from template to parent):
```xml
<ListBox ItemsSource="{Binding Items}">
    <ListBox.ItemTemplate>
        <DataTemplate>
            <StackPanel Orientation="Horizontal">
                <TextBlock Text="{Binding Name}"/>
                <Button Command="{Binding $parent[ListBox].((vm:ListViewModel)DataContext).DeleteItemCommand}"
                        CommandParameter="{Binding}"
                        Content="Delete"/>
            </StackPanel>
        </DataTemplate>
    </ListBox.ItemTemplate>
</ListBox>
```

---

## ObservableCollection

```csharp
public partial class TodoViewModel : ObservableObject
{
    public ObservableCollection<TodoItem> Todos { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddTodoCommand))]
    private string _newTodoText = string.Empty;

    [RelayCommand(CanExecute = nameof(CanAddTodo))]
    private void AddTodo()
    {
        Todos.Add(new TodoItem { Title = NewTodoText });
        NewTodoText = string.Empty;
    }

    private bool CanAddTodo() => !string.IsNullOrWhiteSpace(NewTodoText);

    [RelayCommand]
    private void RemoveTodo(TodoItem item) => Todos.Remove(item);
}

public partial class TodoItem : ObservableObject
{
    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private bool _isCompleted;
}
```

---

## Validation with ObservableValidator

```csharp
using System.ComponentModel.DataAnnotations;

public partial class RegistrationViewModel : ObservableValidator
{
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    private string _email = string.Empty;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Password is required")]
    [MinLength(8, ErrorMessage = "Minimum 8 characters")]
    private string _password = string.Empty;

    [RelayCommand(CanExecute = nameof(CanRegister))]
    private async Task RegisterAsync()
    {
        ValidateAllProperties();
        if (HasErrors) return;
        await _authService.RegisterAsync(Email, Password);
    }

    private bool CanRegister() => !HasErrors;
}
```

**XAML**:
```xml
<TextBox Text="{Binding Email}"/>
<TextBlock Text="{Binding (DataValidationErrors.Errors)[0]}"
           Foreground="Red" FontSize="12"/>
```

---

## Messaging (ViewModel Communication)

### Define Message

```csharp
using CommunityToolkit.Mvvm.Messaging.Messages;

public class UserLoggedInMessage : ValueChangedMessage<User>
{
    public UserLoggedInMessage(User user) : base(user) { }
}
```

### Send Message

```csharp
using CommunityToolkit.Mvvm.Messaging;

[RelayCommand]
private async Task LoginAsync()
{
    var user = await _authService.LoginAsync(Username, Password);
    WeakReferenceMessenger.Default.Send(new UserLoggedInMessage(user));
}
```

### Receive Message

```csharp
public partial class MainViewModel : ObservableObject, IRecipient<UserLoggedInMessage>
{
    public MainViewModel()
    {
        WeakReferenceMessenger.Default.Register(this);
    }

    public void Receive(UserLoggedInMessage message)
    {
        CurrentUser = message.Value;
        IsLoggedIn = true;
    }

    [ObservableProperty]
    private User? _currentUser;
}
```

---

## Navigation Pattern

```csharp
public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableObject? _currentView;

    [RelayCommand]
    private void NavigateToSettings()
    {
        CurrentView = new SettingsViewModel();
    }

    [RelayCommand]
    private void NavigateToHome()
    {
        CurrentView = new HomeViewModel();
    }
}
```

**XAML**:
```xml
<Window>
    <DockPanel>
        <StackPanel DockPanel.Dock="Top" Orientation="Horizontal">
            <Button Command="{Binding NavigateToHomeCommand}" Content="Home"/>
            <Button Command="{Binding NavigateToSettingsCommand}" Content="Settings"/>
        </StackPanel>
        <ContentControl Content="{Binding CurrentView}"/>
    </DockPanel>
</Window>
```

---

## Loading State Pattern

```csharp
public partial class DataViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotLoading))]
    private bool _isLoading;

    public bool IsNotLoading => !IsLoading;

    [ObservableProperty]
    private string? _errorMessage;

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            Data = await _service.LoadAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
```

---

## Selection Pattern

```csharp
public partial class MasterDetailViewModel : ObservableObject
{
    public ObservableCollection<ItemViewModel> Items { get; } = new();

    [ObservableProperty]
    private ItemViewModel? _selectedItem;

    partial void OnSelectedItemChanged(ItemViewModel? value)
    {
        if (value != null)
            _ = LoadDetailsAsync(value.Id);
    }

    [ObservableProperty]
    private ItemDetailViewModel? _details;

    private async Task LoadDetailsAsync(int id)
    {
        Details = await _service.GetDetailsAsync(id);
    }
}
```

---

## Required Usings

```csharp
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
```

---

## Key Points

1. **Always use `partial` class** - Source generators require it
2. **Private fields with underscore** - `_myField` generates `MyField`
3. **`[NotifyCanExecuteChangedFor]`** - Updates command CanExecute
4. **`[NotifyPropertyChangedFor]`** - Notifies dependent properties
5. **Async commands get CancellationToken** - Add as parameter
6. **Validation needs `ObservableValidator`** - Not `ObservableObject`
7. **Command naming** - `DoSomething()` becomes `DoSomethingCommand`
