using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace UniVoxel.Tests
{
    public class NativeListTester : MonoBehaviour
    {
        [BurstCompile]
        public struct EnqueueTestParallelJob : IJobParallelFor
        {
            public NativeQueue<int>.ParallelWriter Queue;

            public void Execute(int index)
            {
                Queue.Enqueue(index);
            }
        }

        [BurstCompile]
        public struct SetListLengthJob : IJob
        {
            public NativeQueue<int> Queue;
            public NativeList<int> List;

            public void Execute()
            {
                var count = 0;
                while (Queue.TryDequeue(out var item))
                {
                    count++;
                }

                List.Length = count;
            }
        }

        [BurstCompile]
        public struct GetResultParallelJob : IJobParallelForDefer
        {
            [WriteOnly]
            public NativeArray<int> DeferedList;

            public void Execute(int index)
            {
                DeferedList[index] = index;
            }
        }


        // Start is called before the first frame update
        void Start()
        {
            var iteration = 10;

            var queue = new NativeQueue<int>(Allocator.TempJob);
            var list = new NativeList<int>(Allocator.TempJob);

            var enqueueJob = new EnqueueTestParallelJob()
            {
                Queue = queue.AsParallelWriter(),
            };

            var dep = enqueueJob.Schedule(iteration, 0);

            var setListLengthJob = new SetListLengthJob()
            {
                Queue = queue,
                List = list,
            };

            dep = setListLengthJob.Schedule(dep);

            var getResultJob = new GetResultParallelJob()
            {
                DeferedList = list.AsDeferredJobArray(),
            };

            dep = getResultJob.Schedule(list, 0, dep);

            dep.Complete();

            var resultInfo = "<NativeListTester>\n";

            for (var i = 0; i < list.Length; i++)
            {
                var output = list[i];
                resultInfo += $"{output}";

                if (i < list.Length - 1)
                {
                    resultInfo += ", ";
                }
            }

            Debug.Log(resultInfo);

            queue.Dispose();
            list.Dispose();
        }
    }
}
