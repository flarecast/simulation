using UnityEngine;
using System.Collections;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;

public class CarManager : MonoBehaviour {

	public static Object prefab = Resources.Load("Car");
	public static CarController[] cars;
	public static int nr_cars;

	public static int status;

	// Use this for initialization
	void Start () {

		float width = gameObject.GetComponent<RectTransform> ().rect.width;
		float height = gameObject.GetComponent<RectTransform> ().rect.height;
		CarController.imWidth = width;
		CarController.imHeight = height;

		string[] files = Directory.GetFiles("positions/", "*.txt", SearchOption.TopDirectoryOnly);

		nr_cars = files.Length;
		cars = new CarController[nr_cars];

		List<Thread> threads = new List<Thread> ();

		int i = 0;
		foreach (string name in files) {
			GameObject newCar = Instantiate (prefab) as GameObject;
			CarController controller = newCar.GetComponent<CarController> ();
			cars [i] = controller;

			// Can't be multithreaded due to instantiates
			controller.Begin (name, i);

			// Multithreaded Socket connections to python
			Thread t = new Thread (delegate() {
				controller.SetupSockets ();
			});

			threads.Add (t);
			t.Start ();
			i++;
		}

		foreach (Thread t in threads) {
			t.Join ();
		}

		foreach (CarController c in cars) {
			c.StartReceiving ();
		}

		// Setup was finished for all cars, ready to get working
		CarController.working = true;
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
