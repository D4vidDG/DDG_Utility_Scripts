
using UnityEngine;

public class ZigZagPath : MonoBehaviour
{
    [SerializeField][Min(1)] int numberOfTriangles;
    [SerializeField] float horizontalSpread;
    [SerializeField] float verticalSpread;

    void Start()
    {
        this.transform.parent = null;
    }

    [InspectorButton("CreatePath")] public bool createPath;

    void CreatePath()
    {
        int numberOfVertices = numberOfTriangles * 3 - (numberOfTriangles - 1);
        if (transform.childCount < numberOfVertices)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                transform.GetChild(i).gameObject.SetActive(true);
            }

            int verticesToCreate = numberOfVertices - transform.childCount;
            for (int i = 0; i < verticesToCreate; i++)
            {
                GameObject newWaypoint = new GameObject();
                newWaypoint.transform.position = this.transform.position;
                newWaypoint.transform.parent = this.transform;
            }
        }
        else if (numberOfVertices < transform.childCount)
        {
            int verticesToDelete = transform.childCount - numberOfVertices;
            for (int i = 0; i < verticesToDelete; i++)
            {
                DestroyImmediate(transform.GetChild(numberOfVertices).gameObject);
            }
        }


        for (int i = 1; i < numberOfVertices; i++)
        {
            int waypointNumber = (i + 1);
            bool isEven = waypointNumber % 2 == 0;
            bool isMultipleOfFour = waypointNumber % 4 == 0;
            if (isEven)
            {
                Vector3 verticalDirection = isMultipleOfFour ? transform.right : -transform.right;
                transform.GetChild(i).position = transform.position +
                    horizontalSpread * i * transform.forward +
                    verticalDirection * verticalSpread;
            }
            else
            {
                transform.GetChild(i).position = transform.position +
                    horizontalSpread * i * transform.forward;
            }
        }
    }
}
