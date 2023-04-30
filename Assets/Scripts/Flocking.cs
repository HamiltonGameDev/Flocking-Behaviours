using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Flocking : MonoBehaviour
{


    [Header("Initial Spawn Setup values")]

    [SerializeField] private Vector3 spawnLimit;
    [SerializeField] private int flockSize;
    [SerializeField] private Flocking ElementPrefab;

    public Flocking[] flockElements;

    [Header("Behaviour Weight Values")]

    [Range(0, 20)]
    [SerializeField] private float _cohesionWeight;
    public float cohesionWeight { get { return _cohesionWeight; } }

    [Range(0, 20)]
    [SerializeField] private float _avoidanceWeight;
    public float avoidanceWeight { get { return _avoidanceWeight; } }

    [Range(0, 20)]
    [SerializeField] private float _aligementWeight;
    public float aligementWeight { get { return _aligementWeight; } }

    [Range(0, 20)]
    [SerializeField] private float _boundaryWeight;
    public float boundaryWeight { get { return _boundaryWeight; } }

    [Range(0, 100)]
    [SerializeField] private float _obstacleWeight;
    public float obstacleWeight { get { return _obstacleWeight; } }


    [Header("Detection Ranges")]

    [Range(0, 20)]
    [SerializeField] private float _cohesionRange;
    public float cohesionRange { get { return _cohesionRange; } }

    [Range(0, 20)]
    [SerializeField] private float _avoidanceRange;
    public float avoidanceRange { get { return _avoidanceRange; } }

    [Range(0, 20)]
    [SerializeField] private float _aligementRange;
    public float aligementRange { get { return _aligementRange; } }

    [Range(0, 20)]
    [SerializeField] private float _obstacleRange;
    public float obstacleRange { get { return _obstacleRange; } }

    [Range(0, 100)]
    [SerializeField] private float _boundaryRange;
    public float boundaryRange { get { return _boundaryRange; } }


    [Header("Speed Values")]

    [Range(0, 20)]
    [SerializeField] private float _maximumSpeed;
    public float maximumSpeed { get { return _maximumSpeed; } }

    [Range(0, 20)]
    [SerializeField] private float _minimumSpeed;
    public float minimumSpeed { get { return _minimumSpeed; } }

    public object myTransform { get; internal set; }

    private void Start()
    {
        SpawnElements();
    }

    private void Update()
    {
        for (int i = 0; i < flockElements.Length; i++)
        {
            flockElements[i].GetComponent<FlockElement>().MoveElement();
        }
    }

    private void SpawnElements()
    {
        flockElements = new Flocking[flockSize];

        for (int i = 0; i < flockElements.Length; i++)
        {
            Vector3 randomVector = UnityEngine.Random.insideUnitSphere;
            randomVector = new Vector3(randomVector.x * spawnLimit.x, randomVector.y * spawnLimit.y, randomVector.z * spawnLimit.z);

            Vector3 spawnLocation = transform.position + randomVector;
            Quaternion rotation = Quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0);

            flockElements[i] = Instantiate(ElementPrefab, spawnLocation, rotation);
            flockElements[i].GetComponent<FlockElement>().AssigntoFlockField(this);
            flockElements[i].GetComponent<FlockElement>().InitializeSpeed(UnityEngine.Random.Range(minimumSpeed, maximumSpeed));
        }
    }
}
