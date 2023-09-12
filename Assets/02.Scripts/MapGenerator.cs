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
    // 벽: 1 / 통로: 2 / 바닥: 3

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

    private void MakeTree(ref BSPNode node, int nowDepth) // root노드를 받아 트리 구조를 생성해줌
    {
        node.nowDepth = nowDepth;
        if (nowDepth >= maxDepth) return; // 만약 최대치까지 분할했다면 종료

        int randomRatio = Random.Range(30, 71); // 3 : 7 비율 안에서 무작위로
        nowDepth += 1;
        if (node.DividedNode(randomRatio, roomMinSize)) // 노드 자르기 (잘리지 않는다면 노드를 더 추가하지 않음)
        {
            MakeTree(ref node.leftNode, nowDepth); // 재귀로 자식 노드가 자르는걸 다시 반복한다.
            MakeTree(ref node.rightNode, nowDepth);
            treeList.Add(node.leftNode); // treeList에 노드를 추가한다.
            treeList.Add(node.rightNode);
        }
    }

    private void MakeRoom() // treeList안에 있는 애들중에 최하단에 있는 노드들에 방을 만들어줌
    {
        for (int i = 0; i < treeList.Count; i++)
        {
            treeList[i].RoomCreate();

            if (!treeList[i].isDivided) // 만약 나누어진적 없으면 = 최하단 노드면
            {
                for (int y = treeList[i].roomLeftBottom.y; y <= treeList[i].roomRightTop.y; y++)
                {
                    for (int x = treeList[i].roomLeftBottom.x; x <= treeList[i].roomRightTop.x; x++) // 전체 범위를 돌면서
                    {
                        if (x == treeList[i].roomLeftBottom.x || x == treeList[i].roomRightTop.x || y == treeList[i].roomLeftBottom.y || y == treeList[i].roomRightTop.y)
                            map[y, x] = 1; // 끝부분은 벽
                        else
                            map[y, x] = 3; // 안쪽은 바닥으로 채움
                    }
                }
                roomList.Add(treeList[i]); // 다 채우고 만든 룸을 roomList로
            }
        }
    }

    private void ConnectRoom()
    {
        for (int x = 0; x < treeList.Count; x++) {
            for (int y = 0; y < treeList.Count; y++) {
                // 서로 같은 노드는 아니지만 부모노드가 같다면 = 한 노드로 부터 나눠진 자식 노드들인가?
                if (treeList[x] != treeList[y] && treeList[x].pNode == treeList[y].pNode) 
                {
                    // 가로가 더 길다면
                    if (treeList[x].pNode.rightTop.x - treeList[x].pNode.leftBottom.x > treeList[x].pNode.rightTop.y - treeList[x].pNode.leftBottom.y)
                    {
                        int temp = (treeList[x].pNode.leftNode.leftBottom.y + treeList[x].pNode.leftNode.rightTop.y) / 2; // 자른 노드 사이의 중간 높이? 구하고
                        Line line = new Line(
                            new Vector3Int(treeList[x].pNode.leftNode.rightTop.x - num, temp, 0), 
                            new Vector3Int(treeList[y].pNode.rightNode.leftBottom.x + num - 1, temp, 0));
                        lineList.Add(line);
                        MakeLine(line);
                    }
                    // 세로가 더 길다면
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
        if (line.lineNode1.x == line.lineNode2.x) // 만약에 X가 같다면 = 세로선이다
            for (int y = line.lineNode1.y; y <= line.lineNode2.y; y++)
                map[y, line.lineNode1.x] = 2; // 방 사이 이동하는 통로
        else 
            for (int x = line.lineNode1.x; x <= line.lineNode2.x; x++)
                map[line.lineNode1.y, x] = 2;
    }

    private void ExtendLine()
    {
        for (int i = 0; i < lineList.Count; i++) // 라인 전체를 돌면서
        {
            if (lineList[i].isHorizontal) // 만약 선이 가로선이면
            {
                while (true)
                {
                    int x = lineList[i].lineNode1.x;
                    int y = lineList[i].lineNode1.y;
                    if (map[y, x - 1] == 0 || map[y, x - 1] == 1) // x 왼쪽이 비어있거나 벽이고
                    {
                        if (map[y + 1, x] == 2 || map[y - 1, x] == 2 || map[y + 1, x] == 3 || map[y - 1, x] == 3) // 만약 y 위 또는 아래가 바닥 또는 길이라면 끝
                            break;
                        map[y, x - 1] = 2; // x 왼쪽은 비어있거나 벽이니까 길로 만들고
                        lineList[i].lineNode1.x = x - 1; // x 왼쪽을 x로
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
                                map[y + yy, x + xx] = 1; // 벽
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
