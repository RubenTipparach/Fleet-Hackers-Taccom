using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DragSelection : MonoBehaviour,
	IPointerDownHandler,
	IDragHandler, 
	IPointerUpHandler
{
	bool isInSelectionMode = false;

	// cool, easier than imediate. Attach an inactive selection box to this thing.
	[SerializeField]
	RectTransform selectionBox;

	[SerializeField]
	GameObject healtBarPrefab;

	[SerializeField]
	Canvas canvas;

	Vector2 rectPosition;
	Vector2 rectSize;

    public List<GameObject> registeredShips;

    /// <summary>
    /// Sealing this reference so hard. Use this to operate on the set of ships.
    /// </summary>
    public IEnumerable<BasicShip> SelectedShips
	{
		get
		{
			return selectedShips;
		}
	}

	List<BasicShip> selectedShips;

	SpaceObject obj;

	List<HealthBar> healthBars;


    /// <summary>
    /// Drag select spaceships.
    /// </summary>
    /// <param name="eventData"></param>
	public void OnDrag(PointerEventData eventData)
	{

		if(!Input.GetMouseButton(0)) { return; }

		rectSize += eventData.delta;

		// DAT MATHFFFFFFFFFF!!!!!
		if (Mathf.Sign(rectSize.x) == 1 && Mathf.Sign(rectSize.y) == 1)
		{
			selectionBox.position = new Vector2(rectPosition.x, rectPosition.y + rectSize.y);
		}
		if (Mathf.Sign(rectSize.x) == -1 && Mathf.Sign(rectSize.y) == -1)
		{
			selectionBox.position = new Vector2(rectPosition.x + rectSize.x, rectPosition.y );
		}
		if (Mathf.Sign(rectSize.x) == -1 && Mathf.Sign(rectSize.y) == 1)
		{
			selectionBox.position = eventData.position;
		}
		if (Mathf.Sign(rectSize.x) == 1 && Mathf.Sign(rectSize.y) == -1)
		{
			selectionBox.position = rectPosition;
		}

		selectionBox.sizeDelta = new Vector2(Mathf.Abs(rectSize.x), Mathf.Abs(rectSize.y));

		//find and select all game objects.
		Rect selectionRect = GetSelectionRect(rectPosition, rectPosition + rectSize);
        selectedShips.Clear();

		ForEachAndAllShips((BasicShip ship) =>
		{
			Vector2 screenPos = Camera.main.WorldToScreenPoint(ship.transform.position);
			screenPos.y = Screen.height - screenPos.y;// invert y lols

			//Debug.Log(gb.name + ": " + screenPos);
			if (selectionRect.Contains(screenPos) && !selectedShips.Contains(ship))
			{
				selectedShips.Add(ship);
			}
		});

		//Debug.Log(rectPosition);
		//Debug.Log(rectSize);
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		if (!Input.GetMouseButton(0)) { return; }

		selectionBox.position = eventData.position;
		rectPosition = eventData.position;
		rectSize = Vector2.zero;
		selectionBox.gameObject.SetActive(true);
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		selectionBox.sizeDelta = new Vector2(0, 0);
		selectionBox.gameObject.SetActive(false);

		//clear everything. might optimize later, but I don't expect more than 1000 ship at once so whatever.
		ForEachAndAllShips((BasicShip ship) =>
		{
			ship.HealthBar.gameObject.SetActive(false);
		});

		foreach (var gb in selectedShips)
		{
			//Debug.Log("Selected: " + gb.gameObject.name);
			gb.HealthBar.gameObject.SetActive(true);
		}
	}

    private void Awake()
    {
        registeredShips = new List<GameObject>();
        healthBars = new List<HealthBar>();

    }

    // Use this for initialization
    void Start()
	{
		selectedShips = new List<BasicShip>();

        ForEachAndAllShips((BasicShip ship) =>
		{
			//Debug.Log(ship.gameObject.name +" selected");
			//.... all ship info should be contained here for convenience,
			GameObject gb = GameObject.Instantiate(healtBarPrefab);
			gb.transform.SetParent(canvas.transform);

			var healthBar = gb.GetComponent<HealthBar>();
			healthBar.SetInitVal(ship.currentHealth);
			healthBar.ship = ship;

            healthBars.Add(healthBar);
			gb.SetActive(false);

			// fuck yes, set the data of a ship to that ship.
			ship.HealthBar = healthBar;
		});
	}

    public void RegisterShip(BasicShip ship)
    {
        registeredShips.Add(ship.gameObject);

        //Debug.Log(ship.gameObject.name +" selected");
        //.... all ship info should be contained here for convenience,
        GameObject gb = GameObject.Instantiate(healtBarPrefab);
        gb.transform.SetParent(canvas.transform);

        var healthBar = gb.GetComponent<HealthBar>();
        healthBar.SetInitVal(ship.currentHealth);
        healthBar.ship = ship;

        healthBars.Add(healthBar);
        gb.SetActive(false);

        // fuck yes, set the data of a ship to that ship.
        ship.HealthBar = healthBar;
    }

	// Update is called once per frame
	void Update ()
	{
		foreach (var ship in selectedShips)
		{
			((RectTransform)ship.HealthBar.transform).position = Camera.main.WorldToScreenPoint(ship.transform.position);
		}
	}

	// helper methods!
	public void ForEachAndAllShips(Action<BasicShip> action)
	{
		//GameObject[] gobs = GameObject.FindGameObjectsWithTag("Ship");
		foreach (var gb in registeredShips)
		{
			var getBasicShip = gb.GetComponent<BasicShip>();
			if (getBasicShip != null)
			{
				action(getBasicShip);
			}
		}
	}

	// Converts rect to wierd ass canvas rect.
	public static Rect GetSelectionRect(Vector2 start, Vector2 end)
	{
		int width = (int)(end.x - start.x);
		int height = (int)((Screen.height - end.y) - (Screen.height - start.y));

		if (width < 0 && height < 0)
		{
			return (new Rect(end.x, Screen.height - end.y, Mathf.Abs(width), Mathf.Abs(height)));
		}
		else if (width < 0)
		{
			return (new Rect(end.x, Screen.height - start.y, Mathf.Abs(width), height));
		}
		else if (height < 0)
		{
			return (new Rect(start.x, Screen.height - end.y, width, Mathf.Abs(height)));
		}
		else
		{
			return (new Rect(start.x, Screen.height - start.y, width, height));
		}
	}

}
