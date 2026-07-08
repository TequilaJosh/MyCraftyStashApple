using CommunityToolkit.Mvvm.ComponentModel;
using MyCraftyStash.Models;

namespace MyCraftyStash.ViewModels;

public partial class ItemDetailViewModel : ObservableObject, IQueryAttributable
{
    [ObservableProperty] public partial StashItem? Item { get; set; }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("Item", out var value) && value is StashItem item)
            Item = item;
    }
}
