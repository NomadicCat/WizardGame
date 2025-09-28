using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraSpring : MonoBehaviour
{
    [Min(0.01f)]
    [SerializeField] private float halfLife = 0.075f;
    [Space]
    [SerializeField] private float frequency = 18f;
    [Space]
    [SerializeField] private float angularDisplacement = 2f;
    [SerializeField] private float linearDisplacement = 0.05f;




    private Vector3 _springPosition;
    private Vector3 _springVelocity;


    public void Initialize()
    {
        _springPosition = transform.position;
        _springVelocity = Vector3.zero;
    }
    public void UpdateSpring(float deltaTime, Vector3 up)
    {
        transform.localPosition = Vector3.zero;


        Spring(ref _springPosition, ref _springVelocity, transform.position, halfLife, frequency, deltaTime);
        var localSpringPosition = _springPosition - transform.position;
        var springHeight = Vector3.Dot(localSpringPosition, up);


        transform.localEulerAngles = new Vector3(-springHeight * angularDisplacement, 0f, 0f);
        transform.localPosition = localSpringPosition * linearDisplacement;


    }

    //private void OnDrawGizmos()
    //{
    //    Gizmos.color = Color.green;
    //    Gizmos.DrawLine(transform.position, _springPosition);
    //    Gizmos.DrawSphere(_springPosition, 0.1f);
    //}


    // Source: http://allenchou.net/2015/04/game-math-precise-control-over-numeric-springing/
    //public void Spring(ref float x, ref float v, float xt, float zeta, float omega, float h)
    //{
    //    float f = 1.0f + 2.0f * h * zeta * omega;
    //    float oo = omega * omega;
    //    float hoo = h * oo;
    //    float hhoo = h * hoo;
    //    float detInv = 1.0f / (f + hhoo);
    //    float detX = f * x + h * v + hhoo * xt;
    //    float detV = v + hoo * (xt - x);
    //    x = detX * detInv;
    //    v = detV * detInv;
    //}

    public void Spring(ref Vector3 current, ref Vector3 velocity, Vector3 target, float halfLife, float frequency, float timeStep)
    {
        var dampingRation = -Mathf.Log(0.5f) / (frequency * halfLife);
        var f = 1.0f + 2.0f * timeStep * dampingRation * frequency;
        var oo = frequency * frequency;
        var hoo = timeStep * oo;
        var hhoo = timeStep * hoo;
        var detInv = 1.0f / (f + hhoo);
        var detX = f * current + timeStep * velocity + hhoo * target;
        var detV = velocity + hoo * (target - current);
        current = detX * detInv;
        velocity = detV * detInv;
    }


}
