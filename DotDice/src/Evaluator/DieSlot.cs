using System.Buffers;
using DotDice.Parser;

namespace DotDice.Evaluator
{
    /// <summary>
    /// The evaluator's internal working representation of one die.
    ///
    /// Holds the same per-die state as <see cref="DieEvent"/>, minus the three fields
    /// that are constant across a single basic roll: the die type, the group id and the
    /// group operator. Those are supplied once when the slots are turned into events,
    /// which keeps a slot to a handful of value-type fields with no reference at all.
    ///
    /// A struct because the point is to keep an evaluation's working set off the heap.
    /// A 500 die pool is one buffer here, against 500 individually allocated records
    /// before.
    /// </summary>
    internal struct DieSlot
    {
        public int Value;
        public DieEventType Type;
        public RollSignificance Significance;
        public DieStatus Status;
        public SuccessStatus Success;

        /// <summary>
        /// True for slots produced by rolling a die, false for the synthetic slots that
        /// carry a success count or a constant modifier.
        ///
        /// Synthetic slots materialise with a null die type, matching what the
        /// event-based pipeline produced for them.
        /// </summary>
        public bool IsDie;
    }

    /// <summary>
    /// A growable list of <see cref="DieSlot"/> over caller-provided storage.
    ///
    /// Small pools sit in a stack buffer the caller supplies and touch no pool at all.
    /// Anything larger, or anything that outgrows the buffer, moves to a pooled array.
    /// That split matters because renting and returning costs about as much as rolling
    /// a handful of dice, which is most rolls: a fixed cost that would otherwise land
    /// hardest on exactly the expressions people write most.
    ///
    /// Growth is needed because explosions and rerolls are unbounded by dice count.
    /// MaxExplosions and MaxCompounds are 100 each and there is no cap on pool size, so
    /// "500d20!" can reach roughly 50,500 slots.
    ///
    /// A ref struct so it can hold a stack buffer. It must be passed by reference to
    /// anything that can grow it, or the caller keeps a stale view.
    /// </summary>
    internal ref struct SlotList
    {
        /// <summary>
        /// Slots a caller should reserve on the stack. Chosen to cover ordinary
        /// expressions outright: at 24 bytes a slot this is well under a kilobyte, and
        /// a pool above it is large enough that a rental is noise against the rolling.
        /// </summary>
        public const int StackCapacity = 32;

        private Span<DieSlot> _slots;
        private DieSlot[]? _rented;
        private int _count;

        /// <summary>
        /// Uses <paramref name="stackBuffer"/> when it is big enough for
        /// <paramref name="capacity"/>, and rents up front when it is not, so a large
        /// pool never grows its way up one doubling at a time.
        /// </summary>
        public SlotList(Span<DieSlot> stackBuffer, int capacity)
        {
            if (capacity <= stackBuffer.Length)
            {
                _slots = stackBuffer;
                _rented = null;
            }
            else
            {
                _rented = ArrayPool<DieSlot>.Shared.Rent(capacity);
                _slots = _rented;
            }

            _count = 0;
        }

        public int Count => _count;

        /// <summary>
        /// A reference to the slot, so callers can mutate status in place.
        ///
        /// The reference is invalidated by anything that grows the list, so it must not
        /// be held across an <see cref="Add"/>.
        /// </summary>
        public ref DieSlot this[int index] => ref _slots[index];

        public void Add(in DieSlot slot)
        {
            if (_count == _slots.Length)
            {
                Grow();
            }

            _slots[_count++] = slot;
        }

        public void Clear() => _count = 0;

        private void Grow()
        {
            var bigger = ArrayPool<DieSlot>.Shared.Rent(Math.Max(_slots.Length * 2, 8));
            _slots.Slice(0, _count).CopyTo(bigger);

            if (_rented != null)
            {
                ArrayPool<DieSlot>.Shared.Return(_rented);
            }

            _rented = bigger;
            _slots = bigger;
        }

        /// <summary>
        /// Returns any pooled array. Safe to call on a purely stack-backed list, and
        /// safe to call twice, so callers can pair it with a finally block without
        /// tracking which backing they ended up with.
        /// </summary>
        public void Return()
        {
            if (_rented != null)
            {
                ArrayPool<DieSlot>.Shared.Return(_rented);
                _rented = null;
            }

            _slots = default;
            _count = 0;
        }
    }
}
