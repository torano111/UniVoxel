﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using System.Threading;
using Unity.Jobs.LowLevel.Unsafe;

namespace UniVoxel.Utility
{
    /// <summary>
    /// Counter for Jobs. Use NativeCacheCounter.Concurrent for parallel jobs.
    /// NativeCacheCounter may be better in point of performance, but this can safely return the value after incrementing in a parallel job.
    /// https://docs.unity3d.com/Packages/com.unity.jobs@0.2/manual/custom_job_types.html
    /// </summary>
    // Mark this struct as a NativeContainer, usually this would be a generic struct for containers, but a counter does not need to be generic
    // TODO - why does a counter not need to be generic? - explain the argument for this reasoning please.
    [StructLayout(LayoutKind.Sequential)]
    [NativeContainer]
    unsafe public struct NativeCounter
    {
        // The actual pointer to the allocated count needs to have restrictions relaxed so jobs can be schedled with this container
        [NativeDisableUnsafePtrRestriction]
        int* m_Counter;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        AtomicSafetyHandle m_Safety;
        // The dispose sentinel tracks memory leaks. It is a managed type so it is cleared to null when scheduling a job
        // The job cannot dispose the container, and no one else can dispose it until the job has run, so it is ok to not pass it along
        // This attribute is required, without it this NativeContainer cannot be passed to a job; since that would give the job access to a managed object
        [NativeSetClassTypeToNullOnSchedule]
        DisposeSentinel m_DisposeSentinel;
#endif

        // Keep track of where the memory for this was allocated
        Allocator m_AllocatorLabel;

        public NativeCounter(Allocator allocator)
        {
            // This check is redundant since we always use an int that is blittable.
            // It is here as an example of how to check for type correctness for generic types.
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (!UnsafeUtility.IsBlittable<int>())
                throw new ArgumentException(string.Format("{0} used in NativeQueue<{0}> must be blittable", typeof(int)));
#endif
            m_AllocatorLabel = allocator;

            // Allocate native memory for a single integer
            m_Counter = (int*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<int>(), 4, allocator);

            // Create a dispose sentinel to track memory leaks. This also creates the AtomicSafetyHandle
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, 0, allocator);
#endif
            // Initialize the count to 0 to avoid uninitialized data
            Count = 0;
        }

        public int Increment()
        {
            // Verify that the caller has write permission on this data. 
            // This is the race condition protection, without these checks the AtomicSafetyHandle is useless
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
            return (*m_Counter)++;
        }

        public int Count
        {
            get
            {
                // Verify that the caller has read permission on this data. 
                // This is the race condition protection, without these checks the AtomicSafetyHandle is useless
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                return *m_Counter;
            }
            set
            {
                // Verify that the caller has write permission on this data. This is the race condition protection, without these checks the AtomicSafetyHandle is useless
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
                *m_Counter = value;
            }
        }

        public bool IsCreated
        {
            get { return m_Counter != null; }
        }

        public void Dispose()
        {
            // Let the dispose sentinel know that the data has been freed so it does not report any memory leaks
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);
#endif

            UnsafeUtility.Free(m_Counter, m_AllocatorLabel);
            m_Counter = null;
        }


        [NativeContainer]
        // This attribute is what makes it possible to use NativeCounter.Concurrent in a ParallelFor job
        [NativeContainerIsAtomicWriteOnly]
        unsafe public struct Concurrent
        {
            // Copy of the pointer from the full NativeCounter
            [NativeDisableUnsafePtrRestriction]
            int* m_Counter;

            // Copy of the AtomicSafetyHandle from the full NativeCounter. The dispose sentinel is not copied since this inner struct does not own the memory and is not responsible for freeing it.
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle m_Safety;
#endif

            // This is what makes it possible to assign to NativeCounter.Concurrent from NativeCounter
            public static implicit operator NativeCounter.Concurrent(NativeCounter cnt)
            {
                NativeCounter.Concurrent concurrent;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndThrow(cnt.m_Safety);
                concurrent.m_Safety = cnt.m_Safety;
                AtomicSafetyHandle.UseSecondaryVersion(ref concurrent.m_Safety);
#endif

                concurrent.m_Counter = cnt.m_Counter;
                return concurrent;
            }

            public int Increment()
            {
                // Increment still needs to check for write permissions
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
                // The actual increment is implemented with an atomic, since it can be incremented by multiple threads at the same time
                return Interlocked.Increment(ref *m_Counter);
            }
        }
    }

    /// <summary>
    /// Counter for Jobs. Use NativeCacheCounter.Concurrent for parallel jobs.
    /// The performance is better than NativeCounter, but Increment method cannot return the value.
    /// https://docs.unity3d.com/Packages/com.unity.jobs@0.2/manual/custom_job_types.html
    /// </summary>
    // Mark this struct as a NativeContainer, usually this would be a generic struct for containers, but a counter does not need to be generic
    // TODO - why does a counter not need to be generic? - explain the argument for this reasoning please.
    [StructLayout(LayoutKind.Sequential)]
    [NativeContainer]
    unsafe public struct NativeCacheCounter
    {
        // The actual pointer to the allocated count needs to have restrictions relaxed so jobs can be schedled with this container
        [NativeDisableUnsafePtrRestriction]
        int* m_Counter;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        AtomicSafetyHandle m_Safety;
        // The dispose sentinel tracks memory leaks. It is a managed type so it is cleared to null when scheduling a job
        // The job cannot dispose the container, and no one else can dispose it until the job has run, so it is ok to not pass it along
        // This attribute is required, without it this NativeContainer cannot be passed to a job; since that would give the job access to a managed object
        [NativeSetClassTypeToNullOnSchedule]
        DisposeSentinel m_DisposeSentinel;
#endif

        // Keep track of where the memory for this was allocated
        Allocator m_AllocatorLabel;

        public const int IntsPerCacheLine = JobsUtility.CacheLineSize / sizeof(int);

        public NativeCacheCounter(Allocator allocator)
        {
            // This check is redundant since we always use an int that is blittable.
            // It is here as an example of how to check for type correctness for generic types.
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (!UnsafeUtility.IsBlittable<int>())
                throw new ArgumentException(string.Format("{0} used in NativeQueue<{0}> must be blittable", typeof(int)));
#endif
            m_AllocatorLabel = allocator;

            // One full cache line (integers per cacheline * size of integer) for each potential worker index, JobsUtility.MaxJobThreadCount
            m_Counter = (int*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<int>() * IntsPerCacheLine * JobsUtility.MaxJobThreadCount, 4, allocator);

            // Create a dispose sentinel to track memory leaks. This also creates the AtomicSafetyHandle
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, 0, allocator);
#endif
            // Initialize the count to 0 to avoid uninitialized data
            Count = 0;
        }

        public void Increment()
        {
            // Verify that the caller has write permission on this data. 
            // This is the race condition protection, without these checks the AtomicSafetyHandle is useless
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
            (*m_Counter)++;
        }

        public int Count
        {
            get
            {
                // Verify that the caller has read permission on this data. 
                // This is the race condition protection, without these checks the AtomicSafetyHandle is useless
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                int count = 0;
                for (int i = 0; i < JobsUtility.MaxJobThreadCount; ++i)
                    count += m_Counter[IntsPerCacheLine * i];
                return count;
            }
            set
            {
                // Verify that the caller has write permission on this data. This is the race condition protection, without these checks the AtomicSafetyHandle is useless
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
                // Clear all locally cached counts, 
                // set the first one to the required value
                for (int i = 1; i < JobsUtility.MaxJobThreadCount; ++i)
                    m_Counter[IntsPerCacheLine * i] = 0;
                *m_Counter = value;
            }
        }

        public bool IsCreated
        {
            get { return m_Counter != null; }
        }

        public void Dispose()
        {
            // Let the dispose sentinel know that the data has been freed so it does not report any memory leaks
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);
#endif

            UnsafeUtility.Free(m_Counter, m_AllocatorLabel);
            m_Counter = null;
        }

        [NativeContainer]
        [NativeContainerIsAtomicWriteOnly]
        // Let the job system know that it should inject the current worker index into this container
        unsafe public struct Concurrent
        {
            [NativeDisableUnsafePtrRestriction]
            int* m_Counter;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle m_Safety;
#endif

            // The current worker thread index; it must use this exact name since it is injected
            [NativeSetThreadIndex]
            int m_ThreadIndex;

            public static implicit operator NativeCacheCounter.Concurrent(NativeCacheCounter cnt)
            {
                NativeCacheCounter.Concurrent concurrent;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndThrow(cnt.m_Safety);
                concurrent.m_Safety = cnt.m_Safety;
                AtomicSafetyHandle.UseSecondaryVersion(ref concurrent.m_Safety);
#endif

                concurrent.m_Counter = cnt.m_Counter;
                concurrent.m_ThreadIndex = 0;
                return concurrent;
            }

            public void Increment()
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
                // No need for atomics any more since we are just incrementing the local count
                ++m_Counter[IntsPerCacheLine * m_ThreadIndex];
            }
        }
    }
}