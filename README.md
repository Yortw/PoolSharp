# PoolSharp

## What is PoolSharp ?
PoolSharp is a simple, light weight, thread safe object pool.

It also supports pooling of disposable types, managing the life time of pooled objects and performing early dispose when possible.
Pool implementations implement a simple common interface, so they can be mocked or replaced with alternatives.

[![GitHub license](https://img.shields.io/github/license/mashape/apistatus.svg)](https://github.com/Yortw/PoolSharp/blob/master/LICENSE.md) 

## Supported Platforms
Currently;

* .Net Framework 4.0+
* Xamarin.iOS
* Xamarin.Android
* WinRT (Windows Store Apps 8.1)
* UWP 10+ (Windows 10 Universal Programs)

## Build Status
[![Build status](https://ci.appveyor.com/api/projects/status/f4e33as09yx0lsn4?svg=true)](https://ci.appveyor.com/project/Yortw/poolsharp)

## How do I use PoolSharp?
*We got your samples right here*

Install the Nuget package like this;

```powershell
    PM> Install-Package PoolSharp
```

[![NuGet Status](http://img.shields.io/nuget/v/PoolSharp.svg?style=flat)](https://www.nuget.org/packages/PoolSharp)
[![Nuget](https://img.shields.io/nuget/dt/PoolSharp.svg)](https://www.nuget.org/packages/PoolSharp)

Or reference the PoolSharp.dll assembly that matches your app's platform.

### Creating a pool.
Create a PoolPolicy<T> instance to configure options and behaviour for the pool, T is the type of item being pooled.
Create a new Pool<T> instance passing the pool policy you created. Pool policies can be re-used across pools so long as the assigned Function and Action delegates are thread-safe.

```C#

    using PoolSharp;
    // Define a policy. This policy;
    //  Is for a StringBuilder pool.
    //  Synchronously resets the StringBuilder state when the item is returned to the pool.
    //  Pools at most 10 instances

    var policy = new PoolPolicy<System.Text.StringBuilder>()
    {
    	Factory = (poolInstance) => new System.Text.StringBuilder(),
    	InitializationPolicy = PooledItemInitialization.Return,
    	MaximumPoolSize = 10,
    	ReinitializeObject = (sb) => sb.Clear()
    };
    
    //Now create a new pool using the policy.
	IPool<System.Text.StringBuilder> pool = new Pool<System.Text.StringBuilder>(policy);

    //Retrieve an instance from the pool
    var stringbuilder = pool.Take();
 
    //Do something with the stringbuilder   
    
    //Return the string builder to the pool
    pool.Add(stringbuilder);    
```

### Using a Pool
Use the Take method to retrieve an instance from the pool. Use the Add method to return an instance to the pool so it can be re-used.

```C#
    //Retrieve an instance from the pool
    var stringbuilder = pool.Take();
 
    //Do something with the stringbuilder   
    
    //Return the string builder to the pool
    pool.Add(stringbuilder);    
```

#### Using a Pool with Auto-Return Semantics
Instead of creating a pool for your specific type, create the pool for PooledObject<T> where T is the type you actually want.
Then you can use auto-return like this;

```C#
    //Retrieve an instance from the pool
    using (var pooledItem pool.Take())
    {
        //pooledItem.Value is the object you actually want.
        //If the pool is for tyhe type PooledObject<System.Text.StringBuilder> then
        //you can access the string builder instance like this;
        pooledItem.Value.Append("Some text to add to the builder");
        
    } // The item will automatically be returned to the pool here.
```

## Contributing
Contributing is encouraged! Please submit pull requests, open issues etc. However, to ensure we end up with a good result and to make my life a little easier, could I please request that;

* All changes be made in a feature branch, not in master, and please don't submit PR's directly against master.
* Make sure any PR contains (well named) new and/or updated unit tests to prove the new feature or bug fix. Failing this, please include enough sample data/problem description that I can write the tests myself.
  
Also, not required, but would be really great if;

* You could use tabs instead of spaces (and not argue about it).
* You could write the code in a similar style as what already exists. I'm not OCD about this so some deviation is fine, we all have different styles and I'm not suggesting mine is 'right', but it helps everybody 
undertand and maintain the code base when it is at least mostly uniform.

Thanks! I look forward to merging your awesomesauce.
