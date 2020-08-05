using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Yarn.Unity;

public class DialogScreen : MonoBehaviour
{
	private const string ResourcePath = "dialog_images";

	public script_GUI gui;

	public TextMeshProUGUI moneyText;
	public TextMeshProUGUI conversationTitle;
	public Scrollbar conversationScroll;
	public Transform conversationHolder;
	public Transform choiceHolder;
	public DialogChoice choiceObject;
	public DialogPiece dialogObject;
	public Image dialogImage;
	public GameObject dialogSpacer;

	private CustomDialogUI yarnUI;
	private InMemoryVariableStorage storage;
	private DialogueRunner runner;
	private Settlement city;

	private void OnValidate() 
	{
		yarnUI = GetComponent<CustomDialogUI>();
		storage = GetComponent<InMemoryVariableStorage>();
		runner = GetComponent<DialogueRunner>();
	}

	private void OnEnable() 
	{
		UpdateMoney();
	}

	public void AddToDialogText(string speaker, string text, TextAlignmentOptions align) {
		StartCoroutine(DoAddToDialogText(speaker, text, align));
	}

	public void AddImage(string imgName) {
		StartCoroutine(DoAddImage(imgName));
	}
	
	private void SetCity(Settlement s) 
	{
		city = s;
		Debug.Log("Current settlement: " + city.name);
		storage.SetValue("$city_name", new Yarn.Value(city.name));
		storage.SetValue("$city_description", new Yarn.Value(city.description));
	}

	public void StartDialog(Settlement s) {
		SetCity(s);
		Clear();
		StartCoroutine(StartDialog());
	}

	private IEnumerator StartDialog() {
		yield return null;
		yield return null;
		runner.StartDialogue();
	}

	private IEnumerator DoAddToDialogText(string speaker, string text, TextAlignmentOptions align) 
	{
		DialogPiece p = Instantiate(dialogObject);
		p.SetAlignment(align);
		p.SetText(speaker, text);
		yield return null;
		p.transform.SetParent(conversationHolder);
		p.transform.SetSiblingIndex(conversationHolder.childCount - 2);
		yield return null;
		conversationScroll.value = 0;
		yield return null;
		conversationScroll.value = 0;
	}

	public IEnumerator DoAddImage(string imgName) 
	{
		Sprite s = Resources.Load<Sprite>(ResourcePath + "/" + imgName);

		if (s != null) {
			Image i = Instantiate(dialogImage);
			i.sprite = s;
			yield return null;
			i.transform.SetParent(conversationHolder);
			i.transform.SetSiblingIndex(conversationHolder.childCount - 2);
			yield return null;
			conversationScroll.value = 0;
			yield return null;
			conversationScroll.value = 0;
		}
	}

	public void AddContinueOption() 
	{
		ClearOptions();
		if (!yarnUI.EndOfBlock) {
			AddChoice("Continue", yarnUI.MarkLineComplete);
		}
		else {
			StartCoroutine(WaitAndComplete());
		}
	}

	private IEnumerator WaitAndComplete() {
		yield return null;
		yarnUI.MarkLineComplete();
	}

	public void AddChoice(string text, UnityEngine.Events.UnityAction click) 
	{
		DialogChoice c = Instantiate(choiceObject);
		c.SetText(text);
		c.transform.SetParent(choiceHolder);
		c.SetOnClick(click);
	}

	public void Clear() 
	{
		ClearChildren(conversationHolder);
		Instantiate(dialogSpacer).transform.SetParent(conversationHolder);
		ClearChildren(choiceHolder);
	}

	public void ClearOptions() 
	{
		ClearChildren(choiceHolder);
	}

	private void ClearChildren(Transform parent) 
	{
		Transform[] objs = parent.GetComponentsInChildren<Transform>();
		foreach (Transform t in objs) 
		{
			if (t != parent) 
			{
				Destroy(t.gameObject);
			}

		}
	}

	private IEnumerator DeactivateSelf() {
		Clear();
		yield return null;
		gameObject.SetActive(false);
	}

	public void ExitConversation() {
		bool city = storage.GetValue("$entering_city").AsBool;
		Debug.Log($"Exiting the conversation. Entering the city {city}");

		if (city) {
			gui.GUI_EnterPort();
		}
		else {
			gui.GUI_ExitPortNotification();
		}

		StartCoroutine(DeactivateSelf());
	}

	[YarnCommand("reset")]
	public void ResetConversation() {
		storage.SetValue("$random_text", new Yarn.Value("Random text"));
		storage.SetValue("$random_bool", new Yarn.Value(false));
		storage.SetValue("$convo_title", new Yarn.Value("Convertation Title"));
		storage.SetValue("$emotion", new Yarn.Value("neutral"));
		storage.SetValue("$jason_connected", false);
		storage.SetValue("$crew_name", new Yarn.Value("Bob IV"));
		Clear();
	}

	[YarnCommand("setconvotitle")]
	public void SetConversationTitle(string title) {
		string text = title.Replace('_', ' ');
		conversationTitle.text = text;
	}

	[YarnCommand("randomtext")]
	public void GenerateRandomText(string[] inputs) 
	{
		System.Enum.TryParse(inputs[0], out DialogText.Type t);
		DialogText.Emotion e = DialogText.Emotion.neutral;
		if (inputs[1] == "any") {
			e = DialogText.RandomEmotion();
		}
		else {
			System.Enum.TryParse(inputs[1], out e);
		}

		List<DialogText> matchingType = Globals.GameVars.portDialogText.FindAll(x => x.TextType == t);
		List<DialogText> matchingBoth = matchingType.FindAll(x => x.TextEmotion == e);

		if (matchingBoth.Count == 0) {
			Debug.Log($"Nothing found with both type {t.ToString()} and emotion {e.ToString()} ({matchingType.Count} matching just type)");
		}

		int i = Random.Range(0, matchingBoth.Count);
		
		Yarn.Value randText = new Yarn.Value(matchingBoth[i].Text);
		storage.SetValue("$random_text", randText);

		storage.SetValue("$emotion", new Yarn.Value(e.ToString()));
	}

	[YarnCommand("randombool")]
	public void TrueOrFalse(string threshold) {
		float limit = float.Parse(threshold);
		bool b = Random.Range(0f, 1f) < limit;
		Yarn.Value randBool = new Yarn.Value(b);
		storage.SetValue("$random_bool", randBool);
	}

	[YarnCommand("citynetworks")]
	public void NumberOfCityNetworks() {
		storage.SetValue("$city_networks", city.networks.Count);
	}

	[YarnCommand("networkconnections")]
	public void NumberOfConnections() {
		IEnumerable<CrewMember> connected = Globals.GameVars.Network.CrewMembersWithNetwork(city, true);
		int connectedNum = Enumerable.Count(connected);
		storage.SetValue("$connections_number", connectedNum);
	}

	[YarnCommand("cityinfo")]
	public void SetCityInfo() {
		storage.SetValue("$city_name", new Yarn.Value(city.name));
		storage.SetValue("$city_description", new Yarn.Value(city.description));
	}

	[YarnCommand("updatemoney")]
	public void UpdateMoney() 
	{
		moneyText.text = Globals.GameVars.playerShipVariables.ship.currency + " dr";
	}

	[YarnCommand("checkafford")]
	public void CheckAffordability(string cost) {
		int itemCost = 0;
		if (cost[0] == '$') {
			itemCost = IntFromVariableName(cost);
		}
		else {
			itemCost = int.Parse(cost);
		}
		storage.SetValue("$can_afford", Globals.GameVars.playerShipVariables.ship.currency >= itemCost);
	}

	[YarnCommand("checkaffordpercent")]
	public void CheckAffordability(string[] costs) {
		int itemCost = 0;
		if (costs[0][0] == '$') {
			itemCost = IntFromVariableName(costs[0]);
		}
		else {
			itemCost = int.Parse(costs[0]);
		}
		float percent = float.Parse(costs[1]);

		float total = itemCost + (itemCost * percent);

		storage.SetValue("$can_afford", Globals.GameVars.playerShipVariables.ship.currency >= total);
	}

	[YarnCommand("pay")]
	public void PayAmount(string cost) {
		int itemCost = 0;
		if (cost[0] == '$') {
			itemCost = IntFromVariableName(cost);
		}
		else {
			itemCost = int.Parse(cost);
		}
		Globals.GameVars.playerShipVariables.ship.currency -= itemCost;
		UpdateMoney();
	}

	[YarnCommand("calculatetaxes")]
	public void CalculateTaxCharges() {
		storage.SetValue("$tax_subtotal", Random.Range(1, 250));
	}

	[YarnCommand("calculatepercents")]
	public void CalculateIntentPercent() {
		float subtotal = storage.GetValue("$tax_subtotal").AsNumber;
		float cargo = CargoValue();

		float percent = 0.1f;
		storage.SetValue("$water_intent", (int)(percent * cargo + subtotal));

		percent = 0.2f;
		storage.SetValue("$trade_intent", (int)(percent * cargo + subtotal));

		percent = 0.3f;
		storage.SetValue("$tavern_intent", (int)(percent * cargo + subtotal));

		percent = 0.5f;
		storage.SetValue("$all_intent", (int)(percent * cargo + subtotal));
	}

	[YarnCommand("checkcitytaxes")]
	public void CheckCityTaxes() {
		storage.SetValue("$god_tax", Random.Range(0.0f, 1.0f) > 0.5f);
		storage.SetValue("$god_tax_amount", Random.Range(0, 50));
		storage.SetValue("$transit_tax", Random.Range(0.0f, 1.0f) > 0.5f);
		storage.SetValue("$transit_tax_amount", Random.Range(2, 6));
		storage.SetValue("$foreigner_tax", Random.Range(0.0f, 1.0f) > 0.5f);
		storage.SetValue("$foreigner_tax_amount", 2);
		storage.SetValue("$wealth_tax", CargoValue() >= 1000000);
		storage.SetValue("$wealth_tax_amount", 10);

		storage.SetValue("$no_taxes", !storage.GetValue("$god_tax").AsBool && !storage.GetValue("$transit_tax").AsBool && !storage.GetValue("$foreigner_tax").AsBool && !storage.GetValue("$wealth_tax").AsBool);
	}

	[YarnCommand("connectedcrew")]
	public void ConnectedCrewName() {
		if (Globals.GameVars.Network.GetCrewMemberNetwork(Globals.GameVars.Jason).Contains(city)) {
			storage.SetValue("$jason_connected", true);
			storage.SetValue("$crew_name_1", "me");
			storage.SetValue("$crew_name_2", "I");
			storage.SetValue("$crew_name_3", "You");
			storage.SetValue("$crew_description", Globals.GameVars.Jason.backgroundInfo);
			storage.SetValue("$crew_home", Globals.GameVars.GetSettlementFromID(Globals.GameVars.Jason.originCity).name);
		}
		else {
			IEnumerable<CrewMember> connected = Globals.GameVars.Network.CrewMembersWithNetwork(city);
			CrewMember crew = connected.RandomElement();
			storage.SetValue("$crew_name_1", crew.name);
			storage.SetValue("$crew_name_2", crew.name);
			storage.SetValue("$crew_name_3", crew.name);
			storage.SetValue("$crew_description", crew.backgroundInfo);
			storage.SetValue("$crew_home", Globals.GameVars.GetSettlementFromID(crew.originCity).name);
		}
	}

	private int IntFromVariableName(string name) {
		return (int)storage.GetValue(name).AsNumber;
	}

	private float CargoValue() {
		return Globals.GameVars.playerShipVariables.ship.GetTotalCargoAmount();
	}
}
