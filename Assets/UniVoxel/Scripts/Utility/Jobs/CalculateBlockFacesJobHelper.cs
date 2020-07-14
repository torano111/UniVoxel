using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniVoxel.Core;

namespace UniVoxel.Utility
{
    public struct SolidBlockData
    {
        public int BlockIndex;
        public int SolidFaceMask;

        // how many quad faces?
        public int SolidFaceCount;

        // how many solid quad faces before this data?
        public int SolidFaceCountBefore;
    }

    public static class CalculateBlockFacesJobHelper
    {
        public readonly static int StartFaceSideIndex = 0;
        public readonly static int EndFaceSideIndex = 5;

        public static int GetFaceSideIndex(BoxFaceSide side)
        {
            switch (side)
            {
                case BoxFaceSide.Front:
                    return 0;
                case BoxFaceSide.Back:
                    return 1;
                case BoxFaceSide.Top:
                    return 2;
                case BoxFaceSide.Bottom:
                    return 3;
                case BoxFaceSide.Right:
                    return 4;
                case BoxFaceSide.Left:
                    return 5;
                default:
                    return -1;
            }
        }

        public static BoxFaceSide GetBoxFaceSide(int faceSideIndex)
        {
            switch (faceSideIndex)
            {
                case 0:
                    return BoxFaceSide.Front;
                case 1:
                    return BoxFaceSide.Back;
                case 2:
                    return BoxFaceSide.Top;
                case 3:
                    return BoxFaceSide.Bottom;
                case 4:
                    return BoxFaceSide.Right;
                case 5:
                default:
                    return BoxFaceSide.Left;
            }
        }
    }
}
