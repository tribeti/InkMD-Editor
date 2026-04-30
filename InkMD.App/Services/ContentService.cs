using InkMD.Core.Helpers;
using System;

namespace InkMD.App.Services;

public class ContentService
{
    private State<string> _contentState;

    public ContentService()
    {
        _contentState = new State<string>(string.Empty);
    }

    public IDisposable SubscribeToContentChanges(Action<string> onChange) => _contentState.Subscribe(onChange);
}
