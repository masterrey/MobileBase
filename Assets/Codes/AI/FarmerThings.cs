using LLMUnity;
using System.Collections;
using UnityEngine;
using System.Text.RegularExpressions;
using UnityEngine.LightTransport;
using UnityEngine.UIElements;

public class FarmerThings : AIFreeWill
{
    [SerializeField] GameObject hand;
    [SerializeField] int foodAmount = 0;
    [SerializeField] int maxFoodAmount = 5;
    [SerializeField] float maxDistance = 5f;
    [SerializeField] GameObject foodFound;
    [SerializeField] float foodCollectedWithSucess = 0;
    [SerializeField] string lastSystemMessage = string.Empty;

    public LLMCharacter llmCharacter;

    [System.Serializable]
    public class LLMActionResponse
    {
        public string action;
        public PositionData position;

        [System.Serializable]
        public class PositionData
        {
            public float x, y, z;
        }
    }

    new void Start()
    {
        currentType = AIType.Farmer;
        currentState = AIState.Idle;
        currentAction = AskToLLMWhatToDo;
        base.Start();
    }

    public IEnumerator AskToLLMWhatToDo(System.Action onComplete)
    {
        string prompt = $"You are an autonomous farmer agent inside a simulation.\n" +
                        $"Last system message: {lastSystemMessage}\n" +
                        $"Food: {foodAmount}/{maxFoodAmount}\n" +
                        $"Current state: {currentState.ToString().ToLower()}\n" +
                        $"Current position: {transform.position.x}, {transform.position.y}, {transform.position.z}\n" +
                        $"Hand: {(hand != null && hand.transform.childCount > 0 ? "has food" : "empty")}\n" +
                        $"Base position: {basePoint.transform.position.x}, {basePoint.transform.position.y}, {basePoint.transform.position.z}\n" +
                        $"Food found: {(foodFound != null ? foodFound.name : "none")}\n" +
                        $"Food position: {(foodFound != null ? foodFound.transform.position.x + ", " + foodFound.transform.position.y + ", " + foodFound.transform.position.z : "none")}\n" +
                        "Your current goal is to gather food when it's close and return it to your base, you can grab food when you are close to than, you can drop the food when you are on the base.\n\n" +
                        "Your available actions are:\n" +
                        "- return_to_base\n" +
                        "- grab_food\n" +
                        "- drop_food\n" +
                        "- idle\n" +
                        "- go_to_position(x,y,z)\n\n" +
                        "Rules:\n" +
                        "1. Return ONLY one action in JSON format as shown below.\n" +
                        "2. Use `go_to_position` only if it's necessary, with coordinates in float.\n" +
                        "3. Do not explain or include any extra text.\n\n" +
                        "Example:\n" +
                        "{ \"action\": \"search_for_food\" }\n" +
                        "or\n" +
                        "{ \"action\": \"go_to_position\", \"position\": { \"x\": 10.5, \"y\": 0.0, \"z\": -3.2 } }\n\n" +
                        "IMPORTANT:\n" +
                        "- ONLY include the `position` field if the action is \"go_to_position\".\n" +
                        "- ALWAYS USE DOT (.) AS THE DECIMAL SEPARATOR, NEVER COMMA (,).\n" +
                        "- Do NOT use markdown code blocks.\n" +
                        "- Only suggest coordinates that are within the world bounds.\n" +
                        $"- Valid positions must be within {maxDistance} meters of the current position.\n" +
                        "- Do not invent distant or unrelated coordinates. Use known object positions when available.\n";

        try
        {
            llmCharacter.ClearChat();
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Failed to clear chat: " + ex.Message);
        }

        var task = llmCharacter.Chat(prompt);
        yield return new WaitUntil(() => task.IsCompleted);

        string rawResponse = task.Result;
        Debug.Log("LLM Response: " + rawResponse);

        string response = rawResponse
            .Replace("```json", "")
            .Replace("```", "")
            .Replace("\n", "")
            .Replace("\r", "")
            .Trim();

        // Replace commas with dots for decimal parsing
        response = Regex.Replace(response, @"(?<=""[xyz]""\s*:\s*)(-?\d+),(\d+)", "$1.$2");

        LLMActionResponse result = null;
        try
        {
            result = JsonUtility.FromJson<LLMActionResponse>(response);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Failed to parse LLM response: " + ex.Message + "\nRaw: " + rawResponse);
            lastSystemMessage = "Error parsing response.";
            onComplete?.Invoke();
            yield break;
        }

        if (result == null || string.IsNullOrEmpty(result.action))
        {
            Debug.LogWarning("Invalid or empty LLM response: " + response);
            lastSystemMessage = "Invalid or empty response.";
            onComplete?.Invoke();
            yield break;
        }

        if (result.action != "go_to_position" && result.position != null)
        {
            Debug.LogWarning($"Action '{result.action}' should not include a 'position' field. Ignoring it.");
            lastSystemMessage = $"Invalid 'position' field with action '{result.action}'.";
            result.position = null;
        }

        switch (result.action)
        {
            case "return_to_base":
                ReturnToBase();
                onComplete?.Invoke();
                break;
            case "grab_food":
                GrabFood();
                onComplete?.Invoke();
                break;
            case "drop_food":
                DropFood();
                onComplete?.Invoke();
                break;
            case "idle":
                onComplete?.Invoke();
                break;
            case "go_to_position":
                Debug.Log($"Moving");
                if (result.position != null)
                {
                    Debug.Log($"to position: {result.position.x}, {result.position.y}, {result.position.z}");
                    SetandWaitTargetPosition(new Vector3(result.position.x, result.position.y, result.position.z));
                    onComplete?.Invoke();
                }
                break;
            default:
                Debug.LogWarning("Unrecognized action from LLM: " + result.action);
                lastSystemMessage = "Unrecognized action from LLM: " + result.action;
                onComplete?.Invoke();
                break;
        }
    }

    public void DropFood()
    {
        if (hand != null && hand.transform.childCount > 0)
        {
            GameObject food = hand.transform.GetChild(0).gameObject;
            food.transform.SetParent(null);
            food.transform.position = transform.position + transform.forward * 0.5f;
            food.tag = "Item";
            if (food.TryGetComponent<Rigidbody>(out var rb)) rb.isKinematic = false;
            if (food.TryGetComponent<Collider>(out var col)) col.enabled = true;
            foodAmount--;
        }
    }

    public void GrabFood()
    {
        if (foodFound != null)
        {
            SetandWaitTargetPosition(targetPosition = foodFound.transform.position);
            if (hand != null && foodAmount < maxFoodAmount)
            {
                foodFound.transform.SetParent(hand.transform);
                foodFound.transform.localPosition = Vector3.zero;
                foodFound.transform.localRotation = Quaternion.identity;
                foodFound.tag = "Untagged";
                if (foodFound.TryGetComponent<Rigidbody>(out var rb)) rb.isKinematic = true;
                if (foodFound.TryGetComponent<Collider>(out var col)) col.enabled = false;
                foodFound = null;
                foodAmount = hand.transform.childCount;
            }
        }
        else
        {
            lastSystemMessage = "Too far to grab food.";
        }
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Food"))
        {
            if (foodFound == null)
            {
                foodFound = other.gameObject;
                Debug.Log($"Food found: {foodFound.name}");
            }
        }
    }
}
