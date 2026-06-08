using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Factory : MonoBehaviour
{
    [SerializeField] private AnimalCreator animalCreator;

    private void Awake()
    {
        var animal = animalCreator.CreateAnimal();
        animal.transform.parent = transform;
        animal.transform.localPosition = Vector3.zero;
        animal.transform.localRotation = Quaternion.identity;
    }
}
