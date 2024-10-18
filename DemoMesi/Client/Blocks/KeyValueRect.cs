using System;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace Client
{
    public class KeyValueRect : KeyValueBase
    {
        public KeyValueRect(SaveUiElement saveUiElement) : this()
        {
            this.Id = saveUiElement.Id;
            this._saveUiElement = saveUiElement;
        }
        public KeyValueRect() : base()
        {
        }
    }
}
