using System;
using System.Text;
using Avalonia.Media;

namespace Client;

public class ArrayRect : BaseContainer
{

    public ArrayRect(SaveUiElement saveUiElement) : this()
    {
        this.Id = saveUiElement.Id;
        this._saveUiElement = saveUiElement;
    }
    public ArrayRect() : base()
    {
        this.TopRect.Fill = Brushes.Gray;
        this.BottomRect.Fill = Brushes.Gray;
    }
    
    public override string GetJsonValue(int depth = 0)
    {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < depth; i++)
        {
            sb.Append("  ");
        }

        sb.Append("\"");
        sb.Append(TextBox.Text);
        sb.Append("\"");
        sb.Append(": ");
        sb.Append("[\n");
        foreach (var baseRect in this.Properties)
        {
            sb.Append(baseRect.GetJsonValue(depth + 1));
        }
        for (int i = 0; i < depth; i++)
        {
            sb.Append("  ");
        }
        sb.Append("]");
        if (NextBlock != null)
        {
            sb.Append(",");
        }
            
        sb.Append("\n");
        return sb.ToString();
    }
}