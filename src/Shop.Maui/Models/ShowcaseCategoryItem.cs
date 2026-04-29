using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Shop.Maui.Models;

public sealed class ShowcaseCategoryItem : INotifyPropertyChanged
{
    private bool _isSelected;

    public ShowcaseCategoryItem(string id, string displayName)
    {
        Id = id;
        DisplayName = displayName;
    }

    public string Id { get; }

    public string DisplayName { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
            {
                return;
            }

            _isSelected = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(BackgroundColor));
            OnPropertyChanged(nameof(TextColor));
            OnPropertyChanged(nameof(BorderColor));
        }
    }

    public Color BackgroundColor => IsSelected ? Color.FromArgb("#2E8BFF") : Color.FromArgb("#27406B");

    public Color TextColor => IsSelected ? Colors.White : Color.FromArgb("#DCEBFF");

    public Color BorderColor => IsSelected ? Color.FromArgb("#6DE7FF") : Color.FromArgb("#40618D");

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
