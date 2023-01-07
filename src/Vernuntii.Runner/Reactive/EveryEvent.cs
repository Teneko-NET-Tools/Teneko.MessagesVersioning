﻿using System.Diagnostics.CodeAnalysis;

namespace Vernuntii.Reactive;

internal class EveryEvent<T> : IEventDataHolder<T>, IEmittableEvent<T>, IUnschedulableEventEmitter<T>
{
    [MaybeNull]
    internal virtual T EventData =>
        throw new InvalidOperationException();

    internal virtual bool HasEventData =>
        false;

    [MaybeNull]
    T IEventDataHolder<T>.EventData =>
        EventData;

    bool IEventDataHolder<T>.HasEventData =>
        HasEventData;

    protected bool HasEventEntries =>
        _eventSubscriptions.Count != 0;

    private readonly SortedSet<EventSubscription> _eventSubscriptions = new();

    protected virtual IEventDataHolder<T>? InspectEmission(T eventData) =>
        new EventDataHolder<T>(eventData, hasEventData: true);

    protected void Emit(EventEmissionContext context, T eventData)
    {
        foreach (var eventEntry in _eventSubscriptions) {
            if (eventEntry.Handler.IsEmissionUnschedulable) {
                eventEntry.Handler.Emit(context, eventData);
            } else if (!context.IsCompleting) {
                // For now, we do not publicly allow handling completion
                context.ScheduleEventEmission(eventEntry.Handler, eventData);
            }
        }
    }

    protected virtual void MakeEmission(EventEmissionContext context, T eventData) =>
        Emit(context, eventData);

    internal void EvaluateEmission(EventEmissionContext context, T eventData)
    {
        var result = InspectEmission(eventData);

        if (result is null || !result.HasEventData) {
            return;
        }

        MakeEmission(context, eventData);
    }

    void IUnschedulableEventEmitter<T>.Emit(EventEmissionContext context, T eventData) =>
        EvaluateEmission(context, eventData);

    public virtual IDisposable Subscribe(IEventEmitter<T> eventEmitter)
    {
        var eventEntry = new EventSubscription(eventEmitter);
        _eventSubscriptions.Add(eventEntry);

        return new DelegatingDisposable<(ISet<EventSubscription>, EventSubscription)>(
            static result => result.Item1.Remove(result.Item2),
            (_eventSubscriptions, eventEntry));
    }

    protected readonly record struct EventSubscription : IComparable<EventSubscription>
    {
        private static uint s_nextId;

        public uint Id { get; }
        public IEventEmitter<T> Handler =>
            _eventEmitter ?? throw new InvalidOperationException();

        private readonly IEventEmitter<T>? _eventEmitter;

        public EventSubscription(IEventEmitter<T> eventEmitter)
        {
            Id = Interlocked.Increment(ref s_nextId);
            _eventEmitter = eventEmitter;
        }

        public int CompareTo(EventSubscription other) =>
            Id.CompareTo(other.Id);
    }
}
