namespace Shop.Maui.Models;

public sealed record ProductListDisplayItem(
    ProductListItem Product,
    string ImageSource,
    string DisplayPriceText,
    string DisplayModelText,
    string ModelLabelImageSource,
    string PriceLabelImageSource,
    string CardBackgroundImageSource);
