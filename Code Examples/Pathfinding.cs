using System.Collections;
using System.Collections.Generic;
using UnityEngine;

struct node
{
    public Vector3Int pos;
    public int g;
    public int h;
    public int f;
    public Vector3Int parentPos;
}

public class Pathfinding : MonoBehaviour
{
    #region singleton
    public static Pathfinding instance;

    void Awake()
    {
        if (instance != null)
        {
            Debug.LogWarning("More than once instance of Pathfinding found.");
            return;
        }
        instance = this;
    }
    #endregion


    public List<Vector3Int> getPath(Vector3Int startingPos, Vector3Int endPos, List<Vector3Int> moveablePoints)
    {
        
        Debug.Log("Starting Pathfinding");
        List<Vector3Int> shortestPath = new List<Vector3Int>();

        List<node> openNodes = new List<node>();
        List<node> closedNodes = new List<node>();
        node curNode,
            successorNode,
            tempNode;

        curNode.pos = startingPos;
        curNode.g = 0;
        curNode.h = distToPoint(curNode.pos, endPos);
        curNode.f = curNode.g + curNode.h;
        curNode.parentPos = startingPos;
        openNodes.Add(curNode);
        
        while (openNodes.Count > 0 )
        {
            curNode = openNodes[0];

            foreach(node q in openNodes)
            {
                if (q.f < curNode.f || 
                   (q.f == curNode.f && q.h < curNode.h))
                    curNode = q;
            }

            openNodes.Remove(curNode);
            closedNodes.Add(curNode);

            if (curNode.pos == endPos)
            {
                //end search
                Debug.Log("curNode = endPos:");
                shortestPath = pathFromParentNodes(closedNodes, curNode, startingPos);
                break;
            }
            //generate node successors
            successorNode.pos = curNode.pos;
            successorNode.pos.x++;
            if (moveablePoints.Contains(successorNode.pos) && !doesPosExistInList(closedNodes, successorNode.pos))
            {
                successorNode.g = curNode.g + 1;
                successorNode.h = distToPoint(successorNode.pos, endPos);
                successorNode.f = successorNode.g + successorNode.h;
                successorNode.parentPos = curNode.pos;
                if (doesPosExistInList(openNodes, successorNode.pos))
                {
                    tempNode = getNodeByPos(openNodes, successorNode.pos);

                    if (successorNode.f < tempNode.f ||
                        (successorNode.f == tempNode.f && successorNode.h < tempNode.h))
                    {
                        openNodes.Remove(tempNode);
                        openNodes.Add(successorNode);
                        //Debug.Log("replacing node in open. Pos:" + successorNode.pos + " h: " + successorNode.h
                                 //+ " f: " + successorNode.f + "parentPos: " + successorNode.parentPos);
                    }
                    else
                    {
                        Debug.Log("Did not add node to open. Pos:" + successorNode.pos + " h: " + successorNode.h
                                + " f: " + successorNode.f + "parentPos: " + successorNode.parentPos);
                        Debug.Log("Adding new node to open. Pos:" + curNode.pos + " h: " + curNode.h
                                + " f: " + curNode.f + "parentPos: " + curNode.parentPos);
                    }
                }
                else
                {
                    openNodes.Add(successorNode);
                    //Debug.Log("node " + successorNode.pos + " G count:" + successorNode.g);

                    //Debug.Log("Adding new node to open. Pos:" + successorNode.pos + " h: " + successorNode.h
                    //+ " f: " + successorNode.f + "parentPos: " + successorNode.parentPos);
                }
            }
            successorNode.pos = curNode.pos;
            successorNode.pos.y--;
            if (moveablePoints.Contains(successorNode.pos) && !doesPosExistInList(closedNodes, successorNode.pos))
            {
                successorNode.g = curNode.g + 1;
                successorNode.h = distToPoint(successorNode.pos, endPos);
                successorNode.f = successorNode.g + successorNode.h;
                successorNode.parentPos = curNode.pos;
                if (doesPosExistInList(openNodes, successorNode.pos))
                {
                    tempNode = getNodeByPos(openNodes, successorNode.pos);

                    if (successorNode.f < tempNode.f ||
                        (successorNode.f == tempNode.f && successorNode.h < tempNode.h))
                    {
                        openNodes.Remove(tempNode);
                        openNodes.Add(successorNode);
                        Debug.Log("replacing node in open. Pos:" + successorNode.pos + " h: " + successorNode.h
                                 + " f: " + successorNode.f + "parentPos: " + successorNode.parentPos);
                    }
                }
                else
                {
                    openNodes.Add(successorNode);
                    //Debug.Log("node " + successorNode.pos + " G count:" + successorNode.g);

                    //Debug.Log("Adding new node to open. Pos:" + successorNode.pos + " h: " + successorNode.h
                    //+ " f: " + successorNode.f + "parentPos: " + successorNode.parentPos);
                }
            }
            successorNode.pos = curNode.pos;
            successorNode.pos.x--;
            if (moveablePoints.Contains(successorNode.pos) && !doesPosExistInList(closedNodes, successorNode.pos))
            {
                successorNode.g = curNode.g + 1;
                successorNode.h = distToPoint(successorNode.pos, endPos);
                successorNode.f = successorNode.g + successorNode.h;
                successorNode.parentPos = curNode.pos;
                if (doesPosExistInList(openNodes, successorNode.pos))
                {
                    tempNode = getNodeByPos(openNodes, successorNode.pos);

                    if (successorNode.f < tempNode.f ||
                        (successorNode.f == tempNode.f && successorNode.h < tempNode.h))
                    {
                        openNodes.Remove(tempNode);
                        openNodes.Add(successorNode);
                        Debug.Log("replacing node in open. Pos:" + successorNode.pos + " h: " + successorNode.h
                                 + " f: " + successorNode.f + "parentPos: " + successorNode.parentPos);
                    }
                }
                else
                {
                    openNodes.Add(successorNode);
                   // Debug.Log("node " + successorNode.pos + " G count:" + successorNode.g);

                    //Debug.Log("Adding new node to open. Pos:" + successorNode.pos + " h: " + successorNode.h
                    //+ " f: " + successorNode.f + "parentPos: " + successorNode.parentPos);
                }
            }
            successorNode.pos = curNode.pos;
            successorNode.pos.y++;
            if (moveablePoints.Contains(successorNode.pos) && !doesPosExistInList(closedNodes, successorNode.pos))
            {
                successorNode.g = curNode.g + 1;
                successorNode.h = distToPoint(successorNode.pos, endPos);
                successorNode.f = successorNode.g + successorNode.h;
                successorNode.parentPos = curNode.pos;
                if (doesPosExistInList(openNodes, successorNode.pos))
                {
                    tempNode = getNodeByPos(openNodes, successorNode.pos);

                    if (successorNode.f < tempNode.f ||
                        (successorNode.f == tempNode.f && successorNode.h < tempNode.h))
                    {
                        openNodes.Remove(tempNode);
                        openNodes.Add(successorNode);
                       // Debug.Log("replacing node in open. Pos:" + successorNode.pos + " h: " + successorNode.h
                                 //+ " f: " + successorNode.f + "parentPos: " + successorNode.parentPos);
                    }
                }
                else
                {
                    openNodes.Add(successorNode);
                    //Debug.Log("node " + successorNode.pos + " G count:" + successorNode.g);

                    //Debug.Log("Adding new node to open. Pos:" + successorNode.pos + " h: " + successorNode.h
                    //+ " f: " + successorNode.f + "parentPos: " + successorNode.parentPos);
                }
            }
            //Debug.Log("OpenNode size:" + openNodes.Count);

            //counter++;
        }
        //Debug.Log("counter:" + counter);
        return shortestPath;
    }

    List<Vector3Int> pathFromParentNodes(List<node> closedNodes, node endNode, Vector3Int startingPos)
    {
        List<Vector3Int> shortestPath = new List<Vector3Int>();
        node curNode = endNode;

        while (curNode.pos != startingPos)
        {
            shortestPath.Insert(0, curNode.pos);

            foreach (node q in closedNodes)
                if (q.pos == curNode.parentPos)
                    curNode = q;
        }

        foreach (Vector3Int pos in shortestPath)
            //Debug.Log("Path-step: " + pos);
            continue;

        return shortestPath;
    }

    int distToPoint(Vector3Int pos, Vector3Int endPos)
    {
        int dist = Mathf.Abs(endPos.x - pos.x) + Mathf.Abs(endPos.y - pos.y);
        return dist;
    }

    bool doesPosExistInList(List<node> nodelist, Vector3Int pos)
    {
        if (nodelist.Count > 0)
            foreach (node q in nodelist)
            {
                if (q.pos == pos)
                    return true;
            }
        else
            Debug.Log("NODELIST IS EMPTY");

        return false;
    }

    node getNodeByPos(List<node> nodeList, Vector3Int pos)
    {
        node tempNode;
        tempNode.g = 0;
        tempNode.h = 0;
        tempNode.f = 0;
        tempNode.pos = pos;
        tempNode.parentPos = pos;

        foreach(node q in nodeList)
        {
            if (q.pos == pos)
                tempNode = q;
        }

        return tempNode;
    }
}
