namespace BrgContainer.Tests
{
    using System;
    using Runtime;
    using NUnit.Framework;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Mathematics;
    using UnityEngine.Rendering;

    public sealed unsafe class BatchInstanceDataBufferTests
    {
        private NativeArray<float4> array;
        private BatchInstanceDataBuffer buffer;
        private UnsafeHashMap<int, MetadataInfo>* metadataInfo;
        private UnsafeList<MetadataValue>* metadataValues;
        private int* instanceCountReference;
        
        [SetUp]
        public void Setup()
        {
            array = new NativeArray<float4>(100, Allocator.Persistent);

            metadataInfo = (UnsafeHashMap<int, MetadataInfo>*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<UnsafeHashMap<int, MetadataInfo>>(),
                UnsafeUtility.AlignOf<UnsafeHashMap<int, MetadataInfo>>(), Allocator.Persistent);
            *metadataInfo = new UnsafeHashMap<int, MetadataInfo>(100, Allocator.Persistent);
            
            metadataInfo->Add(1, new MetadataInfo(16, 0, 1, true));

            metadataValues = (UnsafeList<MetadataValue>*)UnsafeUtility.Malloc(
                UnsafeUtility.SizeOf<UnsafeList<MetadataValue>>(), UnsafeUtility.AlignOf<UnsafeList<MetadataValue>>(),
                Allocator.Persistent);
            *metadataValues = new UnsafeList<MetadataValue>(100, Allocator.Persistent);
            metadataValues->Add(new MetadataValue
            {
                Value = 0,
                NameID = 1
            });

            instanceCountReference = (int*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<int>(),
                UnsafeUtility.AlignOf<int>(), Allocator.Persistent);
            *instanceCountReference = 0;
   
            buffer = new BatchInstanceDataBuffer(array, metadataInfo, metadataValues, instanceCountReference, 100, 10, 400);
        }

        [TearDown]
        public void Teardown()
        {
            // free any resources here
            array.Dispose();
            metadataInfo->Dispose();
            metadataValues->Dispose();
            
            UnsafeUtility.Free(metadataInfo, Allocator.Persistent);
            UnsafeUtility.Free(metadataValues, Allocator.Persistent);
            UnsafeUtility.Free(instanceCountReference, Allocator.Persistent);
        } 

        [Test]
        public void TestInstantiation() 
        {
            Assert.AreEqual(100, buffer.Capacity);
            Assert.AreEqual(0, buffer.InstanceCount);
        }

        [Test]
        public void TestSetInstanceCount()
        {
            buffer.SetInstanceCount(10);
            Assert.AreEqual(10, buffer.InstanceCount);
        }
        
        [Test]
        public void TestSetInstanceCount_OutOfBound()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => 
            {
               buffer.SetInstanceCount(200);
            });
        }
        
        [Test]
        public void TestEqualsMethod()
        {
            var otherBuffer = new BatchInstanceDataBuffer(array, metadataInfo, metadataValues, instanceCountReference, 100, 10, 400);
            Assert.True(buffer.Equals(otherBuffer));
        }
        
        [Test]
        public void TestNotEqualsMethod()
        {
            var otherBuffer = new BatchInstanceDataBuffer(new NativeArray<float4>(50, Allocator.Persistent), metadataInfo, metadataValues, instanceCountReference, 50, 10, 200);
            Assert.False(buffer.Equals(otherBuffer));
        }
        
        [Test]
        public void TestRemove_InstanceCountDecrements()
        {
            buffer.SetInstanceCount(50);

            buffer.Remove(10, 5);

            Assert.AreEqual(45, buffer.InstanceCount);
        }

        [Test]
        public void TestRemove_RemovedInstanceNotReadable()
        {
            buffer.SetInstanceCount(1);
            buffer.WriteInstanceData<int>(0, 1, 50);

            buffer.Remove(0, 1);
  
            Assert.Throws<IndexOutOfRangeException>(() =>
            {
                buffer.ReadInstanceData<int>(0, 1);
            });
        }
        
        [Test]
        public void TestWriteAndReadInstanceData()
        {
            buffer.SetInstanceCount(1);
            buffer.WriteInstanceData<int>(0, 1, 50);

            var data = buffer.ReadInstanceData<int>(0, 1);
            Assert.AreEqual(50, data);
        }

        [Test]
        public void TestReadInstanceData_IndexOutOfRangeExc()
        {
            Assert.Throws<IndexOutOfRangeException>(() =>
            {
                var data = buffer.ReadInstanceData<int>(50, 1);
            });
        }

        [Test]
        public void TestWriteWithIndexOutOfRange_ThrowsException()
        {
            Assert.Throws<IndexOutOfRangeException>(() =>
            {
                buffer.WriteInstanceData<int>(50, 1, 50);
            });
        }
    }
}