﻿using System.Diagnostics.CodeAnalysis;

namespace Vernuntii.Reactive;

internal class EveryEvent<T> : IEventDataHolder<T>, IFulfillableEvent<T>, IUnschedulableEventFulfiller<T>
{
    [MaybeNull]
    internal virtual T EventData => throw new InvalidOperationException();

    internal virtual bool HasEventData => false;
    internal virtual bool IsCompleted => false;

    [MaybeNull]
    T IEventDataHolder<T>.EventData => EventData;

    bool IEventDataHolder<T>.HasEventData => HasEventData;

    protected bool HasEventEntries => _eventEntries.Count != 0;

    private readonly SortedSet<EventFulfillerEntry> _eventEntries = new();

    protected virtual IEventDataHolder<T> CanEvaluate(T eventData) =>
        new EventDataHolder<T>(eventData, hasEventData: true);

    protected void Fullfill(EventFulfillmentContext context, T eventData)
    {
        foreach (var eventEntry in _eventEntries) {
            if (eventEntry.Handler.IsFulfillmentUnschedulable) {
                eventEntry.Handler.Fulfill(context, eventData);

            } else {
                context.ScheduleFulfillment(eventEntry.Handler, eventData);
            }
        }
    }

    protected virtual void PostEvaluation(EventFulfillmentContext context, T eventData) =>
        Fullfill(context, eventData);

    internal void Evaluate(EventFulfillmentContext context, T eventData)
    {
        var result = CanEvaluate(eventData);

        if (!result.HasEventData) {
            return;
        }

        PostEvaluation(context, eventData);
    }

    void IUnschedulableEventFulfiller<T>.Fulfill(EventFulfillmentContext context, T eventData) =>
        Evaluate(context, eventData);

    public virtual IDisposable Subscribe(IEventFulfiller<T> eventFulfiller)
    {
        var eventEntry = new EventFulfillerEntry(eventFulfiller);
        _eventEntries.Add(eventEntry);

        return new DelegatingDisposable<(ISet<EventFulfillerEntry>, EventFulfillerEntry)>(
            static result => result.Item1.Remove(result.Item2),
            (_eventEntries, eventEntry));
    }

    protected readonly record struct EventFulfillerEntry : IComparable<EventFulfillerEntry>
    {
        private static uint s_nextId;

        public uint Id { get; }
        public IEventFulfiller<T> Handler =>
            _eventFulfiller ?? throw new InvalidOperationException();

        private readonly IEventFulfiller<T>? _eventFulfiller;

        public EventFulfillerEntry(IEventFulfiller<T> eventFulfiller)
        {
            Id = Interlocked.Increment(ref s_nextId);
            _eventFulfiller = eventFulfiller;
        }

        public int CompareTo(EventFulfillerEntry other) =>
            Id.CompareTo(other.Id);
    }
}