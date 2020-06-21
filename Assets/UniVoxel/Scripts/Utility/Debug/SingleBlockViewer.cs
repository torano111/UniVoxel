using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniVoxel.AssetData;
using UniVoxel.Core;

namespace UniVoxel.Utility
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class SingleBlockViewer : MonoBehaviour
    {
        MeshRenderer _meshRenderer;
        MeshFilter _meshFilter;

        Mesh _mesh;

        Vector3[] _vertices = new Vector3[0];
        int[] _triangles = new int[0];
        Vector2[] _uv = new Vector2[0];
        Vector3[] _normals = new Vector3[0];
        Vector4[] _tangents = new Vector4[0];

        public Vector3 Center { get; set; }
        public float Extent { get; set; }

        [SerializeField]
        BlockType _currentBlockType;

        [SerializeField]
        BlockDataScriptableObject _blockDataObject;

        [SerializeField]
        Vector2 _singleTextureLengths = new Vector2(16f, 16f);

        [SerializeField]
        Vector2 _textureAtlasLengths = new Vector2(256f, 256f);

        public Vector2 GetUVCoord00(BoxFaceSide side)
        {
            if (_blockDataObject.TryGetBlockData(_currentBlockType, out var data))
            {
                var texAtlasPos = data.GetTexAtlasPosition(side);
                return BlockUtility.GetUV00FromTextureAtlas(texAtlasPos, _singleTextureLengths, _textureAtlasLengths);
            }

            throw new System.InvalidOperationException();
        }

        public Vector2 GetUVCoord11(BoxFaceSide side)
        {
            if (_blockDataObject.TryGetBlockData(_currentBlockType, out var data))
            {
                var texAtlasPos = data.GetTexAtlasPosition(side);
                return BlockUtility.GetUV11FromTextureAtlas(texAtlasPos, _singleTextureLengths, _textureAtlasLengths);
            }

            throw new System.InvalidOperationException();
        }

        [SerializeField]
        Material _material;

        [SerializeField]
        bool _showFrontFace = true;


        [SerializeField]
        bool _showBackFace = true;


        [SerializeField]
        bool _showRightFace = true;


        [SerializeField]
        bool _showLeftFace = true;


        [SerializeField]
        bool _showTopFace = true;


        [SerializeField]
        bool _showBottomFace = true;

        public bool GetShowFace(BoxFaceSide side)
        {
            switch (side)
            {
                case BoxFaceSide.Front:
                    return _showFrontFace;
                case BoxFaceSide.Back:
                    return _showBackFace;
                case BoxFaceSide.Right:
                    return _showRightFace;
                case BoxFaceSide.Left:
                    return _showLeftFace;
                case BoxFaceSide.Top:
                    return _showTopFace;
                case BoxFaceSide.Bottom:
                    return _showBottomFace;
                default:
                    throw new System.ArgumentException();
            }
        }

        public void SetShowFace(BoxFaceSide side, bool show)
        {
            switch (side)
            {
                case BoxFaceSide.Front:
                    _showFrontFace = show;
                    break;
                case BoxFaceSide.Back:
                    _showBackFace = show;
                    break;
                case BoxFaceSide.Right:
                    _showRightFace = show;
                    break;
                case BoxFaceSide.Left:
                    _showLeftFace = show;
                    break;
                case BoxFaceSide.Top:
                    _showTopFace = show;
                    break;
                case BoxFaceSide.Bottom:
                    _showBottomFace = show;
                    break;
                default:
                    throw new System.ArgumentException();
            }
        }

        public void SetAllShowFace(bool show)
        {
            foreach (BoxFaceSide side in System.Enum.GetValues(typeof(BoxFaceSide)))
            {
                SetShowFace(side, show);
            }
        }

        void Awake()
        {
            _meshRenderer = GetComponent<MeshRenderer>();
            _meshFilter = GetComponent<MeshFilter>();

            _meshRenderer.material = this._material;

            _mesh = new Mesh();
            _mesh.MarkDynamic();
            _meshFilter.mesh = this._mesh;


            Center = Vector3.zero;
            Extent = 0.5f;
        }

        void Start()
        {
            CreateBlock();
        }

        public void CreateBlock()
        {
            ClearMeshProperties();

            var numFaces = 0;
            foreach (BoxFaceSide side in System.Enum.GetValues(typeof(BoxFaceSide)))
            {
                if (GetShowFace(side))
                {
                    numFaces++;
                }
            }

            VoxelUtility.ReserveMeshForFaces(numFaces, ref _vertices, ref _triangles, ref _uv, ref _normals, ref _tangents);

            var vertexStartIndex = 0;
            var triangleStartIndex = 0;
            foreach (BoxFaceSide side in System.Enum.GetValues(typeof(BoxFaceSide)))
            {
                if (GetShowFace(side))
                {
                    VoxelUtility.AddMeshForBoxFace(side, Center, Extent, this._vertices, this._triangles, this._uv, GetUVCoord00(side), GetUVCoord11(side), this._normals, this._tangents, vertexStartIndex, triangleStartIndex);
                    vertexStartIndex += VoxelUtility.GetFaceVertexLength();
                    triangleStartIndex += VoxelUtility.GetFaceTriangleLength();
                }
            }
            UpdateMesh();
        }

        void UpdateMesh()
        {
            _mesh.Clear();
            _mesh.vertices = this._vertices;
            _mesh.uv = this._uv;
            _mesh.normals = this._normals;
            _mesh.tangents = this._tangents;
            _mesh.triangles = this._triangles;

            _mesh.RecalculateBounds();
        }

        void ClearMeshProperties()
        {
            _vertices = new Vector3[0];
            _triangles = new int[0];
            _uv = new Vector2[0];
            _normals = new Vector3[0];
            _tangents = new Vector4[0];
        }
    }
}
