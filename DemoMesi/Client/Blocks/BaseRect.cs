using System;
using Avalonia;
using Avalonia.Controls;

namespace Client;

public class BaseRect : UserControl
{
    public Guid Id = Guid.NewGuid();
    public BaseRect? NextBlock = null;
    public BaseContainer? PartOf = null;
    public BaseRect? NextBlockOf = null;
    private bool BegunMoving = false;
    private bool BegunSnapping = false;
    
    private bool _isStatic = false;
    
    public bool isStatic
    {
        get => _isStatic;
        set => _isStatic = value;
    }
    
    public SaveUiElement _saveUiElement;


    public virtual String GetJsonValue(int depth = 0)
    {
        return "";
    }

    public virtual void MoveChildren(Point currentPosition, Point lastPosition)
    {
        if (NextBlock == this) return;
        
        BegunMoving = true;
        
        var offsetX = currentPosition.X - lastPosition.X;
        var offsetY = currentPosition.Y - lastPosition.Y;
                
        var left = Canvas.GetLeft(this);
        var top = Canvas.GetTop(this);

        Canvas.SetLeft(this, left + offsetX);
        Canvas.SetTop(this, top + offsetY);
        
        NextBlock?.MoveChildren(new Point(currentPosition.X, currentPosition.Y), lastPosition);
        BegunMoving = false;
    }

    public virtual void SnapIntoPlace(double left)
    {
        if (NextBlock == this) return;
    
        double topPosition;

        var prevBlock = this.NextBlockOf;
        
        if (prevBlock == null && PartOf != null)
        {
            topPosition = Canvas.GetTop(PartOf) + PartOf.GetRectHeight() + PartOf.Properties.IndexOf(this) * GetBlockHeight();
            Canvas.SetLeft(this, left);
            Canvas.SetTop(this, topPosition);
            Console.WriteLine("Snapping to PartOf");
        }
        else if (prevBlock != null)
        {
            topPosition = Canvas.GetTop(prevBlock) + prevBlock.GetBlockHeight();
            Canvas.SetLeft(this, left);
            Canvas.SetTop(this, topPosition);
            Console.WriteLine("Snapping to previous block");
        }
        else
        {
            throw new Exception("Cannot snap out of place.");
        }
        
        NextBlock?.SnapIntoPlace(left);
    }

    public virtual double GetBlockHeight()
    {
        return 0;
    }

}