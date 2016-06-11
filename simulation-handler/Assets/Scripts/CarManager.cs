using UnityEngine;
using System.Collections;
using System.IO;

public class CarManager : MonoBehaviour {

	public static Object prefab = Resources.Load("Car");
	public static CarController[] cars;
	public static int nr_cars;

	// Use this for initialization
	void Start () {

		float width = gameObject.GetComponent<RectTransform> ().rect.width;
		float height = gameObject.GetComponent<RectTransform> ().rect.height;
		CarController.imWidth = width;
		CarController.imHeight = height;

		string[] files = Directory.GetFiles("positions/", "positions.*.txt", SearchOption.TopDirectoryOnly);

		nr_cars = files.Length;
		cars = new CarController[nr_cars];

		for (int i = 0; i < nr_cars; i++) {
			GameObject newCar = Instantiate (prefab) as GameObject;
			CarController controller = newCar.GetComponent<CarController> ();
			cars [i] = controller;
			controller.Begin("positions/positions." + i + ".txt", i);
		}
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
