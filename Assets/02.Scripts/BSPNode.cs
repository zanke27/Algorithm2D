using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BSPNode
{
    public BSPNode pNode;                   // 부모 노드
    public BSPNode leftNode, rightNode;     // 자식 노드
                                            
    public bool isDivided;                  // 이미 나눠졌는지 여부
    public int nowDepth;                    // 현재 깊이

    public Vector2Int leftBottom, rightTop; // 노드 자체의 좌하단과 우상단
    public Vector2Int roomLeftBottom, roomRightTop; // 노드 내부의 방을 만들 때 사용

    public BSPNode(Vector2Int leftBottom, Vector2Int rightTop)
    {
        this.leftBottom = leftBottom;
        this.rightTop = rightTop;
    }

    public bool DividedNode(int ratio, int minSize)
    {
        float tempWidth, tempHeight;
        Vector2Int devideLine1, devideLine2; // 자를 선 위치

        // 가로가 더 길다 -> 세로로 자를 예정
        if (rightTop.x - leftBottom.x > rightTop.y - leftBottom.y)
        {
            tempWidth = rightTop.x - leftBottom.x;             // tempWidth에 노드의 width값 넣어주고
            tempWidth = tempWidth * ratio / 100;    // 비율에 따라 잘라준다
            int width = Mathf.RoundToInt(tempWidth);// 반올림하면 세로로 자르고 남을 가로 값이 남음
            if (width < minSize || rightTop.x - leftBottom.x - width < minSize)
                return false;                       // 가로 값이 최소치보다 작으면 자르기 실패
            devideLine1 = new Vector2Int(leftBottom.x + width, rightTop.y); // 사각형을 자를 세로선의 위쪽 점
            devideLine2 = new Vector2Int(leftBottom.x + width, leftBottom.y); // 사각형을 자를 세로선의 아래 점
        }
        else
        {
            tempHeight = rightTop.y - leftBottom.y;
            tempHeight = tempHeight * ratio / 100;
            int height = Mathf.RoundToInt(tempHeight);
            if (height < minSize || rightTop.y - leftBottom.y - height < minSize)
                return false;
            devideLine1 = new Vector2Int(rightTop.x, leftBottom.y + height); // 사각형을 자를 가로선의 오른쪽 점
            devideLine2 = new Vector2Int(leftBottom.x, leftBottom.y + height); // 사각형을 자를 가로선의 왼쪽 점
        }

        leftNode = new BSPNode(leftBottom, devideLine1); // 이렇게 하면 가로 세로 상관없이 좌하단 우상단 나옴
        rightNode = new BSPNode(devideLine2, rightTop);
        leftNode.pNode = this;
        rightNode.pNode = this;
        isDivided = true;
        return true;
    }

    public void RoomCreate()
    {
        int disFrom = 2;
        if (!isDivided) // 최소치라 안잘렸다면
        {               // disFrom만큼 벽에서 떨어진 방의 크기를 구함
            roomLeftBottom = new Vector2Int(leftBottom.x + disFrom, leftBottom.y + disFrom);
            roomRightTop = new Vector2Int(rightTop.x - disFrom, rightTop.y - disFrom);
        }
    }
}
