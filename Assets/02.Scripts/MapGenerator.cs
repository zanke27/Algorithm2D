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
                        if (x == treeList[i].roomLeftBottom.x || x == treeList[i].roomRightTop.x
                            || treeList[i].roomLeftBottom.y == y || treeList[i].roomRightTop.y == y)
                            map[y, x] = 1; // 끝부분은 벽, 타일 맵 까는거로 변경해야함
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
        for (int i = 0; i < treeList.Count; i++) {
            for (int j = 0; j < treeList.Count; j++) { 
                // 서로 같은 노드는 아니지만 부모노드가 같다면 = 한 노드로 부터 나눠진 자식 노드들인가?
                if (treeList[i] != treeList[j] && treeList[i].pNode == treeList[i].pNode) { 
                    if (treeList[i].pNode.roomRightTop.x - treeList[i].pNode.roomLeftBottom.x  // 가로가 더 길다면 = 세로 선으로 자른거라면
                        > treeList[i].pNode.roomRightTop.y - treeList[i].pNode.roomLeftBottom.y)
                    {
                        int temp = (treeList[i].pNode.leftNode.leftBottom.y + treeList[i].pNode.leftNode.rightTop.y) / 2; // 자른 노드 사이의 중간 높이? 구하고
                        Line line = new Line(
                            new Vector3Int(treeList[i].pNode.leftNode.rightTop.x - 2, temp, 0), 
                            new Vector3Int(treeList[i].pNode.rightNode.leftBottom.x + 2, temp, 0));
                        lineList.Add(line);
                        MakeLine(line);
                    }
                    else
                    {
                        int temp = (treeList[i].pNode.leftNode.leftBottom.x + treeList[i].pNode.leftNode.rightTop.x) / 2;
                        Line line = new Line(
                            new Vector3Int(temp, treeList[i].pNode.leftNode.rightTop.y -2 , 0),
                            new Vector3Int(temp, treeList[i].pNode.rightNode.leftBottom.y + 2, 0));
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
            for (int i = line.lineNode1.y; i < line.lineNode2.y; i++)
                map[i, line.lineNode1.x] = 2; // 방 사이 이동하는 통로 // 타일맵 까는걸로 변경해야함
        else 
            for (int i = line.lineNode1.x; i < line.lineNode2.x; i++)
                map[line.lineNode1.y, i] = 2;
    }

    private void MakeWall()
    {
        for (int y = 0; y <= rightTop.y; y++)
        {
            for (int x = 0; x <= rightTop.x; x++)
            {
                if (map[y, x] == 2)
                {
                    for (int i = -1; i <= 1; i++)
                        for (int j = -1; j <= 1; j++)
                            if (map[y + j, x + i] == 0)
                                map[y + j, x + i] = 1; // 벽 // 타일맵 까는걸로 변경해야함
                }
            }
        }
    }

    private void GenerateTilemap()
    {

    }
}
