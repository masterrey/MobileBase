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
    [SerializeField]
    protected AIType currentType = AIType.Farmer;

    protected delegate IEnumerator StateAction(System.Action onComplete);
    protected StateAction currentAction;
    protected Coroutine currentRoutine;

    public GameObject basePoint;


    protected virtual void Awake()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected void Start()
    {
        currentRoutine = StartCoroutine(FreeWill());
    }

    protected virtual void Update()
    {
        animator.SetFloat("Speed", agent.velocity.magnitude);

        //check if the agent has no freewill for a while

    }

    IEnumerator FreeWill()
    {
        while (true)
        {
            int randomNumber = Random.Range(0, 5);
            yield return new WaitForSeconds(randomNumber);

            bool actionCompleted = false;
            if (currentAction != null)
            {
                yield return StartCoroutine(currentAction(() => actionCompleted = true));
                // Aguarda até o callback ser chamado
                while (!actionCompleted)
                    yield return null;
            }

            yield return new WaitForSeconds(randomNumber);
        }
    }


    public virtual void ResetRoutine()
    {
        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
            currentRoutine = null;
        }
        StopMoving();

        // Reinicia a rotina principal, se definida
        if (currentAction != null)
        {
            currentRoutine = StartCoroutine(FreeWill());
        }
    }

    protected void ChangeState(AIState newState)
    {
        if (currentState == newState)
            return;

        currentState = newState;

        switch (currentState)
        {
            case AIState.Idle:
                animator.SetFloat("Speed", 0);
                StopMoving();
                break;
            case AIState.Walking:
                animator.SetFloat("Speed", agent.velocity.magnitude);
                StartMoving();
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
        }
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
        ChangeState(AIState.Walking);
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            agent.isStopped = true;
            ChangeState(AIState.Idle);
            return true;
        }
        return false;
    }

    public bool SetandWaitTargetPosition(Vector3 newTargetPosition)
    {
        targetPosition = new Vector3(newTargetPosition.x, 0, newTargetPosition.z);
        agent.SetDestination(targetPosition);
        agent.isStopped = false;
        ChangeState(AIState.Walking);
        Debug.Log("Moving to " + targetPosition);
        if (Vector3.Distance(targetPosition,transform.position)<1)
        {
            Debug.Log("Arrived at " + targetPosition);
            agent.isStopped = true;
            ChangeState(AIState.Idle);
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
        ChangeState(AIState.Idle);
    }
    public void StartMoving()
    {
        agent.isStopped = false;
        agent.SetDestination(targetPosition);
        ChangeState(AIState.Walking);
    }

    

}
