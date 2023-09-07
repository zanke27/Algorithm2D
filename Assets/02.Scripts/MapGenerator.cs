using System.Collections;
using System.Collections.Generic;
using UnityEditor.Networking.PlayerConnection;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Line
{
    public Vector3Int lineNode1;
    public Vector3Int lineNode2;

    public Line (Vector3Int lineNode1, Vector3Int lineNode2)
    {
        this.lineNode1 = lineNode1;
        this.lineNode2 = lineNode2;
    }
}

public class MapGenerator : MonoBehaviour
{
    private List<BSPNode> treeList;
    private List<BSPNode> roomList;

    private List<Line> lineList;

    [SerializeField] private Vector2Int leftBottom, rightTop;
    private bool isDrawable;

    [SerializeField] private int roomMinSize;
    [SerializeField] private int maxDepth;
    private int minDepth;

    private int[,] map;

    [SerializeField] private Tile wall;
    [SerializeField] private Tile road;
    [SerializeField] private Tilemap wallTilemap;
    [SerializeField] private Tilemap roadTilemap;

    private void GenerateMap()
    {
        for (int i = 0; i < rightTop.y; i++)
            for (int j = 0; j < rightTop.x; j++)
                map[j, i] = 0;

        treeList.Clear();
        roomList.Clear();
        lineList.Clear();

        BSPNode rootNode = new BSPNode(leftBottom, rightTop);
        treeList.Add(rootNode);

        MakeTree(ref rootNode, minDepth);
        MakeRoom();
        ConnectRoom();
        isDrawable = true;
        MakeWall();
        GenerateTilemap();
    }

    private void MakeTree(ref BSPNode node, int nowDepth) // root��带 �޾� Ʈ�� ������ ��������
    {
        node.nowDepth = nowDepth;
        if (nowDepth >= maxDepth) return; // ���� �ִ�ġ���� �����ߴٸ� ����

        int randomRatio = Random.Range(30, 71); // 3 : 7 ���� �ȿ��� ��������
        nowDepth += 1;
        if (node.DividedNode(randomRatio, roomMinSize)) // ��� �ڸ��� (�߸��� �ʴ´ٸ� ��带 �� �߰����� ����)
        {
            MakeTree(ref node.leftNode, nowDepth); // ��ͷ� �ڽ� ��尡 �ڸ��°� �ٽ� �ݺ��Ѵ�.
            MakeTree(ref node.rightNode, nowDepth);
            treeList.Add(node.leftNode); // treeList�� ��带 �߰��Ѵ�.
            treeList.Add(node.rightNode);
        }
    }

    private void MakeRoom() // treeList�ȿ� �ִ� �ֵ��߿� ���ϴܿ� �ִ� ���鿡 ���� �������
    {
        for (int i = 0; i < treeList.Count; i++)
        {
            treeList[i].RoomCreate();

            if (!treeList[i].isDivided) // ���� ���������� ������ = ���ϴ� ����
            {
                for (int y = treeList[i].roomLeftBottom.y; y <= treeList[i].roomRightTop.y; y++)
                {
                    for (int x = treeList[i].roomLeftBottom.x; x <= treeList[i].roomRightTop.x; x++) // ��ü ������ ���鼭
                    {
                        if (x == treeList[i].roomLeftBottom.x || x == treeList[i].roomRightTop.x
                            || treeList[i].roomLeftBottom.y == y || treeList[i].roomRightTop.y == y)
                            map[y, x] = 1; // ���κ��� ��
                        else
                            map[y, x] = 3; // ������ �ٴ����� ä��
                    }
                }
                roomList.Add(treeList[i]); // �� ä��� ���� ���� roomList��
            }
        }
    }

    private void ConnectRoom()
    {
        for (int i = 0; i < treeList.Count; i++) {
            for (int j = 0; j < treeList.Count; j++) { 
                // ���� ���� ���� �ƴ����� �θ��尡 ���ٸ� = �� ���� ���� ������ �ڽ� �����ΰ�?
                if (treeList[i] != treeList[j] && treeList[i].pNode == treeList[i].pNode) { 
                    if (treeList[i].pNode.roomRightTop.x - treeList[i].pNode.roomLeftBottom.x  // ���ΰ� �� ��ٸ� = ���� ������ �ڸ��Ŷ��
                        > treeList[i].pNode.roomRightTop.y - treeList[i].pNode.roomLeftBottom.y)
                    {
                        int temp = (treeList[i].pNode.leftNode.rightTop.y + treeList[i].pNode.leftNode.leftBottom.y) / 2; // �ڸ� ��� ������ �߰� ����?
                        //int line = new Line(new Vector3Int(treeList[i]))
                    }
                }
            }
        }
    }

    private void MakeWall()
    {

    }

    private void GenerateTilemap()
    {

    }
}
