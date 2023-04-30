using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class FlockElement : MonoBehaviour
{

    [SerializeField] private float FieldOfViewAngle;
    [SerializeField] private float smoothDamp;
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private Vector3[] checkDirections;                    // Directions to check when avoiding obstacles     

    private List<FlockElement> cohesionNeighbours = new List<FlockElement>();
    private List<FlockElement> avoidanceNeighbours = new List<FlockElement>();
    private List<FlockElement> aligementNeighbours = new List<FlockElement>();

    private Flocking assignedFlock;
    private Vector3 currentVelocity;
    private Vector3 currentObstacleAvoidanceVector;
    private float speed;

    public Transform myTransform { get; set; }

    private void Awake()
    {
        myTransform = transform;
    }

    public void AssigntoFlockField(Flocking flock)
    {
        assignedFlock = flock;
    }

    public void InitializeSpeed(float speed)
    {
        this.speed = speed;
    }

    public void MoveElement()
    {
        LocateNeighbouringElements();
        CalculateSpeed();

        Vector3 aligementVector = CalculateAligementVector() * assignedFlock.aligementWeight;
        Vector3 boundsVector    = CalculateBoundsVector()    * assignedFlock.boundaryWeight;
        Vector3 obstacleVector  = CalculateObstacleVector()  * assignedFlock.obstacleWeight;
        Vector3 cohesionVector  = CalculateCohesionVector()  * assignedFlock.cohesionWeight;
        Vector3 avoidanceVector = CalculateAvoidanceVector() * assignedFlock.avoidanceWeight;
       

        Vector3 overallMoveVector = cohesionVector + avoidanceVector + aligementVector + boundsVector + obstacleVector;
        overallMoveVector = Vector3.SmoothDamp(myTransform.forward, overallMoveVector, ref currentVelocity, smoothDamp);
        overallMoveVector = overallMoveVector.normalized * speed;
        
        if (overallMoveVector == Vector3.zero)
            overallMoveVector = transform.forward;

        myTransform.forward = overallMoveVector;
        myTransform.position += overallMoveVector * Time.deltaTime;
    }



    private void LocateNeighbouringElements()
    {
        cohesionNeighbours.Clear();
        avoidanceNeighbours.Clear();
        aligementNeighbours.Clear();

        var Elments = assignedFlock.flockElements;
       
        for (int i = 0; i < Elments.Length; i++)
        {
            var currentElement = Elments[i];
            
            if (currentElement != this)
            {
                float currentNeighbourDistanceSqr = Vector3.SqrMagnitude(currentElement.transform.position - myTransform.position);
                
                if (currentNeighbourDistanceSqr <= assignedFlock.cohesionRange * assignedFlock.cohesionRange)
                {
                    cohesionNeighbours.Add(currentElement.GetComponent<FlockElement>());
                }
                
                if (currentNeighbourDistanceSqr <= assignedFlock.avoidanceRange * assignedFlock.avoidanceRange)
                {
                    avoidanceNeighbours.Add(currentElement.GetComponent<FlockElement>());
                }
                
                if (currentNeighbourDistanceSqr <= assignedFlock.aligementRange * assignedFlock.aligementRange)
                {
                    aligementNeighbours.Add(currentElement.GetComponent<FlockElement>());
                }
            }
        }
    }

    private void CalculateSpeed()
    {
        if (cohesionNeighbours.Count == 0)
            return;

        speed = 0;

        for (int i = 0; i < cohesionNeighbours.Count; i++)
        {
            speed += cohesionNeighbours[i].speed;
        }

        speed /= cohesionNeighbours.Count;
        speed = Mathf.Clamp(speed, assignedFlock.minimumSpeed, assignedFlock.maximumSpeed);
    }

    private Vector3 CalculateCohesionVector()
    {
        Vector3 cohesionVector = Vector3.zero;
       
        if (cohesionNeighbours.Count == 0)
            return Vector3.zero;
       
        int neighboursInFOV = 0;
       
        for (int i = 0; i < cohesionNeighbours.Count; i++)
        {
            if (IsInFieldOfView(cohesionNeighbours[i].myTransform.position))
            {
                neighboursInFOV++;
                cohesionVector += cohesionNeighbours[i].myTransform.position;
            }
        }

        cohesionVector /= neighboursInFOV;
        cohesionVector -= myTransform.position;
        cohesionVector = cohesionVector.normalized;
       
        return cohesionVector;
    }

    private Vector3 CalculateAligementVector()
    {
        Vector3 aligementVector = myTransform.forward;
       
        if (aligementNeighbours.Count == 0)
            return myTransform.forward;

        int neighboursInFOV = 0;
       
        for (int i = 0; i < aligementNeighbours.Count; i++)
        {
            if (IsInFieldOfView(aligementNeighbours[i].myTransform.forward))
            {
                neighboursInFOV++;
                aligementVector += aligementNeighbours[i].myTransform.forward;
            }
        }

        aligementVector /= neighboursInFOV;
        aligementVector = aligementVector.normalized;
       
        return aligementVector;
    }

    private Vector3 CalculateAvoidanceVector()
    {
        Vector3 avoidanceVector = Vector3.zero;
       
        if (aligementNeighbours.Count == 0)
            return Vector3.zero;
       
        int neighboursInFOV = 0;
        
        for (int i = 0; i < avoidanceNeighbours.Count; i++)
        {
            if (IsInFieldOfView(avoidanceNeighbours[i].myTransform.position))
            {
                neighboursInFOV++;
                avoidanceVector += (myTransform.position - avoidanceNeighbours[i].myTransform.position);
            }
        }

        avoidanceVector /= neighboursInFOV;
        avoidanceVector = avoidanceVector.normalized;
       
        return avoidanceVector;
    }

    private Vector3 CalculateBoundsVector()
    {
        Vector3 offsetToCenter = assignedFlock.transform.position - myTransform.position;
       
        bool isNearCenter = (offsetToCenter.magnitude >= assignedFlock.boundaryRange * 0.9f);
       
        return isNearCenter ? offsetToCenter.normalized : Vector3.zero;
    }

    private Vector3 CalculateObstacleVector()
    {
        Vector3 obstacleVector = Vector3.zero;

        RaycastHit hit;
       
        if (Physics.Raycast(myTransform.position, myTransform.forward, out hit, assignedFlock.obstacleRange, obstacleMask))
        {
            obstacleVector = FindAlternateDirection();
        }
        else
        {
            currentObstacleAvoidanceVector = Vector3.zero;
        }
        return obstacleVector;
    }

    private Vector3 FindAlternateDirection()            // Method used to find the best direction to avoid an obstacle
    {
        if (currentObstacleAvoidanceVector != Vector3.zero)
        {
            RaycastHit hit;

            if (!Physics.Raycast(myTransform.position, myTransform.forward, out hit, assignedFlock.obstacleRange, obstacleMask))
            {
                return currentObstacleAvoidanceVector;
            }
        }
        float maxDistance = int.MinValue;

        Vector3 selectedDirection = Vector3.zero;

        for (int i = 0; i < checkDirections.Length; i++)
        {
            
            RaycastHit hit;

            Vector3 currentDirection = myTransform.TransformDirection(checkDirections[i].normalized);

            Debug.DrawRay(myTransform.position, currentDirection * assignedFlock.obstacleRange * 10, Color.green);
           
            if (Physics.Raycast(myTransform.position, currentDirection, out hit, assignedFlock.obstacleRange, obstacleMask))
            {

                float currentDistance = (hit.point - myTransform.position).sqrMagnitude;

                if (currentDistance > maxDistance)
                {
                    maxDistance = currentDistance;
                    selectedDirection = currentDirection;
                }

                // Draw the raycast in green color
               // Debug.DrawLine(myTransform.position, hit.point, Color.green);
            }
           
            else
            {
                selectedDirection = currentDirection;
                currentObstacleAvoidanceVector = currentDirection.normalized;
                return selectedDirection.normalized;
            }
        }
        return selectedDirection.normalized;
    }

    private bool IsInFieldOfView(Vector3 position)
    {
        return Vector3.Angle(myTransform.forward, position - myTransform.position) <= FieldOfViewAngle;
    }
}
