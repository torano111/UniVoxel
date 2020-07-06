using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniVoxel.Utility;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;

namespace UniVoxel.Tests
{
    public struct CountZerosWithNativeCounter : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<int> input;
        public NativeCounter.Concurrent counter;
        public void Execute(int i)
        {
            if (input[i] == 0)
            {
                counter.Increment();
            }
        }
    }
    public struct CountZerosWithNativeCacheCounter : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<int> input;
        public NativeCacheCounter.Concurrent counter;
        public void Execute(int i)
        {
            if (input[i] == 0)
            {
                counter.Increment();
            }
        }
    }

    public class NativeCounterTester : MonoBehaviour
    {
        [SerializeField]
        int[] _input;

        [SerializeField]
        bool _check = false;

        // Update is called once per frame
        void Update()
        {
            if (_check)
            {
                TestNativeCounter();
                TestNativeCacheCounter();
                CheckZeros();

                _check = false;
            }
        }

        void CheckZeros()
        {
            if (_input == null || _input.Length == 0)
            {
                return;
            }

            Debug.Log("-----Correct Answer-----");

            var count = 0;

            for (var i = 0; i < _input.Length; i++)
            {
                if (_input[i] == 0)
                {
                    count++;
                }
            }

            Debug.Log("The array countains " + count + " zeros");
            
            
            Debug.Log("----- -----");
        }

        void TestNativeCounter()
        {
            if (_input == null || _input.Length == 0)
            {
                return;
            }

            Debug.Log("-----Test NativeCounter-----");

            var counter = new NativeCounter(Allocator.Temp);
            counter.Count = 0;

            var jobData = new CountZerosWithNativeCounter()
            {
                input = new NativeArray<int>(_input, Allocator.TempJob),
                counter = counter,
            };

            var handle = jobData.Schedule(_input.Length, 8);
            handle.Complete();

            Debug.Log("The array countains " + counter.Count + " zeros");
            counter.Dispose();
            jobData.input.Dispose();
            
            Debug.Log("----- -----");
        }

        void TestNativeCacheCounter()
        {
            if (_input == null || _input.Length == 0)
            {
                return;
            }
            
            Debug.Log("-----Test NativeCacheCounter-----");

            var counter = new NativeCacheCounter(Allocator.Temp);
            counter.Count = 0;

            var jobData = new CountZerosWithNativeCacheCounter()
            {
                input = new NativeArray<int>(_input, Allocator.TempJob),
                counter = counter,
            };

            var handle = jobData.Schedule(_input.Length, 8);
            handle.Complete();

            Debug.Log("The array countains " + counter.Count + " zeros");
            counter.Dispose();
            jobData.input.Dispose();
            
            Debug.Log("----- -----");
        }
    }
}
