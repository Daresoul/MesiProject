using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;

namespace Client;

public class BaseContainer : BaseRect
{
    protected Rectangle TopRect;
    protected Rectangle BottomRect;
    public TextBox TextBox { get; protected set; }

    private Canvas _canvas;

    public List<BaseRect> Properties { get; } = new();

    public const double RectHeight = 35;

    public BaseContainer()
    {
        _canvas = new Canvas
        {
            Width = 200,
            Height = RectHeight * 2
        };

        var grid = new Grid
        {
            Height = RectHeight
        };
        
        TopRect = new Rectangle
        {
            Width = 200,
            Height = RectHeight,
            Stroke = Brushes.Black,
            StrokeThickness = 5,
            Fill = Brushes.Coral
        };
        
        TextBox = new TextBox
        {
            Width = 150,
            Height = 30,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        
        grid.Children.Add(TopRect);
        grid.Children.Add(TextBox);
        
        BottomRect = new Rectangle
        {
            Width = 200,
            Height = RectHeight,
            Stroke = Brushes.Black,
            StrokeThickness = 5,
            Fill = Brushes.Coral
        };
            
        _canvas.Children.Add(grid);
        Canvas.SetTop(grid, 0);
        Canvas.SetLeft(grid, 0);

        _canvas.Children.Add(BottomRect);
        Canvas.SetTop(BottomRect, TopRect.Height);
        Canvas.SetLeft(BottomRect, 0);
            
        this.Content = _canvas;
    }

    private void AddAllBlocks(BaseRect prop)
    {
        if (Properties.Contains(prop)) return;
        var current = prop;

        while (current.NextBlock != null && !Properties.Contains(current))
        {
            current.PartOf = this;
            Properties.Add(current);
            current = current.NextBlock;
        }

        current.PartOf = this;
        Properties.Add(current);
    }

    private void RemoveAllBlocks(BaseRect prop)
    {
        var current = prop;

        while (current.NextBlock != null)
        {
            Properties.Remove(current); 
            current.PartOf = null;
            current = current.NextBlock;
        }
        
        Properties.Remove(current);
        current.PartOf = null;
    }

    public void Resize()
    {
        double totalHeight = 0;
        
        foreach (var baseRect in Properties)
        {
            totalHeight += baseRect.GetBlockHeight();
        }
        
        _canvas.Height = totalHeight + RectHeight * 2;
        
        Canvas.SetTop(BottomRect, totalHeight + TopRect.Bounds.Bottom);

        PartOf?.Resize();
        NextBlock?.SnapIntoPlace(Bounds.Left);
    }
    
    public void AddBlock(BaseRect prop)
    {
        AddAllBlocks(prop);
        Resize();
    }

    public void RemoveBlock(BaseRect prop)
    {
        RemoveAllBlocks(prop);
        Resize();
    }
    
    public double GetMiddle()
    {
        return Bounds.Top + RectHeight;
    }

    public double GetRectHeight()
    {
        return RectHeight;
    }

    public override void SnapIntoPlace(double left)
    {
        base.SnapIntoPlace(left);
        if (Properties.Count > 0)
        {
            foreach (var block in Properties)
            {
                block.SnapIntoPlace(left);
            }
        }
    }

    public override void MoveChildren(Point currentPosition, Point lastPosition)
    {
        base.MoveChildren(currentPosition, lastPosition);
        if (Properties.Count > 0) Properties.First().MoveChildren(currentPosition, lastPosition);
    }
    
    public override double GetBlockHeight()
    {
        return _canvas.Height;
    }
}