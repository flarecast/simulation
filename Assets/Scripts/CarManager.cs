using UnityEngine;
using System.Collections;
using System.IO;

public class CarManager : MonoBehaviour {

	public static Object prefab = Resources.Load("Car");
	public static readonly int NR_CARS = 3;

	// Use this for initialization
	void Start () {
		for (int i = 0; i < NR_CARS; i++) {
			GameObject newCar = Instantiate (prefab) as GameObject;
			newCar.SendMessage ("Begin", "positions/positions." + i + ".txt");
		}
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
