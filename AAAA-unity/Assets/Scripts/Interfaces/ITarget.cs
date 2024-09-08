using System.Collections.Generic;
using Unity.Barracuda;
using UnityEngine;

public interface ITarget: IMeasurable
{
    void ResetTarget();
    GameObject GetGameObject();  // TODO: This could be avoided by using subclasses instead of interfaces?
}
