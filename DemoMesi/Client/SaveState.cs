using System;
using System.Collections.Generic;

namespace Client;

public class SaveState
{
    public Guid StartBlockId { get; set; }
    public Guid? StartBlockNext { get; set; }
    
    public Guid HeaderStartBlockId { get; set; }
    
    public Guid? HeaderStartBlockNext { get; set; }

    public List<SaveUiElement> Elements { get; set; }

    public SaveState(Guid startBlockId, Guid? startBlockNext, Guid headerStartBlockId, Guid? headerStartBlockNext, List<SaveUiElement> elements)
    {
        StartBlockId = startBlockId;
        StartBlockNext = startBlockNext;
        HeaderStartBlockId = headerStartBlockId;
        HeaderStartBlockNext = headerStartBlockNext;
        Elements = elements;
    }

    public SaveState()
    {
        Elements = new List<SaveUiElement>();
        StartBlockNext = null;
        StartBlockId = Guid.NewGuid();
    }
}