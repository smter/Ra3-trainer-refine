using System.Collections.ObjectModel;
using Microsoft.Win32;
using Ra3Trainer.Core.Features;

namespace Ra3Trainer.App.ViewModels;

public sealed class ReinforcementUnitPickerViewModel : ViewModelBase
{
    private readonly List<ReinforcementUnitEntry> _units;
    private string _searchText = string.Empty;
    private ReinforcementUnitEntry? _selectedUnit;
    private string _statusMessage;

    public ReinforcementUnitPickerViewModel(IEnumerable<ReinforcementUnitEntry> units)
    {
        _units = ReinforcementUnitCatalog.Merge(Array.Empty<ReinforcementUnitEntry>(), units).ToList();
        _statusMessage = $"共 {_units.Count} 条单位码。";
        FilteredUnits = new ObservableCollection<ReinforcementUnitEntry>();
        ConfirmCommand = new RelayCommand(Confirm, () => SelectedUnit is not null);
        CancelCommand = new RelayCommand(Cancel);
        ImportCommand = new RelayCommand(ImportFromFile);
        RefreshFilter();
    }

    public event Action<bool?>? RequestClose;

    public ObservableCollection<ReinforcementUnitEntry> FilteredUnits { get; }

    public IReadOnlyList<ReinforcementUnitEntry> Units => _units;

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (string.Equals(_searchText, value, StringComparison.Ordinal))
            {
                return;
            }

            _searchText = value ?? string.Empty;
            OnPropertyChanged();
            RefreshFilter();
        }
    }

    public ReinforcementUnitEntry? SelectedUnit
    {
        get => _selectedUnit;
        set
        {
            if (EqualityComparer<ReinforcementUnitEntry?>.Default.Equals(_selectedUnit, value))
            {
                return;
            }

            _selectedUnit = value;
            OnPropertyChanged();
            ConfirmCommand.RaiseCanExecuteChanged();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (string.Equals(_statusMessage, value, StringComparison.Ordinal))
            {
                return;
            }

            _statusMessage = value;
            OnPropertyChanged();
        }
    }

    public RelayCommand ImportCommand { get; }

    public RelayCommand ConfirmCommand { get; }

    public RelayCommand CancelCommand { get; }

    public void LoadFromFile(string path)
    {
        var imported = ReinforcementUnitCatalog.Load(path);
        if (imported.Count == 0)
        {
            StatusMessage = "没有读取到可用单位码。";
            return;
        }

        var beforeCount = _units.Count;
        foreach (var entry in imported)
        {
            if (_units.Any(item => item.Code == entry.Code))
            {
                continue;
            }

            _units.Add(entry);
        }

        RefreshFilter();
        var addedCount = _units.Count - beforeCount;
        StatusMessage = addedCount > 0
            ? $"已导入 {addedCount} 条单位码，当前共 {_units.Count} 条。"
            : $"导入完成，未增加新条目，当前共 {_units.Count} 条。";
    }

    private void ImportFromFile()
    {
        try
        {
            var dialog = new OpenFileDialog
            {
                Title = "选择单位代码文件",
                Filter = "文本文件 (*.txt;*.csv)|*.txt;*.csv|所有文件 (*.*)|*.*",
                CheckFileExists = true
            };

            if (dialog.ShowDialog() == true)
            {
                LoadFromFile(dialog.FileName);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"导入单位代码失败：{ex.Message}";
        }
    }

    private void RefreshFilter()
    {
        var filtered = ReinforcementUnitCatalog.Filter(_units, SearchText);

        var previouslySelected = SelectedUnit;

        FilteredUnits.Clear();
        foreach (var unit in filtered)
        {
            FilteredUnits.Add(unit);
        }

        if (previouslySelected is not null && !FilteredUnits.Contains(previouslySelected))
        {
            SelectedUnit = null;
        }
    }

    private void Confirm()
    {
        RequestClose?.Invoke(true);
    }

    private void Cancel()
    {
        RequestClose?.Invoke(false);
    }
}
