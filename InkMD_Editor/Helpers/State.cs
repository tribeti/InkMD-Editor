using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace InkMD_Editor.Helpers;

public class State<T>
{
    private BehaviorSubject<T> _subject;

    public State (T initialState)
    {
        _subject = new BehaviorSubject<T>(initialState);
    }

    public T CurrentValue => _subject.Value;

    public IObservable<T> AsObservable () => _subject.AsObservable();

    public IDisposable Subscribe (Action<T> onChange) => _subject.Subscribe(onChange);

    public void SetValue (T state) => _subject.OnNext(state);
}
