using System.Collections;
using UnityEngine;

public class FarmerThings : AIFreeWill
{
    [SerializeField]
    GameObject hand;

    [SerializeField]
    int foodAmount = 0;

    [SerializeField]
    int maxFoodAmount = 5;

    [SerializeField]
    float maxDistance = 5f;

    [SerializeField]
    GameObject foodFound;

    new void Start()
    {
        currentType = AIType.Farmer;
        currentState = AIState.Idle;

        currentAction = SearchForFood;
        base.Start();
    }

    /// <summary>
    /// Search for food coroutine
    /// </summary>
    /// <returns></returns>
    public IEnumerator SearchForFood(System.Action onComplete)
    {
        Debug.Log("Searching for food");
        if (foodAmount >= maxFoodAmount)
        {
            if (ReturnToBase())
            {
                int drops = hand != null ? hand.transform.childCount : 0;
                for (int i = 0; i < drops; i++)
                {
                    DropFood();
                    yield return new WaitForSeconds(1f);
                }
            }
        }
        else
        {
            if (foodFound != null)
            {
                GotoTheFood();
                GrabFood();
                yield return new WaitForSeconds(0.1f);
            }
            else
            {
                // Busca por comida em uma posição aleatória
                Vector3 randomPos = new Vector3(Random.Range(-maxDistance, maxDistance), 0, Random.Range(-maxDistance, maxDistance));
                Debug.Log("Searching for food at " + randomPos);
                SetTargetPosition(randomPos);
                yield return new WaitForSeconds(0.5f);
            }
        }
        onComplete?.Invoke();
    }

    public void DropFood()
    {
        // Drop the food
        if (hand != null && hand.transform.childCount > 0)
        {
            GameObject food = hand.transform.GetChild(0).gameObject;
            food.transform.localPosition = Vector3.zero; // Reset position to avoid floating
            food.transform.localPosition += transform.forward * 0.5f; // Drop in front of the farmer
            var rb = food.GetComponent<Rigidbody>();
            var col = food.GetComponent<Collider>();
            if (rb != null) rb.isKinematic = false;
            if (col != null) col.enabled = true;
            food.transform.SetParent(null);
            food.tag = "Item";
            foodAmount--;
        }
    }
   
    /// <summary>
    /// Get the food and set the target position
    /// </summary>
    /// <param name="foodFound"></param>
    public void GrabFood()
    {
        if (foodFound != null && hand != null
        && Vector3.Distance(foodFound.transform.position, transform.position) < 1
        && foodAmount < maxFoodAmount)
        {
            foodFound.tag = "Untagged";
            foodFound.transform.SetParent(hand.transform);
            foodFound.transform.localPosition = Vector3.zero;
            foodFound.transform.localRotation = Quaternion.identity;
            var rb = foodFound.GetComponent<Rigidbody>();
            var col = foodFound.GetComponent<Collider>();
            if (rb != null) rb.isKinematic = true;
            if (col != null) col.enabled = false;
            foodFound = null;
            foodAmount = hand.transform.childCount; // Sincroniza foodAmount com o número de filhos
        }
    }

    public void GotoTheFood()
    {
        if(foodFound == null)
        {
            return;
        }
        targetPosition = foodFound.transform.position;
        SetTargetPosition(targetPosition);

    }

    public void OnTriggerEnter(Collider other)
    {
        Debug.Log("Trigger Entered " + other.name);

        if (other.CompareTag("Food") && foodFound == null && foodAmount < maxFoodAmount)
        {
            foodFound = other.gameObject;
            ResetRoutine();
        }
    }
}
