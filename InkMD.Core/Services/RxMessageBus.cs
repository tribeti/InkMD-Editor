using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using InkMD.Core.Messages;

namespace InkMD.Core.Services;

public sealed class RxMessageBus
{
    private readonly Subject<object> _subject = new();
    private readonly ReplaySubject<FormattingStateMessage> _formattingStateSubject = new(1);
    private readonly ReplaySubject<FontChangedMessage> _fontChangedSubject = new(1);

    public static RxMessageBus Default { get; } = new();

    private RxMessageBus() { }

    public void Publish<TMessage>(TMessage message) where TMessage : notnull
    {
        if (message is FormattingStateMessage fsm)
        {
            _formattingStateSubject.OnNext(fsm);
        }
        else if (message is FontChangedMessage fcm)
        {
            _fontChangedSubject.OnNext(fcm);
        }
        else
        {
            _subject.OnNext(message);
        }
    }

    public IObservable<TMessage> Subscribe<TMessage>()
    {
        if (typeof(TMessage) == typeof(FormattingStateMessage))
        {
            return (IObservable<TMessage>) (object) _formattingStateSubject.AsObservable();
        }

        if (typeof(TMessage) == typeof(FontChangedMessage))
        {
            return (IObservable<TMessage>) (object) _fontChangedSubject.AsObservable();
        }

        return _subject.OfType<TMessage>();
    }
}
