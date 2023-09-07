using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BSPNode
{
    public BSPNode pNode;                   // �θ� ���
    public BSPNode leftNode, rightNode;     // �ڽ� ���
                                            
    public bool isDivided;                  // �̹� ���������� ����
    public int nowDepth;                    // ���� ����

    public Vector2Int leftBottom, rightTop; // ��� ��ü�� ���ϴܰ� ����
    public Vector2Int roomLeftBottom, roomRightTop; // ��� ������ ���� ���� �� ���

    public BSPNode(Vector2Int leftBottom, Vector2Int rightTop)
    {
        this.leftBottom = leftBottom;
        this.rightTop = rightTop;
    }

    public bool DividedNode(int ratio, int minSize)
    {
        float tempWidth, tempHeight;
        Vector2Int devideLine1, devideLine2; // �ڸ� �� ��ġ

        // ���ΰ� �� ��� -> ���η� �ڸ� ����
        if (rightTop.x - leftBottom.x > rightTop.y - leftBottom.y)
        {
            tempWidth = rightTop.x - leftBottom.x;             // tempWidth�� ����� width�� �־��ְ�
            tempWidth = tempWidth * ratio / 100;    // ������ ���� �߶��ش�
            int width = Mathf.RoundToInt(tempWidth);// �ݿø��ϸ� ���η� �ڸ��� ���� ���� ���� ����
            if (width < minSize || rightTop.x - leftBottom.x - width < minSize)
                return false;                       // ���� ���� �ּ�ġ���� ������ �ڸ��� ����
            devideLine1 = new Vector2Int(leftBottom.x + width, rightTop.y); // �簢���� �ڸ� ���μ��� ���� ��
            devideLine2 = new Vector2Int(leftBottom.x + width, leftBottom.y); // �簢���� �ڸ� ���μ��� �Ʒ� ��
        }
        else
        {
            tempHeight = rightTop.y - leftBottom.y;
            tempHeight = tempHeight * ratio / 100;
            int height = Mathf.RoundToInt(tempHeight);
            if (height < minSize || rightTop.y - leftBottom.y - height < minSize)
                return false;
            devideLine1 = new Vector2Int(rightTop.x, leftBottom.y + height); // �簢���� �ڸ� ���μ��� ������ ��
            devideLine2 = new Vector2Int(leftBottom.x, leftBottom.y + height); // �簢���� �ڸ� ���μ��� ���� ��
        }

        leftNode = new BSPNode(leftBottom, devideLine1); // �̷��� �ϸ� ���� ���� ������� ���ϴ� ���� ����
        rightNode = new BSPNode(devideLine2, rightTop);
        leftNode.pNode = this;
        rightNode.pNode = this;
        isDivided = true;
        return true;
    }

    public void RoomCreate()
    {
        int disFrom = 2;
        if (!isDivided) // �ּ�ġ�� ���߷ȴٸ�
        {               // disFrom��ŭ ������ ������ ���� ũ�⸦ ����
            roomLeftBottom = new Vector2Int(leftBottom.x + disFrom, leftBottom.y + disFrom);
            roomRightTop = new Vector2Int(rightTop.x - disFrom, rightTop.y - disFrom);
        }
    }
}
