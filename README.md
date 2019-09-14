# DelegatedPropertySharp.Fody

This is an add-in for [Fody](https://github.com/Fody/Fody).  
Created by Cyl18.  
**Still work in process**.  
The idea comes from [Kotlin delegated property](https://kotlinlang.org/docs/reference/delegated-properties.html).

## Installation

- Install the NuGet package:
  ```
  Install-Package DelegatedPropertySharp.Fody
  ```
- Add `<DelegatedPropertySharp/>` to your `FodyWeavers.xml`.

## Example

### Before

```csharp
    public class Class1
    {
        [Lazy] public int LazyObject1 { get; }
        [Lazy] public object LazyObject2 { get; set; }
    }

    public class LazyAttribute : DelegatedPropertyAttributeBase
    {
    }

    public class Handler<TThis, TProperty> : IDelegatedPropertyHandler<TThis, TProperty, Lazy>
    {
        public PropertyInfo PropertyInfo { get; set; }
        readonly Lazy<TProperty> _lazy = new Lazy<TProperty>();

        public TProperty Get(TThis @this)
        {
            return _lazy.Value;
        }

        public void Set(TThis @this, TProperty value)
        {
            throw new InvalidOperationException();
        }
    }
```

### What gets compiled

```csharp
    public class Class1()
    {
        static Class1()
        {
            _handler0 = new Handler<Class1, int>();
            _handler0.PropertyInfo = typeof(Class1).GetProperty("LazyObject1");

            _handler1 = new Handler<Class1, object>();
            _handler1.PropertyInfo = typeof(Class1).GetProperty("LazyObject2");
        }

        private static Handler<Class1, int> _handler0;
        [Lazy]
        public int LazyObject1
        {
            get => _handler0.Get(this);
        }

        private static Handler<Class1, object> _handler1;
        [Lazy]
        public object LazyObject2
        {
            get => _handler1.Get(this);
            set => _handler1.Set(this, value);
        }
    }
    ...
```
