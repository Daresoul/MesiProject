using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Avalonia;

namespace Client;

public class SaveManager
{
    private readonly string _stateFilePath = "appState.json";

    private readonly List<SaveUiElement> _saveStates = new List<SaveUiElement>();
    
    public void SaveState(List<BaseRect> baseRects, StartRect startRect, HeaderStartBlock headerStartBlock)
    {
        _saveStates.Clear();
        foreach (var rect in baseRects)
        {
            String? val1 = null;
            String? val2 = null;
            String? val3 = null;
            var guids = new List<Guid>();

            switch (rect)
            {
                case KeyValueRect keyValueRect:
                    val1 = keyValueRect.TextBox1.Text;
                    val2 = keyValueRect.TextBox2.Text;
                    break;
                case ObjectRect objectRect:
                    val1 = objectRect.TextBox.Text;
                    foreach (var brect in objectRect.Properties)
                    {
                        guids.Add(brect.Id);
                    }
                    break;
                case ArrayRect arrayRect:
                    val1 = arrayRect.TextBox.Text;
                    foreach (var brect in arrayRect.Properties)
                    {
                        guids.Add(brect.Id);
                    }
                    break;
                case HeaderKeyValueBlock headerKeyValueBlock:
                    val1 = headerKeyValueBlock.TextBox1.Text;
                    val2 = headerKeyValueBlock.TextBox2.Text;
                    break;
                default:
                    break;
            }
            
            _saveStates.Add(new SaveUiElement(
                rect.Id,
                rect.GetType(),
                rect.Bounds.Left,
                rect.Bounds.Top,
                rect.NextBlock,
                rect.NextBlockOf,
                rect.PartOf,
                val1,
                val2,
                val3,
                guids
            ));
        }

        var saveState = new SaveState(startRect.Id, startRect.NextBlock?.Id, headerStartBlock.Id, headerStartBlock.NextBlock?.Id, _saveStates);

        string jsonString = JsonSerializer.Serialize(saveState, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_stateFilePath, jsonString);
    }

    public void DiscardSaveStates()
    {
        File.Delete(_stateFilePath);
    }
    
    public SaveState LoadState()
    {
        if (File.Exists(_stateFilePath))
        {
            string jsonString = File.ReadAllText(_stateFilePath);
            return JsonSerializer.Deserialize<SaveState>(jsonString) ??  new SaveState();
        }

        return new SaveState();
    }
}