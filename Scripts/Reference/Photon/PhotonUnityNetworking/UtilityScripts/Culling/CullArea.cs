// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CullArea.cs" company="Exit Games GmbH">
//   Part of: Photon Unity Utilities,
// </copyright>
// <summary>
//  表示用于网络剔除的剔除区域。
// </summary>
// <author>developer@exitgames.com</author>
// --------------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;

namespace Photon.Pun.UtilityScripts
{
    /// <summary>
    ///     表示用于网络剔除的剔除区域。
    /// </summary>
    public class CullArea : MonoBehaviour
    {
        private const int MAX_NUMBER_OF_ALLOWED_CELLS = 250;

        public const int MAX_NUMBER_OF_SUBDIVISIONS = 3;

        /// <summary>
        ///     这表示分配给第一个创建的单元格的第一个ID。
        ///     如果你已经有一些兴趣组占用了这个第一个ID，可以随意更改它。
        ///     但是增加第一个组ID会减少允许的最大单元格数量。
        ///     允许的值范围为1到250。
        /// </summary>
        public readonly byte FIRST_GROUP_ID = 1;

        /// <summary>
        ///     这表示更新发送的顺序。
        ///     数字表示单元格层次结构的细分级别：
        ///     - 0: 消息发送给所有玩家
        ///     - 1: 消息发送给对第一级细分的匹配单元格感兴趣的玩家
        ///     如果只有一个细分级别，我们先向所有玩家发送一次更新，
        ///     然后再向处于相同单元格或对当前单元格更新感兴趣的玩家发送三次后续更新。
        /// </summary>
        public readonly int[] SUBDIVISION_FIRST_LEVEL_ORDER = new int[4] { 0, 1, 1, 1 };

        /// <summary>
        ///     这表示更新发送的顺序。
        ///     数字表示单元格层次结构的细分级别：
        ///     - 0: 消息发送给所有玩家
        ///     - 1: 消息发送给对第一级细分的匹配单元格感兴趣的玩家
        ///     - 2: 消息发送给对第二级细分的匹配单元格感兴趣的玩家
        ///     如果有两个细分级别，我们每隔一次更新才发送给
        ///     处于相同单元格或对当前单元格更新感兴趣的玩家。
        /// </summary>
        public readonly int[] SUBDIVISION_SECOND_LEVEL_ORDER = new int[8] { 0, 2, 1, 2, 0, 2, 1, 2 };

        /// <summary>
        ///     这表示更新发送的顺序。
        ///     数字表示单元格层次结构的细分级别：
        ///     - 0: 消息发送给所有玩家
        ///     - 1: 消息发送给对第一级细分的匹配单元格感兴趣的玩家
        ///     - 2: 消息发送给对第二级细分的匹配单元格感兴趣的玩家
        ///     - 3: 消息发送给对第三级细分的匹配单元格感兴趣的玩家
        ///     如果有三个细分级别，我们每隔一次更新才发送给
        ///     处于相同单元格或对当前单元格更新感兴趣的玩家。
        /// </summary>
        public readonly int[] SUBDIVISION_THIRD_LEVEL_ORDER = new int[12] { 0, 3, 2, 3, 1, 3, 2, 3, 1, 3, 2, 3 };

        public Vector2 Center;
        public Vector2 Size = new Vector2(25.0f, 25.0f);

        public Vector2[] Subdivisions = new Vector2[MAX_NUMBER_OF_SUBDIVISIONS];

        public int NumberOfSubdivisions;

        public int CellCount { get; private set; }

        public CellTree CellTree { get; private set; }

        public Dictionary<int, GameObject> Map { get; private set; }

        public bool YIsUpAxis = false;
        public bool RecreateCellHierarchy = false;

        private byte idCounter;

        /// <summary>
        ///     在运行时创建单元格层次结构。
        /// </summary>
        public void Awake()
        {
            this.idCounter = this.FIRST_GROUP_ID;

            this.CreateCellHierarchy();
        }

        /// <summary>
        ///     在编辑器中创建单元格层次结构并绘制单元格视图。
        /// </summary>
        public void OnDrawGizmos()
        {
            this.idCounter = this.FIRST_GROUP_ID;

            if (this.RecreateCellHierarchy)
            {
                this.CreateCellHierarchy();
            }

            this.DrawCells();
        }

        /// <summary>
        ///     创建单元格层次结构。
        /// </summary>
        private void CreateCellHierarchy()
        {
            if (!this.IsCellCountAllowed())
            {
                if (Debug.isDebugBuild)
                {
                    Debug.LogError("There are too many cells created by your subdivision options. Maximum allowed number of cells is " + (MAX_NUMBER_OF_ALLOWED_CELLS - this.FIRST_GROUP_ID) +
                                   ". Current number of cells is " + this.CellCount + ".");
                    return;
                }
                else
                {
                    Application.Quit();
                }
            }

            CellTreeNode rootNode = new CellTreeNode(this.idCounter++, CellTreeNode.ENodeType.Root, null);

            if (this.YIsUpAxis)
            {
                this.Center = new Vector2(transform.position.x, transform.position.y);
                this.Size = new Vector2(transform.localScale.x, transform.localScale.y);

                rootNode.Center = new Vector3(this.Center.x, this.Center.y, 0.0f);
                rootNode.Size = new Vector3(this.Size.x, this.Size.y, 0.0f);
                rootNode.TopLeft = new Vector3((this.Center.x - (this.Size.x / 2.0f)), (this.Center.y - (this.Size.y / 2.0f)), 0.0f);
                rootNode.BottomRight = new Vector3((this.Center.x + (this.Size.x / 2.0f)), (this.Center.y + (this.Size.y / 2.0f)), 0.0f);
            }
            else
            {
                this.Center = new Vector2(transform.position.x, transform.position.z);
                this.Size = new Vector2(transform.localScale.x, transform.localScale.z);

                rootNode.Center = new Vector3(this.Center.x, 0.0f, this.Center.y);
                rootNode.Size = new Vector3(this.Size.x, 0.0f, this.Size.y);
                rootNode.TopLeft = new Vector3((this.Center.x - (this.Size.x / 2.0f)), 0.0f, (this.Center.y - (this.Size.y / 2.0f)));
                rootNode.BottomRight = new Vector3((this.Center.x + (this.Size.x / 2.0f)), 0.0f, (this.Center.y + (this.Size.y / 2.0f)));
            }

            this.CreateChildCells(rootNode, 1);

            this.CellTree = new CellTree(rootNode);

            this.RecreateCellHierarchy = false;
        }

        /// <summary>
        ///     创建所有子单元格。
        /// </summary>
        /// <param name="parent">当前的父节点。</param>
        /// <param name="cellLevelInHierarchy">当前层次结构中的单元格级别。</param>
        private void CreateChildCells(CellTreeNode parent, int cellLevelInHierarchy)
        {
            if (cellLevelInHierarchy > this.NumberOfSubdivisions)
            {
                return;
            }

            int rowCount = (int)this.Subdivisions[(cellLevelInHierarchy - 1)].x;
            int columnCount = (int)this.Subdivisions[(cellLevelInHierarchy - 1)].y;

            float startX = parent.Center.x - (parent.Size.x / 2.0f);
            float width = parent.Size.x / rowCount;

            for (int row = 0; row < rowCount; ++row)
            {
                for (int column = 0; column < columnCount; ++column)
                {
                    float xPos = startX + (row * width) + (width / 2.0f);

                    CellTreeNode node = new CellTreeNode(this.idCounter++, (this.NumberOfSubdivisions == cellLevelInHierarchy) ? CellTreeNode.ENodeType.Leaf : CellTreeNode.ENodeType.Node, parent);

                    if (this.YIsUpAxis)
                    {
                        float startY = parent.Center.y - (parent.Size.y / 2.0f);
                        float height = parent.Size.y / columnCount;
                        float yPos = startY + (column * height) + (height / 2.0f);

                        node.Center = new Vector3(xPos, yPos, 0.0f);
                        node.Size = new Vector3(width, height, 0.0f);
                        node.TopLeft = new Vector3(xPos - (width / 2.0f), yPos - (height / 2.0f), 0.0f);
                        node.BottomRight = new Vector3(xPos + (width / 2.0f), yPos + (height / 2.0f), 0.0f);
                    }
                    else
                    {
                        float startZ = parent.Center.z - (parent.Size.z / 2.0f);
                        float depth = parent.Size.z / columnCount;
                        float zPos = startZ + (column * depth) + (depth / 2.0f);

                        node.Center = new Vector3(xPos, 0.0f, zPos);
                        node.Size = new Vector3(width, 0.0f, depth);
                        node.TopLeft = new Vector3(xPos - (width / 2.0f), 0.0f, zPos - (depth / 2.0f));
                        node.BottomRight = new Vector3(xPos + (width / 2.0f), 0.0f, zPos + (depth / 2.0f));
                    }

                    parent.AddChild(node);

                    this.CreateChildCells(node, (cellLevelInHierarchy + 1));
                }
            }
        }

        /// <summary>
        ///     绘制单元格。
        /// </summary>
        private void DrawCells()
        {
            if ((this.CellTree != null) && (this.CellTree.RootNode != null))
            {
                this.CellTree.RootNode.Draw();
            }
            else
            {
                this.RecreateCellHierarchy = true;
            }
        }

        /// <summary>
        ///     检查单元格数量是否在允许范围内。
        /// </summary>
        /// <returns>如果单元格数量在允许范围内则返回True，如果单元格数量过大则返回false。</returns>
        private bool IsCellCountAllowed()
        {
            int horizontalCells = 1;
            int verticalCells = 1;

            foreach (Vector2 v in this.Subdivisions)
            {
                horizontalCells *= (int)v.x;
                verticalCells *= (int)v.y;
            }

            this.CellCount = horizontalCells * verticalCells;

            return (this.CellCount <= (MAX_NUMBER_OF_ALLOWED_CELLS - this.FIRST_GROUP_ID));
        }

        /// <summary>
        ///     获取玩家当前所在或附近的所有单元格ID的列表。
        /// </summary>
        /// <param name="position">玩家的当前位置。</param>
        /// <returns>包含玩家当前所在或附近的所有单元格ID的列表。</returns>
        public List<byte> GetActiveCells(Vector3 position)
        {
            List<byte> activeCells = new List<byte>(0);
            this.CellTree.RootNode.GetActiveCells(activeCells, this.YIsUpAxis, position);

            // 对"附近"的单元格进行排序是有意义的。这些在列表中位于点所在的细分级别之后的位置。2个细分级别导致点处于3个区域中。
            int cellsActive = this.NumberOfSubdivisions + 1;
            int cellsNearby = activeCells.Count - cellsActive;
            if (cellsNearby > 0)
            {
                activeCells.Sort(cellsActive, cellsNearby, new ByteComparer());
            }
            return activeCells;
        }
    }

    /// <summary>
    ///     表示可从其根节点访问的树结构。
    /// </summary>
    public class CellTree
    {
        /// <summary>
        ///     表示单元格树的根节点。
        /// </summary>
        public CellTreeNode RootNode { get; private set; }

        /// <summary>
        ///     默认构造函数。
        /// </summary>
        public CellTree()
        {
        }

        /// <summary>
        ///     用于定义根节点的构造函数。
        /// </summary>
        /// <param name="root">树的根节点。</param>
        public CellTree(CellTreeNode root)
        {
            this.RootNode = root;
        }
    }

    /// <summary>
    ///     表示树的单个节点。
    /// </summary>
    public class CellTreeNode
    {
        public enum ENodeType : byte
        {
            Root = 0,
            Node = 1,
            Leaf = 2
        }

        /// <summary>
        ///     表示单元格的唯一ID。
        /// </summary>
        public byte Id;

        /// <summary>
        ///     表示单元格的中心、左上或右下位置，或单元格的大小。
        /// </summary>
        public Vector3 Center, Size, TopLeft, BottomRight;

        /// <summary>
        ///     描述单元格树节点的当前节点类型。
        /// </summary>
        public ENodeType NodeType;

        /// <summary>
        ///     对父节点的引用。
        /// </summary>
        public CellTreeNode Parent;

        /// <summary>
        ///     包含所有子节点的列表。
        /// </summary>
        public List<CellTreeNode> Childs;

        /// <summary>
        ///     玩家与单元格中心的最大距离，用于判断是否"附近"。
        ///     这在运行时计算一次。
        /// </summary>
        private float maxDistance;

        /// <summary>
        ///     默认构造函数。
        /// </summary>
        public CellTreeNode()
        {
        }

        /// <summary>
        ///     用于定义ID和节点类型以及设置父节点的构造函数。
        /// </summary>
        /// <param name="id">单元格的ID，用作兴趣组。</param>
        /// <param name="nodeType">单元格树节点的节点类型。</param>
        /// <param name="parent">单元格树节点的父节点。</param>
        public CellTreeNode(byte id, ENodeType nodeType, CellTreeNode parent)
        {
            this.Id = id;

            this.NodeType = nodeType;

            this.Parent = parent;
        }

        /// <summary>
        ///     将给定的子节点添加到当前节点。
        /// </summary>
        /// <param name="child">要添加到节点的子节点。</param>
        public void AddChild(CellTreeNode child)
        {
            if (this.Childs == null)
            {
                this.Childs = new List<CellTreeNode>(1);
            }

            this.Childs.Add(child);
        }

        /// <summary>
        ///     在编辑器中绘制单元格。
        /// </summary>
        public void Draw()
        {
#if UNITY_EDITOR
            if (this.Childs != null)
            {
                foreach (CellTreeNode node in this.Childs)
                {
                    node.Draw();
                }
            }

            Gizmos.color = new Color((this.NodeType == ENodeType.Root) ? 1 : 0, (this.NodeType == ENodeType.Node) ? 1 : 0, (this.NodeType == ENodeType.Leaf) ? 1 : 0);
            Gizmos.DrawWireCube(this.Center, this.Size);

            byte offset = (byte)this.NodeType;
            GUIStyle gs = new GUIStyle() { fontStyle = FontStyle.Bold };
            gs.normal.textColor = Gizmos.color;
            UnityEditor.Handles.Label(this.Center + (Vector3.forward * offset * 1f), this.Id.ToString(), gs);
#endif
        }

        /// <summary>
        ///     收集玩家当前所在或附近的所有单元格ID。
        /// </summary>
        /// <param name="activeCells">用于添加玩家当前所在或附近的所有单元格ID的列表。</param>
        /// <param name="yIsUpAxis">描述是否使用y轴作为上轴。</param>
        /// <param name="position">玩家的当前位置。</param>
        public void GetActiveCells(List<byte> activeCells, bool yIsUpAxis, Vector3 position)
        {
            if (this.NodeType != ENodeType.Leaf)
            {
                foreach (CellTreeNode node in this.Childs)
                {
                    node.GetActiveCells(activeCells, yIsUpAxis, position);
                }
            }
            else
            {
                if (this.IsPointNearCell(yIsUpAxis, position))
                {
                    if (this.IsPointInsideCell(yIsUpAxis, position))
                    {
                        activeCells.Insert(0, this.Id);

                        CellTreeNode p = this.Parent;
                        while (p != null)
                        {
                            activeCells.Insert(0, p.Id);

                            p = p.Parent;
                        }
                    }
                    else
                    {
                        activeCells.Add(this.Id);
                    }
                }
            }
        }

        /// <summary>
        ///     检查给定点是否在单元格内。
        /// </summary>
        /// <param name="yIsUpAxis">描述是否使用y轴作为上轴。</param>
        /// <param name="point">要检查的点。</param>
        /// <returns>如果点在单元格内则返回True，否则返回false。</returns>
        public bool IsPointInsideCell(bool yIsUpAxis, Vector3 point)
        {
            if ((point.x < this.TopLeft.x) || (point.x > this.BottomRight.x))
            {
                return false;
            }

            if (yIsUpAxis)
            {
                if ((point.y >= this.TopLeft.y) && (point.y <= this.BottomRight.y))
                {
                    return true;
                }
            }
            else
            {
                if ((point.z >= this.TopLeft.z) && (point.z <= this.BottomRight.z))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     检查给定点是否在单元格附近。
        /// </summary>
        /// <param name="yIsUpAxis">描述是否使用y轴作为上轴。</param>
        /// <param name="point">要检查的点。</param>
        /// <returns>如果点在单元格附近则返回True，如果点太远则返回false。</returns>
        public bool IsPointNearCell(bool yIsUpAxis, Vector3 point)
        {
            if (this.maxDistance == 0.0f)
            {
                this.maxDistance = (this.Size.x + this.Size.y + this.Size.z) / 2.0f;
            }

            return ((point - this.Center).sqrMagnitude <= (this.maxDistance * this.maxDistance));
        }
    }


    public class ByteComparer : IComparer<byte>
    {
        /// <inheritdoc />
        public int Compare(byte x, byte y)
        {
            return x == y ? 0 : x < y ? -1 : 1;
        }
    }
}