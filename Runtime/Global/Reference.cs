using System;
using Unity.Collections.LowLevel.Unsafe;

namespace E.Entities
{
    public interface IReference { }

    public unsafe struct Reference<T> : IReference, IEquatable<Reference<T>> where T : unmanaged
    {
        [NativeDisableUnsafePtrRestriction]
        public readonly T* ptr;

        public Reference(T* ptr)
        {
            this.ptr = ptr;
        }

        public ref T Ref => ref *ptr;

        public override bool Equals(object obj) => obj is Reference<T> reference && Equals(reference);

        public bool Equals(Reference<T> other) => ptr == other.ptr;

        public override int GetHashCode() => HashCode.Combine((long)ptr);

        public static bool operator ==(Reference<T> left, Reference<T> right) => left.ptr == right.ptr;

        public static bool operator !=(Reference<T> left, Reference<T> right) => left.ptr != right.ptr;

        public static implicit operator T(Reference<T> reference) => *reference.ptr;

        public static implicit operator T*(Reference<T> reference) => reference.ptr;
    }
}