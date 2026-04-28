using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace InkMD.Core.Services;

public sealed class RxMessageBus
{
    private readonly Subject<object> _subject = new();

    public static RxMessageBus Default { get; } = new();

    private RxMessageBus() { }

    public void Publish<TMessage>(TMessage message) where TMessage : notnull
    {
        _subject.OnNext(message);
    }

    public IObservable<TMessage> Subscribe<TMessage>()
    {
        return _subject.OfType<TMessage>();
    }
}
