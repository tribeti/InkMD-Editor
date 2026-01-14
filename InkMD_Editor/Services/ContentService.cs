using InkMD_Editor.Helpers;
using System;

namespace InkMD_Editor.Services;

public class ContentService
{
    private State<string> _contentState;

    public ContentService ()
    {
        _contentState = new State<string>(string.Empty);
    }

    public IDisposable SubcribetoContentChanges (Action<string> onChange) => _contentState.Subscribe(onChange);

}
