using System.Collections;
using UnityEngine;

public class FarmerThings : AIFreeWill
{
    Vector3 targetPosition;

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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentType = AIType.Farmer;
        currentState = AIState.Idle;

        currentAction = SearchForFood;
        base.Start();
    }

    // Update is called once per frame
    void Update()
    {
        base.Update();
       
    }
    /// <summary>
    /// Search for food coroutine
    /// </summary>
    /// <returns></returns>
    public IEnumerator SearchForFood()
    {
        Debug.Log("Searching for food");
        //check if the farmer has food in his hand
        if (foodAmount >= maxFoodAmount)
        {
            // If the farmer has food, go to the base
            if (ReturnToBase())
            {
                // Drop the food
                for (int i = 0; i < foodAmount; i++)
                {
                    DropFood();
                    yield return new WaitForSeconds(0.5f);
                }
            }
        }
        else { 
            if (foodFound)
            {
                GotoTheFood();
                GrabFood();
                yield return new WaitForSeconds(0.1f);
            }
            else
            {
                // If the farmer doesn't have food, search for food
                targetPosition = new Vector3(Random.Range(-maxDistance, maxDistance), 0, Random.Range(-maxDistance, maxDistance));
            }
            Debug.Log("Searching for food at " + targetPosition);
            SetTargetPosition(targetPosition);
        }
    }

    public void DropFood()
    {
        // Drop the food
        if (hand.transform.childCount > 0)
        {
            GameObject food = hand.transform.GetChild(0).gameObject;
            food.transform.SetParent(null);
            food.GetComponent<Rigidbody>().isKinematic = false;
            food.GetComponent<Collider>().enabled = true;
            food.transform.gameObject.tag = "Item";
            foodAmount--;
        }
    }
   
    /// <summary>
    /// Get the food and set the target position
    /// </summary>
    /// <param name="foodFound"></param>
    public void GrabFood()
    {
        if (Vector3.Distance(foodFound.transform.position, transform.position) < 1)
        {
            foodFound.tag = "Untagged";
            foodFound.transform.position = hand.transform.position;
            foodFound.transform.SetParent(hand.transform);
            foodFound.transform.localPosition = Vector3.zero;
            foodFound.transform.localRotation = Quaternion.identity;
            foodFound.GetComponent<Rigidbody>().isKinematic = true;
            foodFound.GetComponent<Collider>().enabled = false;
            foodFound = null;
            foodAmount++;
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

        if (other.CompareTag("Food") && !foodFound && foodAmount < maxFoodAmount)
        {
            // If the farmer collides with food, set the target position to the food's position
            targetPosition = other.transform.position;
            foodFound = other.gameObject;
        }
    }
}
