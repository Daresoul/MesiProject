using System.Text;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;

namespace Client;

public class KeyValueBase : BaseRect
{
    public TextBox TextBox1 { get; }
    public TextBox TextBox2 { get; }
    
    protected Rectangle Rectangle;

    const double RectHeight = 50;
    
    public KeyValueBase()
    {
        var grid = new Grid
        {
            Width = 500,
            Height = RectHeight,
        };
            
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
            
        Rectangle = new Rectangle
        {
            Fill = Brushes.LightBlue,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
            
        TextBox1 = new TextBox
        {
            Width = 150,
            Height = 30,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
            
        TextBox2 = new TextBox
        {
            Width = 150,
            Height = 30,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
            
        grid.Children.Add(Rectangle);
        Grid.SetColumnSpan(Rectangle, 2);
            
        grid.Children.Add(TextBox1);
        Grid.SetColumn(TextBox1, 0);

        grid.Children.Add(TextBox2);
        Grid.SetColumn(TextBox2, 1);
            
        Content = grid;
    }
    
    public override string GetJsonValue(int depth = 0)
    {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < depth; i++)
        {
            sb.Append("  ");
        }
        sb.Append("\"");
        sb.Append(TextBox1.Text);
        sb.Append("\"");
        sb.Append(": ");
        sb.Append("\"");
        sb.Append(TextBox2.Text);
        sb.Append("\"");

        if (NextBlock != null)
        {
            sb.Append(",");
        }
            
        sb.Append("\n");

        return sb.ToString();
    }
    
    public override double GetBlockHeight()
    {
        return RectHeight;
    }
}