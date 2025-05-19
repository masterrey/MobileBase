using System.Collections;
using UnityEngine;
using UnityEngine.AI;


public class AIFreeWill : MonoBehaviour
{
    [SerializeField]
    protected NavMeshAgent agent;
    [SerializeField]
    protected Vector3 targetPosition;

    [SerializeField]
    Animator animator;

    [SerializeField]
    protected enum AIState
    {
        Idle,
        Walking,
        Attacking,
        Dead,
        Gathering,
    }
    [SerializeField]
    protected AIState currentState = AIState.Idle;

    [SerializeField]
    protected enum AIType
    {
        Farmer,
        Hunter,
        Gatherer,
        Merchant,
        Warrior,
    }
    protected AIType currentType = AIType.Farmer;

    protected delegate IEnumerator StateAction();
    protected StateAction currentAction;

    public GameObject basePoint;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected void Start()
    {
        StartCoroutine(FreeWill());
    }


    IEnumerator FreeWill()
    {
        int RandomNumber = Random.Range(0, 5);
        yield return new WaitForSeconds(RandomNumber);

        StartCoroutine(currentAction());

        yield return new WaitForSeconds(RandomNumber);

        StartCoroutine(FreeWill());
    }

    // Update is called once per frame
    protected void Update()
    {
        if (agent.remainingDistance > agent.stoppingDistance)
        {
            animator.SetFloat("Speed", agent.velocity.magnitude);
        }

        switch (currentState)
        {
            case AIState.Idle:
                animator.SetFloat("Speed", agent.velocity.magnitude);
                StopMoving();

                break;
            case AIState.Walking:
                animator.SetFloat("Speed", agent.velocity.magnitude);
                break;
            case AIState.Attacking:
                animator.SetTrigger("Attack");
                StopMoving();
                break;
            case AIState.Dead:
                animator.SetTrigger("Die");
                StopMoving();
                break;
            case AIState.Gathering:
                animator.SetTrigger("Gather");
                StopMoving();
                break;
            default:
                animator.SetFloat("Speed", agent.velocity.magnitude);
                break;

        }
    }

    public void SetTargetPosition(Vector3 newTargetPosition)
    {
        targetPosition = newTargetPosition;
        agent.SetDestination(targetPosition);
        agent.isStopped = false;
        currentState = AIState.Walking;
    }

    /// <summary>
    /// Return to base
    /// </summary>
    public bool ReturnToBase()
    {
        Debug.Log("Returning to base");
        targetPosition = basePoint.transform.position;
        agent.SetDestination(targetPosition);
        agent.isStopped = false;
        currentState = AIState.Walking;
        if (Vector3.Distance(transform.position, targetPosition) < 1)
        {
            agent.isStopped = true;
            currentState = AIState.Idle;
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool SetandWaitTargetPosition(Vector3 newTargetPosition)
    {
        targetPosition = newTargetPosition;
        agent.SetDestination(targetPosition);
        agent.isStopped = false;
        currentState = AIState.Walking;
        Debug.Log("Moving to " + targetPosition);
        if (Vector3.Distance(targetPosition,transform.position)<1)
        {
            Debug.Log("Arrived at " + targetPosition);
            agent.isStopped = true;
            currentState = AIState.Idle;
            return true;
        }
        else
        {
            return false;
        }
    }
    public void StopMoving()
    {
        agent.isStopped = true;
    }
    public void StartMoving()
    {
        agent.isStopped = false;
        agent.SetDestination(targetPosition);
    }

    

}
