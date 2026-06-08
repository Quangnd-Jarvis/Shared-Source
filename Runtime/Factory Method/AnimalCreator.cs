using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AnimalCreator : MonoBehaviour
{
    [SerializeField] protected GameObject animalPrefab;
    
    public virtual GameObject CreateAnimal()
    {
        return Instantiate(animalPrefab);
    }

    public virtual void DoSomething()
    {
        
    }
}

    
