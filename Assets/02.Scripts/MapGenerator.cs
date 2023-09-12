using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEditor.Networking.PlayerConnection;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Line
{
    public Vector3Int lineNode1;
    public Vector3Int lineNode2;

    public bool isHorizontal;

    public Line (Vector3Int lineNode1, Vector3Int lineNode2)
    {
        this.lineNode1 = lineNode1;
        this.lineNode2 = lineNode2;

        if (lineNode1.y == lineNode2.y)
            isHorizontal = true;
        else
            isHorizontal = false;
    }
}

public class MapGenerator : MonoBehaviour
{
    private List<BSPNode> treeList = new List<BSPNode>();
    private List<BSPNode> roomList = new List<BSPNode>();

    private List<Line> lineList = new List<Line>();

    [SerializeField] private Vector2Int leftBottom, rightTop;
    private bool isDrawable;

    [SerializeField] private int roomMinSize;
    [SerializeField] private int maxDepth;
    private int minDepth;

    private int[,] map;
    // ��: 1 / ���: 2 / �ٴ�: 3

    public int num;

    [SerializeField] private Tile wall;
    [SerializeField] private Tile road;
    [SerializeField] private Tile place;
    [SerializeField] private Tilemap wallTilemap;
    [SerializeField] private Tilemap roadTilemap;

    public void GenerateMap()
    {
        map = new int[rightTop.y + 1, rightTop.x + 1];

        for (int y = 0; y <= rightTop.y; y++)
            for (int x = 0; x <= rightTop.x; x++)
                map[y, x] = 0;

        treeList.Clear();
        roomList.Clear();
        lineList.Clear();

        wallTilemap.ClearAllTiles();
        roadTilemap.ClearAllTiles();

        BSPNode rootNode = new BSPNode(leftBottom, rightTop);
        treeList.Add(rootNode);

        MakeTree(ref rootNode, minDepth);
        MakeRoom();
        ConnectRoom();
        ExtendLine();
        isDrawable = true;
        MakeWall();
        
        //string str = "";
        //for (int i = 0; i < rightTop.y; i++)
        //{
        //    for (int j = 0; j < rightTop.x; j++)
        //    {
        //        str += map[i, j].ToString();
        //        str += " ";
        //        if (map[i, j] == 1)
        //            str += " ";
        //    }
        //    str += "\n";
        //}
        //Debug.Log(str);

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
                        if (x == treeList[i].roomLeftBottom.x || x == treeList[i].roomRightTop.x || y == treeList[i].roomLeftBottom.y || y == treeList[i].roomRightTop.y)
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
        for (int x = 0; x < treeList.Count; x++) {
            for (int y = 0; y < treeList.Count; y++) {
                // ���� ���� ���� �ƴ����� �θ��尡 ���ٸ� = �� ���� ���� ������ �ڽ� �����ΰ�?
                if (treeList[x] != treeList[y] && treeList[x].pNode == treeList[y].pNode) 
                {
                    // ���ΰ� �� ��ٸ�
                    if (treeList[x].pNode.rightTop.x - treeList[x].pNode.leftBottom.x > treeList[x].pNode.rightTop.y - treeList[x].pNode.leftBottom.y)
                    {
                        int temp = (treeList[x].pNode.leftNode.leftBottom.y + treeList[x].pNode.leftNode.rightTop.y) / 2; // �ڸ� ��� ������ �߰� ����? ���ϰ�
                        Line line = new Line(
                            new Vector3Int(treeList[x].pNode.leftNode.rightTop.x - num, temp, 0), 
                            new Vector3Int(treeList[y].pNode.rightNode.leftBottom.x + num - 1, temp, 0));
                        lineList.Add(line);
                        MakeLine(line);
                    }
                    // ���ΰ� �� ��ٸ�
                    else
                    {
                        int temp = (treeList[x].pNode.leftNode.leftBottom.x + treeList[x].pNode.leftNode.rightTop.x) / 2;
                        Line line = new Line(
                            new Vector3Int(temp, treeList[x].pNode.leftNode.rightTop.y - num, 0),
                            new Vector3Int(temp, treeList[y].pNode.rightNode.leftBottom.y + num - 1, 0));
                        lineList.Add(line);
                        MakeLine(line);
                    }
                }
                }
        }
    }

    private void MakeLine(Line line)
    {
        if (line.lineNode1.x == line.lineNode2.x) // ���࿡ X�� ���ٸ� = ���μ��̴�
            for (int y = line.lineNode1.y; y <= line.lineNode2.y; y++)
                map[y, line.lineNode1.x] = 2; // �� ���� �̵��ϴ� ���
        else 
            for (int x = line.lineNode1.x; x <= line.lineNode2.x; x++)
                map[line.lineNode1.y, x] = 2;
    }

    private void ExtendLine()
    {
        for (int i = 0; i < lineList.Count; i++) // ���� ��ü�� ���鼭
        {
            if (lineList[i].isHorizontal) // ���� ���� ���μ��̸�
            {
                while (true)
                {
                    int x = lineList[i].lineNode1.x;
                    int y = lineList[i].lineNode1.y;
                    if (map[y, x - 1] == 0 || map[y, x - 1] == 1) // x ������ ����ְų� ���̰�
                    {
                        if (map[y + 1, x] == 2 || map[y - 1, x] == 2 || map[y + 1, x] == 3 || map[y - 1, x] == 3) // ���� y �� �Ǵ� �Ʒ��� �ٴ� �Ǵ� ���̶�� ��
                            break;
                        map[y, x - 1] = 2; // x ������ ����ְų� ���̴ϱ� ��� �����
                        lineList[i].lineNode1.x = x - 1; // x ������ x��
                    }
                    else break;
                }
                while (true)
                {
                    int x = lineList[i].lineNode2.x;
                    int y = lineList[i].lineNode2.y;
                    if (map[y, x + 1] == 0 || map[y, x + 1] == 1)
                    {
                        if (map[y + 1, x] == 2 || map[y - 1, x] == 2 || map[y + 1, x] == 3 || map[y - 1, x] == 3)
                            break;
                        map[y, x + 1] = 2;
                        lineList[i].lineNode2.x = x + 1;
                    }
                    else break;
                }
            }
            if (!lineList[i].isHorizontal)
            {
                while (true)
                {
                    int x = lineList[i].lineNode1.x;
                    int y = lineList[i].lineNode1.y;
                    if (map[y - 1, x] == 0 || map[y - 1, x] == 1)
                    {
                        if (map[y, x + 1] == 2 || map[y, x - 1] == 2 || map[y, x + 1] == 3 || map[y, x - 1] == 3)
                            break;
                        map[y - 1, x] = 2;
                        lineList[i].lineNode1.y = y - 1;
                    }
                    else break;
                }
                while (true)
                {
                    int x = lineList[i].lineNode2.x;
                    int y = lineList[i].lineNode2.y;
                    if (map[y + 1, x] == 0 || map[y + 1, x] == 1)
                    {
                        if (map[y, x + 1] == 2 || map[y, x - 1] == 2 || map[y, x + 1] == 3 || map[y, x - 1] == 3)
                            break;
                        map[y + 1, x] = 2;
                        lineList[i].lineNode2.y = y + 1;
                    }
                    else break;
                }
            }
        }
    }

    private void MakeWall()
    {
        for (int y = 0; y < rightTop.y; y++)
        {
            for (int x = 0; x < rightTop.x; x++)
            {
                if (map[y, x] == 2)
                {
                    for (int yy = -1; yy <= 1; yy++)
                        for (int xx = -1; xx <= 1; xx++)
                            if (map[y + yy, x + xx] == 0)
                                map[y + yy, x + xx] = 1; // ��
                }
            }
        }
    }

    private void GenerateTilemap()
    {
        for (int y = 0; y < rightTop.y; y++)
            for (int x = 0; x < rightTop.x; x++)
            {
                if (map[y, x] == 1)
                    wallTilemap.SetTile(new Vector3Int(x, y, 0), wall);
                else if (map[y, x] == 2)
                    roadTilemap.SetTile(new Vector3Int(x, y, 0), road);
                else if (map[y, x] == 3)
                    roadTilemap.SetTile(new Vector3Int(x, y, 0), place);
            }

    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (isDrawable)
        {
            Gizmos.color = Color.green;
            foreach (var room in roomList)
            {
                float x = room.rightTop.x - room.leftBottom.x;
                float y = room.rightTop.y - room.leftBottom.y;
                Vector3 size = new Vector3(room.rightTop.x - room.leftBottom.x, room.rightTop.y - room.leftBottom.y, 0);
                Vector3 center = new Vector3(room.rightTop.x - (x / 2), room.rightTop.y - (y / 2));
                Gizmos.DrawWireCube(center, size);
            }
            foreach (var line in lineList)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireCube(new Vector3(line.lineNode1.x + 0.5f, line.lineNode1.y + 0.5f, 0), Vector3.one);
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(new Vector3(line.lineNode2.x + 0.5f, line.lineNode2.y + 0.5f, 0), Vector3.one);
            }

        }
    }
#endif
}
