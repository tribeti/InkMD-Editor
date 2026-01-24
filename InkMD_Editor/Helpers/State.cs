using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace InkMD_Editor.Helpers;

public class State<T>(T initialState)
{
    private BehaviorSubject<T> _subject = new(initialState);

    public T CurrentValue => _subject.Value;

    public IObservable<T> AsObservable() => _subject.AsObservable();

    public IDisposable Subscribe(Action<T> onChange) => _subject.Subscribe(onChange);

    public void SetValue(T state) => _subject.OnNext(state);
}
