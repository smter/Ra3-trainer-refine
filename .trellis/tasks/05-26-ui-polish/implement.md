# Implementation Plan: UI Polish

## Execution Order

### Step 1: ViewModel Changes

**1a. MainViewModel — ConnectionState enum + property**

File: `src/Ra3Trainer.App/ViewModels/MainViewModel.cs`

```csharp
public enum ConnectionState { Disconnected, Connected, Processing }
```

Add property:
```csharp
private ConnectionState _connectionState = ConnectionState.Disconnected;
public ConnectionState ConnectionState
{
    get => _connectionState;
    private set { _connectionState = value; OnPropertyChanged(); }
}
```

Set `ConnectionState` in: `RefreshProcess()`, `LaunchAndLoadAsync()`, `InstallPatches()`, `RestorePatches()`.

**1b. FeatureItemViewModel — IsActive**

```csharp
public bool IsActive => _enabled;
```

Add `OnPropertyChanged(nameof(IsActive))` call after line 71 (`_enabled = nextEnabled;`).

**Validation:** `dotnet build` + `dotnet test` pass.

### Step 2: Converter

Create `src/Ra3Trainer.App/Converters/ConnectionStateToBrushConverter.cs`:

```csharp
using System.Globalization;
using System.Windows.Data;

namespace Ra3Trainer.App.Converters;

public class ConnectionStateToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ConnectionState state)
        {
            return state switch
            {
                ConnectionState.Connected => Application.Current.TryFindResource("SuccessBrush"),
                ConnectionState.Processing => Application.Current.TryFindResource("WarningBrush"),
                _ => Application.Current.TryFindResource("ErrorBrush")
            };
        }
        return Application.Current.TryFindResource("ErrorBrush");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
```

**Validation:** `dotnet build` succeeds.

### Step 3: Theme File Updates

**3a. SemanticTokens.xaml** — change `InputBackgroundBrush`:

```xml
<SolidColorBrush x:Key="InputBackgroundBrush" Color="{StaticResource Catppuccin.Base}" />
```

**3b. GlobalStyles.xaml** — add DangerButtonStyle (copy default button template, replace hover trigger with ErrorBrush).

**3c. GlobalStyles.xaml** — add DataTrigger to PrimaryButtonStyle:

```xml
<DataTrigger Binding="{Binding IsActive}" Value="True">
    <Setter TargetName="border" Property="Background" Value="{StaticResource SuccessBrush}" />
    <Setter TargetName="border" Property="BorderBrush" Value="{StaticResource SuccessBrush}" />
</DataTrigger>
```

**Validation:** `dotnet build` succeeds. No XAML parse errors.

### Step 4: MainWindow.xaml — Layout Restructure

**4a. Top Bar** — remove Money/Power/SC/Reinforcement rows (RowDefinitions 2+3). Keep: title + status + path + params + action buttons.

**4b. Top Bar** — add status dot before StatusMessage:

```xml
<Ellipse Width="8" Height="8" Fill="{Binding ConnectionState, Converter={StaticResource ConnectionStateToBrushConverter}}" Margin="0,0,6,0" />
```

**4c. ScrollViewer content** — insert "基础资源" card first:

```xml
<Border Background="..." BorderBrush="..." CornerRadius="6" Padding="14" Margin="0,0,0,14">
  <StackPanel>
    <TextBlock Text="基础资源" FontSize="16" FontWeight="SemiBold" ... />
    <Grid>
      <!-- Money / Power / SC Point with labels -->
    </Grid>
  </StackPanel>
</Border>
```

**4d. 增援预设 card** — increase button margins to `12,0,0,0`. Add visual group separator between preset management and queue buttons.

**4e. Feature items** — `Hotkey` TextBlock `Foreground="{StaticResource TextDisabledBrush}"`.

**Validation:** `dotnet build` + visual check.

### Step 5: Button Style Assignment

Apply styles per the assignment map in design.md §7:
- `Style="{StaticResource PrimaryButtonStyle}"` on CTA buttons
- `Style="{StaticResource DangerButtonStyle}"` on dangerous action buttons
- Default buttons left unstyled (implicit)

**Validation:** Hover on "清空队列" → red. Hover on "一键启动" → Lavender. Click feature "开启" → turns green.

### Step 6: Converter in App.xaml

Add converter to Application.Resources or MainWindow.Resources (simpler: MainWindow.Resources since only MainWindow uses it):

```xml
<Window.Resources>
    <converters:ConnectionStateToBrushConverter x:Key="ConnectionStateToBrushConverter" />
</Window.Resources>
```

```xml
xmlns:converters="clr-namespace:Ra3Trainer.App.Converters"
```

### Step 7: Build & Smoke Test

```bash
dotnet build src/Ra3Trainer.App/ -c Debug
dotnet test tests/Ra3Trainer.Tests/
```

Manual verification checklist in design.md.

### Step 8: Update Spec

Update `.trellis/spec/app/component-guidelines.md`:
- Add button style assignment rules (Primary / Secondary / Danger)
- Add ConnectionState to ViewModel section

## Rollback Points

- Step 1: git checkout ViewModels/
- Step 2: remove Converters/ dir
- Step 3: git checkout Themes/
- Step 4-6: git checkout MainWindow.xaml
- Full: git checkout src/Ra3Trainer.App/

## Files NOT Changed

- `Ra3Trainer.Core` (any file)
- `tests/` (any file)
- `App.xaml` (except possibly converter resource)
- All `*.xaml.cs` files
- `CatppuccinMocha.xaml`
- `ReinforcementUnitPickerWindow.xaml`
