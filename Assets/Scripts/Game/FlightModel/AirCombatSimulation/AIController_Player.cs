using UnityEngine;

public class AIController_Player : StateBase
{
    public AIController controller;
    public PlayerInputs inputs;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public override void OnStateEnd()
    {
        throw new System.NotImplementedException();
    }

    public override void OnStateStay()
    {
        if (inputs != null)
        {
            controller.targetPosition = inputs.targetWorldPosition;
            controller.SteerToTarget(controller.targetPosition);
        }
    }
}
