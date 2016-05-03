using UnityEngine;
using System.Collections;
using System.IO;

public class CarManager : MonoBehaviour {

	public static Object prefab = Resources.Load("Car");
	public static readonly int NR_CARS = 3;
	public static CarController[] cars = new CarController[NR_CARS];

	// Use this for initialization
	void Start () {

		float width = gameObject.GetComponent<RectTransform> ().rect.width;
		float height = gameObject.GetComponent<RectTransform> ().rect.height;
		CarController.imWidth = width;
		CarController.imHeight = height;

		for (int i = 0; i < NR_CARS; i++) {
			GameObject newCar = Instantiate (prefab) as GameObject;
			CarController controller = newCar.GetComponent<CarController> ();
			cars [i] = controller;
			controller.Begin("positions/positions." + i + ".txt");
		}
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
