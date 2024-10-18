using Avalonia.Media;

namespace Client;

public class HeaderKeyValueBlock : KeyValueBase
{
    public HeaderKeyValueBlock(SaveUiElement saveUiElement) : this()
    {
        this.Id = saveUiElement.Id;
        this._saveUiElement = saveUiElement;
    }
    public HeaderKeyValueBlock() : base()
    {
        Rectangle.Fill = Brushes.RosyBrown;
    }
}