using System;

namespace OfdrwNet.Core
{
    [Serializable]
    public sealed class Holder<T>
    {
        public T Value { get; set; }

        public Holder()
        {
        }

        public Holder(T value)
        {
            Value = value;
        }

        public static implicit operator Holder<T>(T value)
        {
            return new Holder<T>(value);
        }

        public static implicit operator T(Holder<T> holder)
        {
            if (holder == null)
                return default!;
            return holder.Value;
        }

        public bool IsEmpty()
        {
            return Value == null || Value.Equals(default(T));
        }

        public bool HasValue()
        {
            return !IsEmpty();
        }

        public void Reset()
        {
            Value = default!;
        }

        public Holder<T> SetIfEmpty(T value)
        {
            if (IsEmpty())
            {
                Value = value;
            }
            return this;
        }

        public Holder<T> IfPresent(Action<T> action)
        {
            if (HasValue() && action != null)
            {
                action(Value);
            }
            return this;
        }

        public Holder<TResult> Map<TResult>(Func<T, TResult> mapper)
        {
            if (HasValue() && mapper != null)
            {
                return new Holder<TResult>(mapper(Value));
            }
            return new Holder<TResult>();
        }

        public T GetOrDefault(T defaultValue)
        {
            return HasValue() ? Value : defaultValue;
        }

        public T GetOrDefault(Func<T> defaultValueSupplier)
        {
            if (HasValue())
                return Value;
            return defaultValueSupplier != null ? defaultValueSupplier() : default!;
        }

        public override bool Equals(object obj)
        {
            if (obj is Holder<T> other)
            {
                if (Value == null && other.Value == null)
                    return true;

                return Value?.Equals(other.Value) ?? false;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Value?.GetHashCode() ?? 0;
        }

        public override string ToString()
        {
            return Value?.ToString() ?? "null";
        }
    }

    public static class Holder
    {
        public static Holder<T> Of<T>(T value)
        {
            return new Holder<T>(value);
        }

        public static Holder<T> Empty<T>()
        {
            return new Holder<T>();
        }

        public static Holder<T> OfNullable<T>(T value)
        {
            return new Holder<T>(value);
        }
    }
}
