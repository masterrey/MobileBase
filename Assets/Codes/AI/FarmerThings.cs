using LLMUnity;
using System.Collections;
using UnityEngine;
using System.Text.RegularExpressions;
using UnityEngine.LightTransport;
using UnityEngine.UIElements;
using Newtonsoft.Json;
using TMPro;
using System.Linq;

public class FarmerThings : AIFreeWill
{
    [SerializeField] GameObject hand;
    [SerializeField] int foodAmount = 0;
    [SerializeField] int maxFoodAmount = 5;
    [SerializeField] float maxDistance = 5f;
    [SerializeField] GameObject foodFound;
    [SerializeField] float foodCollectedWithSucess = 0;
    [SerializeField] string lastSystemMessage = string.Empty;
    [SerializeField] TextMeshProUGUI commentBubble;

    public LLMCharacter llmCharacter;

    [System.Serializable]
    public class PositionData
    {
        public float x, y, z;
    }

    [System.Serializable]
    public class LLMActionResponse
    {
        public string action;
        public PositionData position;
        public string comment;
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
        string prompt =
            $"Last system message: {lastSystemMessage}\n" +
            $"Food: {foodAmount}/{maxFoodAmount}\n" +
            $"Current state: {currentState.ToString().ToLower()}\n" +
            $"Current position: {transform.position.x}, {transform.position.y}, {transform.position.z}\n" +
            $"Hand: {(hand != null && hand.transform.childCount > 0 ? "has food" : "empty")}\n" +
            $"Base position: {basePoint.transform.position.x}, {basePoint.transform.position.y}, {basePoint.transform.position.z}\n" +
            $"Food found: {(foodFound != null ? foodFound.name : "none")}\n" +
            $"Food position: {(foodFound != null ? foodFound.transform.position.x + ", " + foodFound.transform.position.y + ", " + foodFound.transform.position.z : "none")}\n";

        var task = llmCharacter.Chat(prompt);
        yield return new WaitUntil(() => task.IsCompleted);

        string rawResponse = task.Result;
        Debug.Log("LLM Response: " + rawResponse);

        LLMActionResponse result = null;
        try
        {
            // Clean up smart quotes and invisible characters
            string cleanResponse = rawResponse
                .Replace('“', '"')
                .Replace('”', '"')
                .Replace('\u0000', ' ')
                .Trim();

            // Try to extract just the valid JSON from raw string
            int startIndex = cleanResponse.IndexOf('{');
            int endIndex = cleanResponse.LastIndexOf('}');

            if (startIndex == -1 || endIndex == -1 || endIndex <= startIndex)
                throw new System.Exception("Could not locate JSON boundaries.");

            string json = cleanResponse.Substring(startIndex, endIndex - startIndex + 1);
            Debug.Log("Extracted JSON: " + json);

            result = JsonConvert.DeserializeObject<LLMActionResponse>(json);
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
            Debug.LogWarning("Invalid or empty LLM response.");
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

        if (!string.IsNullOrEmpty(result.comment))
        {
            Debug.Log($"Comment from LLM: {result.comment}");
            StartCoroutine(ShowComment(result.comment));
        }

        switch (result.action)
        {
            case "return_to_base":
                ReturnToBase();
                break;
            case "grab_food":
                GrabFood();
                break;
            case "drop_food":
                DropFood();
                break;
            case "idle":
                break;
            case "go_to_position":
                if (result.position != null)
                {
                    Debug.Log($"Moving to position: {result.position.x}, {result.position.y}, {result.position.z}");
                    SetandWaitTargetPosition(new Vector3(result.position.x, result.position.y, result.position.z));
                }
                break;
            default:
                Debug.LogWarning("Unrecognized action from LLM: " + result.action);
                lastSystemMessage = "Unrecognized action from LLM: " + result.action;
                break;
        }

        onComplete?.Invoke();
    }

    IEnumerator ShowComment(string text, float duration = 3f)
    {
        if (commentBubble != null)
        {
            commentBubble.text = text;
            commentBubble.gameObject.SetActive(true);
            yield return new WaitForSeconds(duration);
            commentBubble.gameObject.SetActive(false);
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
