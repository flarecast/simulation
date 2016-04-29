using UnityEngine;
using System.Collections;
using System.IO;

public class CarManager : MonoBehaviour {

	public static Object prefab = Resources.Load("Car");
	public static readonly int NR_CARS = 3;

	// Use this for initialization
	void Start () {

		float width = gameObject.GetComponent<RectTransform> ().rect.width;
		float height = gameObject.GetComponent<RectTransform> ().rect.height;

		for (int i = 0; i < NR_CARS; i++) {
			GameObject newCar = Instantiate (prefab) as GameObject;
			newCar.GetComponent<CarController>().Begin("positions/positions." + i + ".txt", width, height);
		}
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
