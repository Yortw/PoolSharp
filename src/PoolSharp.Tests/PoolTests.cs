using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PoolSharp.Tests
{
	[TestClass]
	public class PoolTests
	{

		#region Constructor Tests

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public void Pool_Constructor_ThrowsOnNullPolicy()
		{
			new Pool<TestPoolItem>(null);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void Pool_Constructor_ThrowsOnNullFactoryFunc()
		{
			new Pool<TestPoolItem>(new PoolPolicy<TestPoolItem>());
		}

		#endregion

		#region Take Tests

		[TestMethod]
		public void Pool_Take_ProvidesInstance()
		{
			var pool = GetPool();
			var item = pool.Take();
			Assert.IsNotNull(item);
		}


		[TestMethod]
		public void Pool_Take_TreatsZeroMaximumSizeAsUnlimited()
		{
			var pool = GetPool(0, PooledItemInitialization.Return);
			var item = pool.Take();
			pool.Add(item);
			var item2 = pool.Take();
			Assert.AreEqual(item, item2);
		}

		[TestMethod]
		public void Pool_Take_ProvidesUniqueInstance()
		{
			var pool = GetPool();
			var item = pool.Take();
			var item2 = pool.Take();
			Assert.AreNotEqual((object)item, (object)item2);
		}

		[TestMethod]
		public void Pool_Take_ReturnsInstanceWhenFull()
		{
			var pool = GetPool();
			for (int cnt = 0; cnt < 6; cnt++)
			{
				var item = pool.Take();
				Assert.IsNotNull(item);
			}
		}

		[TestMethod]
		public void Pool_Take_ItemIsResetOnTake()
		{
			var pool = GetPool(1, PooledItemInitialization.Take);
			var item = pool.Take();
			Guid itemId = item.Id;
			pool.Add(item);

			var item2 = pool.Take();
			Assert.IsTrue((object)item == (object)item2);
			Assert.AreNotEqual(itemId, item.Id);
		}

		[ExpectedException(typeof(ObjectDisposedException))]
		[TestMethod]
		public void Pool_Take_ThrowsWhenPoolDisposed()
		{
			var pool = GetPool(1, PooledItemInitialization.Take);
			pool.Dispose();
			var item = pool.Take();
		}

		#endregion

		#region Add Tests

		[TestMethod]
		public void Pool_Add_ReturnsItemToPool()
		{
			var pool = GetPool(1, PooledItemInitialization.Return);
			var item = pool.Take();
			pool.Add(item);
			var item2 = pool.Take();
			Assert.AreEqual(item, item2);
		}

		[ExpectedException(typeof(ArgumentNullException))]
		[TestMethod]
		public void Pool_Add_ThrowsOnNullInstance()
		{
			var pool = GetPool();
			pool.Add(null);	
		}

		[TestMethod]
		public void Pool_Add_DisposesInstanceWhenPoolDisposed()
		{
			var pool = GetDisposablePool();
			var item = pool.Take();
			pool.Dispose();
			Assert.IsFalse(item.IsDisposed);
			pool.Add(item);
			Assert.IsTrue(item.IsDisposed);
		}

		[TestMethod]
		public void Pool_Add_DisposesValueOnPooledObjectWhenPoolDisposed()
		{
			var pool = GetPoolForPooledObjectWrapper();
			var item = pool.Take();
			pool.Dispose();
			Assert.IsFalse(item.Value.IsDisposed);
			pool.Add(item);
			Assert.IsTrue(item.Value.IsDisposed);
		}

		[TestMethod]
		public void Pool_Add_DisposesInstanceWhenPoolFull()
		{
			var pool = GetDisposablePool(1, PooledItemInitialization.Return);

			var item1 = pool.Take();
			var item2 = pool.Take();
			pool.Add(item1);
			pool.Add(item2);

			Assert.IsFalse(item1.IsDisposed);
			Assert.IsTrue(item2.IsDisposed);
		}

		[TestMethod]
		public void Pool_Add_DisposesValueOnPooledObjectWhenPoolFull()
		{
			var pool = GetPoolForPooledObjectWrapper();

			var item1 = pool.Take();
			var item2 = pool.Take();
			pool.Add(item1);
			pool.Add(item2);

			Assert.IsFalse(item1.Value.IsDisposed);
			Assert.IsTrue(item2.Value.IsDisposed);
		}

		[TestMethod]
		public void Pool_Add_ItemIsResetOnAdd()
		{
			var pool = GetPool(1, PooledItemInitialization.Return);
			var item = pool.Take();
			Guid itemId = item.Id;
			pool.Add(item);

			var item2 = pool.Take();
			Assert.IsTrue((object)item == (object)item2);
			Assert.AreNotEqual(itemId, item.Id);
		}

		#endregion

		#region Dispose Tests

		[TestMethod]
		public void Pool_Dispose_ExecutesWhenTypeIsNotDisposable()
		{
			var pool = GetPool();
			pool.Dispose();
		}

		[TestMethod]
		public void Pool_Dispose_AllowsRepeatCallsWhenTypeIsNotDisposable()
		{
			var pool = GetPool();
			pool.Dispose();
			pool.Dispose();
		}

		[TestMethod]
		public void Pool_Dispose_ExecutesWhenTypeIsDisposable()
		{
			var pool = GetDisposablePool();
			pool.Dispose();
		}

		[TestMethod]
		public void Pool_Dispose_AllowsRepeatCallsWhenTypeIsDisposable()
		{
			var pool = GetDisposablePool();
			pool.Dispose();
			pool.Dispose();
		}

		[TestMethod]
		public void Pool_Dispose_DisposesSubTypesWhenDisposed()
		{
			var pool = GetDisposablePool();
			var item = pool.Take();
			Assert.IsFalse(item.IsDisposed);
			pool.Add(item);
			pool.Dispose();
			Assert.IsTrue(item.IsDisposed);
		}

		[TestMethod]
		public void Pool_Dispose_DisposesItemWhenReturnedToFullPool()
		{
			var pool = GetDisposablePool(1, PooledItemInitialization.Return);
			var item = pool.Take();
			Assert.IsFalse(item.IsDisposed);
			var item2 = pool.Take();
			Assert.IsFalse(item.IsDisposed);
			pool.Add(item);
			pool.Add(item2);

			Assert.IsFalse(item.IsDisposed);
			Assert.IsTrue(item2.IsDisposed);
		}

		[TestMethod]
		public void Pool_Dispose_SetsIsDisposed()
		{
			var pool = GetDisposablePool(1, PooledItemInitialization.Return);
			Assert.IsFalse(pool.IsDisposed);
			pool.Dispose();
			Assert.IsTrue(pool.IsDisposed);
		}

		[TestMethod]
		public void Pool_Dispose_DisposesValueOnPooledObject()
		{
			var pool = GetPoolForPooledObjectWrapper();
			var item = pool.Take();
			Assert.IsFalse(item.Value.IsDisposed);
			pool.Add(item);

			pool.Dispose();
			Assert.IsTrue(item.Value.IsDisposed);
		}

		#endregion

		#region Expand Tests

		[TestMethod]
		public void Pool_Expand_OnUnlimitedPoolAllocatesNothing()
		{
			var pool = GetPool(0, PooledItemInitialization.Return);
			TestPoolItem.InstanceCount = 0;
			pool.Expand();
			Assert.AreEqual(0, TestPoolItem.InstanceCount);
		}

		[TestMethod]
		public void Pool_Expand_CreatesExpectedNumberOfInstances()
		{
			var pool = GetPool();
			TestPoolItem.InstanceCount = 0;
			pool.Expand();
			Assert.AreEqual(5, TestPoolItem.InstanceCount);
		}

		[TestMethod]
		public void Pool_ExpandByAmount_CreatesExpectedNumberOfInstances()
		{
			var pool = GetPool();
			TestPoolItem.InstanceCount = 0;
			pool.Expand(2);
			Assert.AreEqual(2, TestPoolItem.InstanceCount);
		}

		[TestMethod]
		public void Pool_ExpandByAmount_ObeysMaximumPoolSize()
		{
			var pool = GetPool();

			var item1 = pool.Take();
			var item2 = pool.Take();
			pool.Add(item1);
			pool.Add(item2);

			TestPoolItem.InstanceCount = 0;
			pool.Expand(5);
			Assert.AreEqual(3, TestPoolItem.InstanceCount);
		}

		[TestMethod]
		public void Pool_ExpandByAmount_OnUnlimitedPoolExpandsByIncrement()
		{
			var pool = GetPool(0, PooledItemInitialization.Return);

			TestPoolItem.InstanceCount = 0;
			pool.Expand(5);
			Assert.AreEqual(5, TestPoolItem.InstanceCount);
		}

		[TestMethod]
		[ExpectedException(typeof(ObjectDisposedException))]
		public void Pool_Expand_ThrowsWhenDisposed()
		{
			var pool = GetPool();
			pool.Dispose();
			pool.Expand();
		}

		[TestMethod]
		[ExpectedException(typeof(ObjectDisposedException))]
		public void Pool_ExpandByAmount_ThrowsWhenDisposed()
		{
			var pool = GetPool();
			pool.Dispose();
			pool.Expand(10);
		}

		#endregion

		#region PooledObject<T> Tests

		[TestMethod]
		public void PooledObject_Dispose_ReturnsToPool()
		{
			var pool = GetPoolForPooledObjectWrapper();

			PooledObject<DisposableTestPoolItem> itemRef = null;
			Guid itemId = Guid.Empty;
			using (var item = pool.Take())
			{
				itemRef = item;
				itemId = item.Value.Id;
			}
			var item2 = pool.Take();

			Assert.AreEqual((object)item2.Value, (object)itemRef.Value);
			Assert.AreNotEqual(item2.Value.Id, itemId);
		}

		#endregion

		#region Private Methods

		private IPool<TestPoolItem> GetPool()
		{
			return GetPool(5, PooledItemInitialization.Return);
		}

		private IPool<TestPoolItem> GetPool(int maxSize, PooledItemInitialization reinitialisePolicy)
		{
			var policy = new PoolPolicy<TestPoolItem>()
			{
				Factory = (pool) => new TestPoolItem(),
				InitializationPolicy = reinitialisePolicy,
				MaximumPoolSize = maxSize,
				ReinitializeObject = (pi) =>
				{
					pi.Id = Guid.NewGuid();
					pi.Date = DateTime.Now;
				}
			};

			return new Pool<TestPoolItem>(policy);
		}

		private IPool<DisposableTestPoolItem> GetDisposablePool()
		{
			return GetDisposablePool(5, PooledItemInitialization.Return);
		}

		private IPool<DisposableTestPoolItem> GetDisposablePool(int maxSize, PooledItemInitialization reinitialisePolicy)
		{
			var policy = new PoolPolicy<DisposableTestPoolItem>()
			{
				Factory = (pool) => new DisposableTestPoolItem(),
				InitializationPolicy = reinitialisePolicy,
				MaximumPoolSize = maxSize,
				ReinitializeObject = (pi) =>
				{
					pi.Id = Guid.NewGuid();
					pi.Date = DateTime.Now;
				}
			};

			return new Pool<DisposableTestPoolItem>(policy);
		}

		private static Pool<PooledObject<DisposableTestPoolItem>> GetPoolForPooledObjectWrapper()
		{
			var policy = new PoolPolicy<PooledObject<DisposableTestPoolItem>>()
			{
				Factory = (p) => new PooledObject<DisposableTestPoolItem>(p, new DisposableTestPoolItem()),
				InitializationPolicy = PooledItemInitialization.Return,
				MaximumPoolSize = 1,
				ReinitializeObject = (item) =>
				{
					item.Value.Id = Guid.NewGuid();
					item.Value.Date = DateTime.Now;
				}
			};

			var pool = new Pool<PooledObject<DisposableTestPoolItem>>(policy);
			return pool;
		}

		#endregion

	}
}