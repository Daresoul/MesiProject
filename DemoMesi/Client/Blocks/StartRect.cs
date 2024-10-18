using System;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;

namespace Client;

public class StartRect : BaseRect
{

    private double RectHeight = 100;
    
    public StartRect(SaveUiElement saveUiElement) : this()
    {
        this.Id = saveUiElement.Id;
        SaveUiElement = saveUiElement;
    }
    
    public StartRect()
    {
        var grid = new Grid
        {
            Width = 200,
            Height = RectHeight
        };
        
        var rectangle = new Rectangle
        {
            Fill = Brushes.Red,
        };
        
        var text = new TextBlock()
        {
            Text = "Body",
            FontSize = 30,
            TextAlignment = TextAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        
        grid.Children.Add(rectangle);
        grid.Children.Add(text);
        
        this.Content = grid;
    }

    public override double GetBlockHeight()
    {
        return RectHeight;
    }
}