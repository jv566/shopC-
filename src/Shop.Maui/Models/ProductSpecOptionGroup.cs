using System.Collections.ObjectModel;

namespace Shop.Maui.Models;

public sealed class ProductSpecOptionGroup
{
    public ProductSpecOptionGroup(string specName, IEnumerable<ProductColorImageOption> options)
    {
        SpecName = string.IsNullOrWhiteSpace(specName) ? "可选类型" : specName.Trim();
        Options = new ObservableCollection<ProductColorImageOption>(options);
    }

    public string SpecName { get; }

    public ObservableCollection<ProductColorImageOption> Options { get; }
}
