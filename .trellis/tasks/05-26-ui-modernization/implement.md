# Implementation Plan: UI Modernization

## Execution Order

### Step 1: Infrastructure — Theme Directory & Font

**Actions:**
1. Create `src/Ra3Trainer.App/Themes/` directory
2. Copy `res/SarasaMonoSC-Regular.ttf` → `src/Ra3Trainer.App/Themes/SarasaMonoSC-Regular.ttf`
3. Update `.csproj` — add font resource:
```xml
<ItemGroup>
  <Resource Include="Themes\SarasaMonoSC-Regular.ttf" />
</ItemGroup>
```

**Validation:** `dotnet build src/Ra3Trainer.App/` succeeds

### Step 2: CatppuccinMocha.xaml — Raw Color Palette

**Action:** Create `src/Ra3Trainer.App/Themes/CatppuccinMocha.xaml`

25 `Color` resources with key naming `Catppuccin.{Name}`:
- Rosewater, Flamingo, Pink, Mauve, Red, Maroon, Peach, Yellow, Green, Teal, Sky, Sapphire, Blue, Lavender
- Text, Subtext1, Subtext0, Overlay2, Overlay1, Overlay0, Surface2, Surface1, Surface0, Base, Mantle, Crust

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <Color x:Key="Catppuccin.Base">#1E1E2E</Color>
  <Color x:Key="Catppuccin.Mantle">#181825</Color>
  <!-- ... all 25 colors -->
</ResourceDictionary>
```

**Validation:** `dotnet build` succeeds. ResourceDictionary loads without parse errors.

### Step 3: SemanticTokens.xaml — Semantic Brush Mapping

**Action:** Create `src/Ra3Trainer.App/Themes/SemanticTokens.xaml`

Map raw colors → semantic `SolidColorBrush` tokens (see design.md Layer 2 table).
Merges `CatppuccinMocha.xaml` internally.

```xml
<ResourceDictionary>
  <ResourceDictionary.MergedDictionaries>
    <ResourceDictionary Source="CatppuccinMocha.xaml" />
  </ResourceDictionary.MergedDictionaries>
  <SolidColorBrush x:Key="WindowBackgroundBrush" Color="{StaticResource Catppuccin.Base}" />
  <!-- ... all semantic tokens -->
</ResourceDictionary>
```

**Validation:** `dotnet build` succeeds.

### Step 4: GlobalStyles.xaml — Implicit Control Styles

**Action:** Create `src/Ra3Trainer.App/Themes/GlobalStyles.xaml`

Merges `SemanticTokens.xaml`. Define:

1. **AppFont** — `FontFamily` resource referencing embedded Sarasa Mono SC
2. **DefaultButtonStyle** — implicit `Style TargetType="Button"` (no x:Key)
3. **PrimaryButtonStyle** — explicit `Style x:Key="PrimaryButtonStyle"` for CTA buttons
4. **TextBoxStyle** — implicit dark textbox
5. **ComboBoxStyle** — implicit dark combobox
6. **ListViewStyle** — implicit dark listview
7. **WindowStyle** — implicit `Style TargetType="Window"` with `Background="{StaticResource WindowBackgroundBrush}"`

Key template patterns:
```
Button: flat, 6px radius, surface0 bg + 1px Overlay0 border → hover Mauve border
TextBox: 4px radius, Surface0 bg, Overlay0 border → focus Mauve border
```

**Validation:** `dotnet build` succeeds. No XAML parse errors in GlobalStyles.xaml.

### Step 5: App.xaml — Wire Up Resources

**Action:** Modify `src/Ra3Trainer.App/App.xaml`:

```xml
<Application x:Class="Ra3Trainer.App.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             StartupUri="MainWindow.xaml">
  <Application.Resources>
    <ResourceDictionary>
      <ResourceDictionary.MergedDictionaries>
        <ResourceDictionary Source="Themes/GlobalStyles.xaml" />
      </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
  </Application.Resources>
</Application>
```

Remove any `Background` attribute that may exist at Application level.

**Validation:** `dotnet build` succeeds. Application starts without resource-not-found exceptions.

### Step 6: Migrate MainWindow.xaml

**Action:** Replace all hardcoded color values with `{StaticResource}` references:

| Current | Replacement |
|---------|-------------|
| `Background="#F4F5F7"` (Window) | Remove — handled by WindowStyle |
| `Background="#1F2937"` (Top Bar) | `{StaticResource TopBarBackgroundBrush}` |
| `Foreground="White"` (Title) | `{StaticResource TextPrimaryBrush}` |
| `Foreground="#CBD5E1"` (Top bar text) | `{StaticResource TextSecondaryBrush}` |
| `Foreground="#111827"` (Heading) | `{StaticResource TextPrimaryBrush}` |
| `Foreground="#4B5563"` (Labels) | `{StaticResource TextSecondaryBrush}` |
| `Foreground="#6B7280"` (Status) | `{StaticResource TextSecondaryBrush}` |
| `Background="White"` (Cards) | `{StaticResource CardBackgroundBrush}` |
| `BorderBrush="#D7DAE0"` (Cards) | `{StaticResource CardBorderBrush}` |
| `Foreground="#1F2937"` (Items) | `{StaticResource TextPrimaryBrush}` |
| CTA Button `Background` | `Style="{StaticResource PrimaryButtonStyle}"` instead |

**Existing `CornerRadius="6"`** on cards — keep, matches design.

Note: `Binding` paths and `Command` bindings **untouched**.

**Validation:** Visual comparison — all text readable, cards visible, layout intact.

### Step 7: Migrate ReinforcementUnitPickerWindow.xaml

**Action:** Same replacement pattern as MainWindow:

| Current | Replacement |
|---------|-------------|
| `Background="#F4F5F7"` (Window) | Remove — handled by WindowStyle |
| `Background="White"` (ListView) | `{StaticResource CardBackgroundBrush}` |
| `BorderBrush="#D7DAE0"` | `{StaticResource CardBorderBrush}` |
| `Foreground="#4B5563"` | `{StaticResource TextSecondaryBrush}` |

**Validation:** Open unit picker from main window → all rows visible, search works, selection works.

### Step 8: Build & Smoke Test

**Commands:**
```bash
dotnet build src/Ra3Trainer.App/ -c Release
```

**Manual verification checklist:**
- [ ] Window opens with dark theme
- [ ] Top bar is darker than background
- [ ] All text readable (contrast check)
- [ ] Buttons show hover/pressed states
- [ ] TextBox input visible against dark background
- [ ] ComboBox dropdown readable
- [ ] Reinforcement unit picker: search, select, confirm works
- [ ] Preset save/apply buttons functional
- [ ] Queue items visible and removable
- [ ] Feature group cards distinct from background

### Step 9: Update Spec Documents

**Files to update:**
1. `.trellis/spec/app/quality-guidelines.md` — §"避免内联样式" section: update from "不创建 Style/ResourceDictionary" to "颜色通过 StaticResource 引用，不硬编码"
2. `.trellis/spec/app/component-guidelines.md` — add: "颜色使用 Catppuccin Mocha 主题系统的 StaticResource token，参见 `Themes/` 目录"
3. `.trellis/spec/app/state-management.md` — no change needed (only XAML layer affected)

## Rollback Points

- After Step 3: If semantic token mapping is wrong, only `SemanticTokens.xaml` needs reverting
- After Step 5: If App.xaml merge fails, revert `App.xaml` to empty `<Application.Resources />`
- After Step 6/7: If a migration goes wrong, git checkout that single XAML file
- Full rollback: `git checkout -- src/Ra3Trainer.App/` + remove `Themes/` directory

## Files NOT Changed

- `MainWindow.xaml.cs` — code-behind minimal
- `ReinforcementUnitPickerWindow.xaml.cs`
- `App.xaml.cs`
- All `ViewModels/*.cs` files
- `Ra3Trainer.Core` project (any file)
- `tests/` project (any file)
